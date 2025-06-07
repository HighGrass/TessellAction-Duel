using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FindMatchButton : MonoBehaviour
{
    [SerializeField]
    TMP_Text buttonText;

    [SerializeField]
    WaitingSymbol waitingSymbol;

    [SerializeField]
    TimeCounter timeCounter;

    [SerializeField]
    SimplePunLauncher punMatchmaking;

    bool searching = false;

    void StartMatchSearch()
    {
        buttonText.text = "Stop Searching";
        waitingSymbol.SetActive();
        timeCounter.StartCounter();

        punMatchmaking.FindMatch();
    }

    void StopMatchSearch()
    {
        buttonText.text = "Search Match";
        waitingSymbol.SetInactive();
        timeCounter.StopCounter();

        punMatchmaking.LeaveMatchmakingRoom();
    }

    public void ToggleMatchSearch()
    {
        if (punMatchmaking == null || !punMatchmaking.IsInLobby)
        {
            Debug.LogWarning("Não é possível procurar partida. Ainda não está no lobby.");
            return;
        }

        searching = !searching;
        if (searching)
            StartMatchSearch();
        else
            StopMatchSearch();
    }
}
