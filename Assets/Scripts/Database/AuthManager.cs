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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "auth.json");

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            string decryptedJson = EncryptDecrypt(json);
            SavedAuthData data = JsonUtility.FromJson<SavedAuthData>(decryptedJson);

            AuthToken = data.token;
            UserId = data.userId;

            StartCoroutine(VerifyTokenCoroutine());
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

                SceneManager.LoadScene("MatchmakingMenu");
            }
            else
            {
                Debug.LogError(
                    "Token era válido, mas falhou ao obter o perfil: " + profileRequest.error
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
            Debug.Log("Login bem-sucedido!");

            SavedAuthData dataToSave = new SavedAuthData { token = AuthToken, userId = UserId };
            string json = JsonUtility.ToJson(dataToSave);
            string encryptedJson = EncryptDecrypt(json);

            Debug.Log("A tentar guardar o ficheiro em: " + savePath);

            File.WriteAllText(savePath, encryptedJson);

            Debug.Log("Dados de login guardados. A carregar a cena de matchmaking...");
            SceneManager.LoadScene("MatchmakingMenu");
        }
        else
        {
            Debug.LogError("Erro: " + request.error);
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
            Debug.Log("Registo bem-sucedido! A fazer login automaticamente...");

            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            AuthToken = response.token;
            UserId = response.userId;
            Username = username;
            GlobalScore = response.globalScore;
            GamesPlayed = response.gamesPlayed;
            GamesWon = response.gamesWon;

            SavedAuthData dataToSave = new SavedAuthData { token = AuthToken, userId = UserId };
            string json = JsonUtility.ToJson(dataToSave);
            string encryptedJson = EncryptDecrypt(json);
            File.WriteAllText(savePath, encryptedJson);

            Debug.Log("Dados de login guardados. A carregar a cena de matchmaking...");
            SceneManager.LoadScene("MatchmakingMenu");
        }
        else
        {
            string responseText = request.downloadHandler.text;
            string errorMessage = "Erro desconhecido no registo.";

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);
                    errorMessage = "Erro no registo: " + errorResponse.error;
                }
                catch
                {
                    errorMessage = "Erro no registo: " + request.error + " | " + responseText;
                }
            }
            else
            {
                errorMessage = "Erro no registo: " + request.error;
            }
            Debug.LogError(errorMessage);
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

        SceneManager.LoadScene("LoginMenu");
    }
}
