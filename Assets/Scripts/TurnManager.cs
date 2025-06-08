using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TurnManager : MonoBehaviourPunCallbacks
{
    public int CurrentTurnIndex { get; private set; } = -1;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Start()
    {
        // Apenas o MasterClient decide o estado inicial.
        if (PhotonNetwork.IsMasterClient)
        {
            // MasterClient define o primeiro turno e ordena a todos que sincronizem.
            int firstTurn = PhotonNetwork.MasterClient.ActorNumber;
            photonView.RPC("SyncTurnRpc", RpcTarget.All, firstTurn);
        }
    }

    [PunRPC]
    private void SyncTurnRpc(int newTurnIndex)
    {
        CurrentTurnIndex = newTurnIndex;
        Debug.Log($"--- TURNO ATUALIZADO PARA JOGADOR: {CurrentTurnIndex} ---");
    }

    [PunRPC]
    private int GetPlayerPieces(int playerActorNumber) =>
        FindObjectsOfType<Piece>().Count(p => p.OwnerId == playerActorNumber && p.IsInteractable);

    [PunRPC]
    private void ProcessGameResultRpc(int winnerActorNumber)
    {
        Debug.Log($"ProcessGameResultRpc chamado. Vencedor: {winnerActorNumber}, Jogador Local: {PhotonNetwork.LocalPlayer.ActorNumber}");

        // Verificar se o AuthManager está disponível
        if (AuthManager.Instance != null)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNumber)
            {
                // Jogador que venceu ganha 50 pontos
                Debug.Log("Enviando resultado: WIN com 50 pontos");
                AuthManager.Instance.EnviarResultadoDeJogo("win", 50);
            }
            else
            {
                // Jogador que perdeu perde 25 pontos
                Debug.Log("Enviando resultado: LOSE com -25 pontos");
                AuthManager.Instance.EnviarResultadoDeJogo("lose", -25);
            }
        }
        else
        {
            Debug.LogError("AuthManager.Instance é null! Não é possível enviar resultado do jogo.");
        }
    }

    [PunRPC]
    private void GameOverRpc()
    {
        Debug.Log("Fim de Jogo! A iniciar o processo de saída da sala...");
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("MatchmakingMenu");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Saída da sala confirmada. A carregar o menu...");
        SceneManager.LoadScene("MatchmakingMenu");
    }

    [PunRPC]
    private void RequestMoveRpc(
        int movingPieceViewID,
        int destinationPieceViewID,
        PhotonMessageInfo info
    )
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (info.Sender.ActorNumber != CurrentTurnIndex)
            return;

        Piece movingPiece = PhotonView.Find(movingPieceViewID)?.GetComponent<Piece>();
        Piece destinationPiece = PhotonView.Find(destinationPieceViewID)?.GetComponent<Piece>();

        if (movingPiece == null || destinationPiece == null)
            return;
        if (movingPiece.OwnerId != info.Sender.ActorNumber)
            return;
        if (!movingPiece.GetPossibleMoves().Contains(destinationPiece))
            return;

        float moveDistance = Vector3.Distance(
            movingPiece.CurrentPosition,
            destinationPiece.CurrentPosition
        );

        if (moveDistance > 2.5f)
        {
            Piece capturedPiece = null;
            Vector3 moveDirection = (
                destinationPiece.CurrentPosition - movingPiece.CurrentPosition
            ).normalized;
            foreach (Piece neighbor in movingPiece.Neighbors)
            {
                if (neighbor.OwnerId != -1 && neighbor.OwnerId != movingPiece.OwnerId)
                {
                    Vector3 neighborDirection = (
                        neighbor.CurrentPosition - movingPiece.CurrentPosition
                    ).normalized;
                    if (Vector3.Dot(moveDirection, neighborDirection) > 0.95f)
                    {
                        capturedPiece = neighbor;
                        break;
                    }
                }
            }

            if (capturedPiece != null)
            {
                int opponentId = capturedPiece.OwnerId;
                int opponentPieceCount = GetPlayerPieces(opponentId);

                capturedPiece.SetOwnerState(info.Sender.ActorNumber, false);

                if (opponentPieceCount <= 1)
                {
                    Debug.Log($"FIM DE JOGO! Jogador {info.Sender.ActorNumber} venceu!");

                    // Enviar RPC para todos os jogadores processarem o resultado
                    photonView.RPC("ProcessGameResultRpc", RpcTarget.All, info.Sender.ActorNumber);
                    photonView.RPC("GameOverRpc", RpcTarget.All);
                    return;
                }
            }
        }

        destinationPiece.SetOwnerState(info.Sender.ActorNumber, true);
        movingPiece.SetOwnerState(info.Sender.ActorNumber, false);

        var otherPlayer = PhotonNetwork.CurrentRoom.Players.Values.FirstOrDefault(p =>
            p.ActorNumber != CurrentTurnIndex
        );
        int nextTurnIndex =
            (otherPlayer != null) ? otherPlayer.ActorNumber : info.Sender.ActorNumber;
        photonView.RPC("SyncTurnRpc", RpcTarget.All, nextTurnIndex);
    }
}
