// TurnManager.cs
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class TurnManager : MonoBehaviourPun
{
    public int CurrentTurnIndex { get; private set; } = -1;

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
        // Este método é chamado em TODOS os clientes.
        CurrentTurnIndex = newTurnIndex;
        Debug.Log($"--- TURNO ATUALIZADO PARA JOGADOR: {CurrentTurnIndex} ---");
    }

    [PunRPC]
    private void RequestMoveRpc(int movingPieceViewID, int destinationPieceViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (info.Sender.ActorNumber != CurrentTurnIndex) return;

        Piece movingPiece = PhotonView.Find(movingPieceViewID)?.GetComponent<Piece>();
        Piece destinationPiece = PhotonView.Find(destinationPieceViewID)?.GetComponent<Piece>();

        if (movingPiece == null || destinationPiece == null) return;
        if (movingPiece.OwnerId != info.Sender.ActorNumber) return;
        if (!movingPiece.GetPossibleMoves().Contains(destinationPiece)) return;
        
        // --- EXECUÇÃO DA JOGADA ---
        float moveDistance = Vector3.Distance(movingPiece.transform.position, destinationPiece.transform.position);

        if (moveDistance > 1.5f)
        {
             Vector3 midpoint = (movingPiece.transform.position + destinationPiece.transform.position) / 2f;
            
              Collider[] hits = Physics.OverlapSphere(midpoint, 0.5f, LayerMask.GetMask("Piece"));
            Piece capturedPiece = hits
                .Select(h => h.GetComponent<Piece>())
                .FirstOrDefault(p => p != null && p != movingPiece && p != destinationPiece);

            if (capturedPiece != null && capturedPiece.OwnerId != -1 && capturedPiece.OwnerId != info.Sender.ActorNumber)
            {
                // A peça do inimigo torna-se sua e inativa.
                capturedPiece.SetOwnerState(info.Sender.ActorNumber, false);
            }
        }

        // A peça de destino torna-se sua e interativa.
        destinationPiece.SetOwnerState(info.Sender.ActorNumber, true);
        
        // A peça de origem torna-se sua e inativa.
        movingPiece.SetOwnerState(info.Sender.ActorNumber, false);

        var otherPlayer = PhotonNetwork.CurrentRoom.Players.Values.FirstOrDefault(p => p.ActorNumber != CurrentTurnIndex);
        int nextTurnIndex = (otherPlayer != null) ? otherPlayer.ActorNumber : info.Sender.ActorNumber;
        photonView.RPC("SyncTurnRpc", RpcTarget.All, nextTurnIndex);
    }
}
