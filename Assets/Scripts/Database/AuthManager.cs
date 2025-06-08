using System;
using System.Collections;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public event Action OnStatsUpdated;

    public event Action OnUserChanged;

    private string backendUrl = "https://tessellaction-backend.onrender.com";
    public static string AuthToken { get; private set; }
    public static string UserId { get; private set; }
    public static string Username { get; private set; }
    public static int GlobalScore { get; private set; }
    public static int GamesPlayed { get; private set; }
    public static int GamesWon { get; private set; }

    private string savePath;
    private string secretKey = "tesselactiongualter";

    [System.Serializable]
    private class SavedAuthData
    {
        public string token;
        public string userId;
    }

    private void Awake()
    {
        Debug.Log($"[{(Application.isEditor ? "EDITOR" : "BUILD")}] AuthManager.Awake() chamado");

        if (Instance != null && Instance != this)
        {
            Debug.Log("AuthManager já existe, destruindo esta instância");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "auth.json");
        Debug.Log($"Save path: {savePath}");

        if (File.Exists(savePath))
        {
            Debug.Log("Ficheiro de auth encontrado, a carregar dados...");
            try
            {
                string json = File.ReadAllText(savePath);
                string decryptedJson = EncryptDecrypt(json);
                SavedAuthData data = JsonUtility.FromJson<SavedAuthData>(decryptedJson);

                AuthToken = data.token;
                UserId = data.userId;

                Debug.Log(
                    $"Dados carregados - UserId: {UserId}, Token existe: {!string.IsNullOrEmpty(AuthToken)}"
                );

                StartCoroutine(VerifyTokenCoroutine());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro ao carregar dados de auth: {e.Message}");
                File.Delete(savePath);
            }
        }
        else
        {
            Debug.Log("Nenhum ficheiro de auth encontrado");
        }
    }

    private IEnumerator VerifyTokenCoroutine()
    {
        Debug.Log("A verificar token guardado...");
        UnityWebRequest verifyRequest = new UnityWebRequest(
            backendUrl + "/api/auth/verify",
            "POST"
        );
        verifyRequest.SetRequestHeader("Authorization", "Bearer " + AuthToken);
        verifyRequest.downloadHandler = new DownloadHandlerBuffer();

        yield return verifyRequest.SendWebRequest();

        if (verifyRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token válido. A obter dados do perfil...");

            UnityWebRequest profileRequest = UnityWebRequest.Get(backendUrl + "/api/auth/me");
            profileRequest.SetRequestHeader("Authorization", "Bearer " + AuthToken);
            profileRequest.downloadHandler = new DownloadHandlerBuffer();

            yield return profileRequest.SendWebRequest();

            if (profileRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Perfil obtido com sucesso!");
                var response = JsonUtility.FromJson<ProfileResponse>(
                    profileRequest.downloadHandler.text
                );

                UserId = response.userId;
                Username = response.username;
                GlobalScore = response.globalScore;
                GamesPlayed = response.gamesPlayed;
                GamesWon = response.gamesWon;

                OnStatsUpdated?.Invoke();

                OnUserChanged?.Invoke();

                SceneManager.LoadScene("MatchmakingMenu");
            }
            else
            {
                Debug.LogError(
                    "Token era válido, mas falhou ao obter o perfil: " + profileRequest.error
                );
                ErrorMessageManager.Instance.ShowError(
                    "Failed to load profile. Check your connection."
                );
                File.Delete(savePath);
            }
        }
        else
        {
            Debug.Log("Token guardado inválido ou expirado. A remover dados locais.");
            File.Delete(savePath);
            AuthToken = null;
            UserId = null;
        }
    }

    public void Login(string username, string password)
    {
        StartCoroutine(LoginCoroutine(username, password));
    }

    public void Register(string username, string password)
    {
        StartCoroutine(RegisterCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        string jsonBody = $"{{\"username\": \"{username}\", \"password\": \"{password}\"}}";
        UnityWebRequest request = new UnityWebRequest(backendUrl + "/api/auth/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            AuthToken = response.token;
            UserId = response.userId;
            Username = username;
            GlobalScore = response.globalScore;
            GamesPlayed = response.gamesPlayed;
            GamesWon = response.gamesWon;

            OnStatsUpdated?.Invoke();
            OnUserChanged?.Invoke();

            SavedAuthData dataToSave = new SavedAuthData { token = AuthToken, userId = UserId };
            string json = JsonUtility.ToJson(dataToSave);
            string encryptedJson = EncryptDecrypt(json);

            File.WriteAllText(savePath, encryptedJson);
            SceneManager.LoadScene("MatchmakingMenu");
        }
        else
        {
            string responseText = request.downloadHandler.text;
            string errorMessage = "Login failed: Invalid credentials or network error.";

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);
                    if (!string.IsNullOrEmpty(errorResponse.error))
                    {
                        errorMessage = "Login failed: " + errorResponse.error;
                    }
                }
                catch
                {
                    // Se não conseguir, usa a mensagem padrão.
                }
            }

            ErrorMessageManager.Instance.ShowError(errorMessage);
            Debug.LogError("Erro de Login: " + request.error);
        }
    }

    private IEnumerator RegisterCoroutine(string username, string password)
    {
        string jsonBody = $"{{\"username\": \"{username}\", \"password\": \"{password}\"}}";
        UnityWebRequest request = new UnityWebRequest(backendUrl + "/api/auth/register", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            AuthToken = response.token;
            UserId = response.userId;
            Username = username;
            GlobalScore = response.globalScore;
            GamesPlayed = response.gamesPlayed;
            GamesWon = response.gamesWon;

            OnStatsUpdated?.Invoke();
            OnUserChanged?.Invoke();

            SavedAuthData dataToSave = new SavedAuthData { token = AuthToken, userId = UserId };
            string json = JsonUtility.ToJson(dataToSave);
            string encryptedJson = EncryptDecrypt(json);
            File.WriteAllText(savePath, encryptedJson);

            SceneManager.LoadScene("MatchmakingMenu");
        }
        else
        {
            string responseText = request.downloadHandler.text;
            string errorMessage = "Unknown registration error.";

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);
                    errorMessage = "Registration error: " + errorResponse.error;
                }
                catch
                {
                    errorMessage = "Registration error: " + request.error + " | " + responseText;
                }
            }
            else
            {
                errorMessage = "Registration error: " + request.error;
            }
            ErrorMessageManager.Instance.ShowError(errorMessage);
            Debug.LogError(errorMessage);
        }
    }

    public void SendGameResult(string resultado, int pontos)
    {
        if (string.IsNullOrEmpty(AuthToken) || string.IsNullOrEmpty(UserId))
        {
            Debug.LogError("AuthToken ou UserId é null! Não é possível enviar resultado do jogo.");
            return;
        }

        StartCoroutine(AtualizarEstatisticas(resultado, pontos));
    }

    private IEnumerator AtualizarEstatisticas(string resultado, int pontos)
    {
        string jsonBody = $"{{\"result\":\"{resultado}\",\"score\":{pontos}}}";
        Debug.Log($"Enviando dados para o servidor: {jsonBody}");

        UnityWebRequest request = new UnityWebRequest(backendUrl + "/api/auth/stats", "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + AuthToken);

        Debug.Log($"Enviando request para: {backendUrl}/api/auth/stats");
        Debug.Log(
            $"Authorization header: Bearer {AuthToken.Substring(0, Mathf.Min(10, AuthToken.Length))}..."
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Estatísticas atualizadas com sucesso!");
            Debug.Log($"Resposta do servidor: {request.downloadHandler.text}");

            try
            {
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                GlobalScore = response.globalScore;
                GamesPlayed = response.gamesPlayed;
                GamesWon = response.gamesWon;
                Debug.Log(
                    $"Stats locais atualizadas: Score={GlobalScore}, Played={GamesPlayed}, Won={GamesWon}"
                );

                Debug.Log("Disparando evento OnStatsUpdated");
                OnStatsUpdated?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Erro ao fazer parse da resposta: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"Erro ao atualizar estatísticas: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode}");
            Debug.LogError($"Response Text: {request.downloadHandler.text}");
        }
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string token;
        public string userId;
        public string username;
        public int globalScore;
        public int gamesPlayed;
        public int gamesWon;
    }

    [System.Serializable]
    private class ProfileResponse
    {
        public string userId;
        public string username;
        public int globalScore;
        public int gamesPlayed;
        public int gamesWon;
    }

    [System.Serializable]
    private class ErrorResponse
    {
        public string error;
    }

    private string EncryptDecrypt(string data)
    {
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            result.Append((char)(data[i] ^ secretKey[i % secretKey.Length]));
        }

        return result.ToString();
    }

    public void LogOut()
    {
        Debug.Log("A iniciar o processo de logout...");

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Ficheiro de autenticação local apagado.");
        }

        AuthToken = null;
        UserId = null;
        Username = null;
        GlobalScore = 0;
        GamesPlayed = 0;
        GamesWon = 0;

        // Notificar que o utilizador mudou (logout)
        OnUserChanged?.Invoke();

        SceneManager.LoadScene("LoginMenu");
    }
}
