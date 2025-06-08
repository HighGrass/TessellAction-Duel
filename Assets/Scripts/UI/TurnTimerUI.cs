using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnTimerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject timerPanel;

    private TurnManager turnManager;
    private bool isInitialized = false;

    void Start()
    {
        // Encontrar o TurnManager na cena
        turnManager = FindObjectOfType<TurnManager>();

        if (turnManager == null)
        {
            Debug.LogWarning("TurnTimerUI: TurnManager not found in scene!");
            gameObject.SetActive(false);
            return;
        }

        isInitialized = true;

        // Inicializar UI
        if (timerPanel != null)
            timerPanel.SetActive(true);
    }

    void Update()
    {
        if (!isInitialized || turnManager == null)
            return;

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        // Só mostrar temporizador se o jogo estiver ativo
        if (!turnManager.IsGameActive)
        {
            if (timerPanel != null)
                timerPanel.SetActive(false);
            return;
        }

        if (timerPanel != null)
            timerPanel.SetActive(true);

        float timeRemaining = turnManager.CurrentTurnTimeRemaining;
        float maxTime = turnManager.turnTimeLimit;
        bool isMyTurn = turnManager.IsMyTurn;

        // Atualizar texto do temporizador
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            string playerIndicator = isMyTurn ? "Your turn" : "Opponent's turn";
            timerText.text = $"{playerIndicator}\n{seconds:00}s";
        }
    }
}
