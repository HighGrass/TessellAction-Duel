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
            usernameText.text = $"Utilizador: {AuthManager.Username}";

        if (scoreText != null)
            scoreText.text = $"Pontuação Global: {AuthManager.GlobalScore}";

        if (gamesPlayedText != null)
            gamesPlayedText.text = $"Jogos Jogados: {AuthManager.GamesPlayed}";

        if (winRateText != null)
        {
            float winRatePercent = 0f;
            
            if (AuthManager.GamesPlayed > 0)
            {
                winRatePercent = ((float)AuthManager.GamesWon / AuthManager.GamesPlayed) * 100f;
            }

            winRateText.text = $"Taxa de Vitória: {Mathf.FloorToInt(winRatePercent)}%";
        }
    }
}
