using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    private string backendUrl = "https://tessellaction-backend.onrender.com";
    public static string AuthToken { get; private set; }
    public static string UserId { get; private set; }

    public void Login(string username, string password)
    {
        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        string jsonBody = $"{{\"username\": \"{username}\", \"password\": \"{password}\"}}";
        UnityWebRequest request = new UnityWebRequest(backendUrl + "/login", "POST");
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
            Debug.Log("Login bem-sucedido!");
        }
        else
        {
            Debug.LogError("Erro: " + request.error);
        }
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string token;
        public string userId;
        public int globalScore;
    }
}
