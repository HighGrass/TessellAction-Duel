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
        CurrentTurnIndex = newTurnIndex;
        Debug.Log($"--- TURNO ATUALIZADO PARA JOGADOR: {CurrentTurnIndex} ---");
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
                capturedPiece.SetOwnerState(info.Sender.ActorNumber, false);
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
