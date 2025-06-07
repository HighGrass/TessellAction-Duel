using System.Collections;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
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
        UnityWebRequest request = new UnityWebRequest(backendUrl + "/api/auth/verify", "POST");
        request.SetRequestHeader("Authorization", "Bearer " + AuthToken);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token válido. Login automático bem-sucedido!");
            SceneManager.LoadScene("MatchmakingMenu");
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
}
