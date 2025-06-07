using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindMatchButton : MonoBehaviourPunCallbacks
{
    [SerializeField]
    TMP_Text buttonText;

    [SerializeField]
    WaitingSymbol waitingSymbol;

    [SerializeField]
    TimeCounter timeCounter;

    SimplePunLauncher punMatchmaking;

    Button thisButton;

    bool searching = false;


    void Start()
    {
        thisButton = GetComponent<Button>();

        if (punMatchmaking == null)
        {
            punMatchmaking = FindObjectOfType<SimplePunLauncher>();
            if (punMatchmaking == null)
                Debug.LogWarning("SimplePunLauncher não encontrado na cena!");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado ao Master Server.");

        // Só entra no lobby se não estiver já entrando ou dentro dele
        if (PhotonNetwork.NetworkClientState != ClientState.JoiningLobby && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("Tentando entrar no lobby...");
        }
        else
        {
            Debug.Log("Já está no lobby ou entrando.");
        }
    }

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
        if (punMatchmaking == null)
        {
            Debug.LogWarning("Não é possível procurar partida.");
            return;
        }

        searching = !searching;
        if (searching)
            StartMatchSearch();
        else
            StopMatchSearch();
    }

    void FixedUpdate()
    {
        if (PhotonNetwork.InLobby && !searching)
            thisButton.interactable = true;
        else if (!searching)
            thisButton.interactable = false;
    }
}
