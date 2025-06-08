using TMPro;
using UnityEngine;

public class UserInfoUI : MonoBehaviour
{
    [Header("UI Text Elements")]
    [SerializeField]
    private TMP_Text usernameText;

    [SerializeField]
    private TMP_Text scoreText;

    [SerializeField]
    private TMP_Text gamesPlayedText;

    [SerializeField]
    private TMP_Text winRateText;

    void Start()
    {
        Debug.Log($"[{(Application.isEditor ? "EDITOR" : "BUILD")}] UserInfoUI.Start() chamado");
        RefreshUI();
    }

    void OnEnable()
    {
        Debug.Log($"[{(Application.isEditor ? "EDITOR" : "BUILD")}] UserInfoUI.OnEnable() chamado");
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnStatsUpdated += RefreshUI;
        }

        RefreshUI();
        Invoke(nameof(RefreshUI), 0.1f);
    }

    void OnDisable()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnStatsUpdated -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
       
        if (string.IsNullOrEmpty(AuthManager.Username))
        {
            Debug.LogWarning("UserInfoUI: Dados do utilizador não encontrados.");
            if (usernameText != null)
                usernameText.gameObject.SetActive(false);
            if (scoreText != null)
                scoreText.gameObject.SetActive(false);
            if (gamesPlayedText != null)
                gamesPlayedText.gameObject.SetActive(false);
            if (winRateText != null)
                winRateText.gameObject.SetActive(false);
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (usernameText != null)
            usernameText.text = $"User: {AuthManager.Username}";

        if (scoreText != null)
            scoreText.text = $"Score: {AuthManager.GlobalScore}";

        if (gamesPlayedText != null)
            gamesPlayedText.text = $"Matches played: {AuthManager.GamesPlayed}";

        if (winRateText != null)
        {
            float winRatePercent = 0f;

            if (AuthManager.GamesPlayed > 0)
            {
                winRatePercent = ((float)AuthManager.GamesWon / AuthManager.GamesPlayed) * 100f;
            }

            winRateText.text = $"Win rate: {Mathf.FloorToInt(winRatePercent)}%";
        }

        Debug.Log($"UI atualizada - Score: {AuthManager.GlobalScore}, Played: {AuthManager.GamesPlayed}, Won: {AuthManager.GamesWon}");
    }
}
