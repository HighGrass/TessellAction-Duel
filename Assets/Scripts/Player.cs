using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private Piece hoveredPiece;
    private Camera mainCamera;
    private TurnManager turnManager;

    private Piece selectedPiece = null;
    private Piece[] possibleMoves = null;
    public Piece[] PossibleMoves
    {
        get => possibleMoves;
        set => possibleMoves = value;
    }

    public int ThisPlayerID => (int)NetworkManager.Singleton.LocalClientId;

    public override void OnNetworkSpawn()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera?.gameObject.SetActive(true);
    }

    void Start()
    {
        turnManager = FindObjectOfType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogError("TurnManager not found in the scene.");
            return;
        }
    }

    void Update()
    {
        if (!IsOwner)
            return; // Only the owner can control the camera

        if (turnManager.CurrentTurnIndex != (int)NetworkManager.Singleton.LocalClientId)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var piece = hit.collider.GetComponent<Piece>();
            if (piece != null)
            {
                if (piece != hoveredPiece && hoveredPiece != null)
                {
                    hoveredPiece.SetHovered(false);
                }

                if (possibleMoves != null && possibleMoves.Contains(piece)) { }
                else if (piece.OwnerId != ThisPlayerID)
                    return;

                piece.SetHovered(true);
                hoveredPiece = piece;

                // Select and deselect any piece
                if (Input.GetMouseButtonDown(0))
                {
                    OnClickPiece(piece);
                }
            }
            else if (hoveredPiece != null)
            {
                hoveredPiece.SetHovered(false);
                hoveredPiece = null;
            }
        }
        else if (hoveredPiece != null)
        {
            hoveredPiece.SetHovered(false);
            hoveredPiece = null;
        }
    }

    private void OnClickPiece(Piece piece)
    {
        if (selectedPiece != null && !piece.IsInteractable)
        { // move selected piece
            MoveSelectedPiece(piece);
            return;
        }
        else if (
            piece.OwnerId == (int)NetworkManager.Singleton.LocalClientId
            && piece.IsInteractable
        )
        { // select piece
            bool wasSelected = SelectPiece(piece);
            if (!wasSelected)
                Debug.LogError("Piece could not be selected");
            return;
        }
        return;
    }

    private bool SelectPiece(Piece piece)
    {
        if (
            piece.IsInteractable // the clicked piece can be selected
            && piece.OwnerId == ThisPlayerID // the clicked piece is owned by the player
        )
        { // can select piece
            if (selectedPiece != null)
                // is this piece already selected ?
                if (selectedPiece != piece)
                    // deselect the previously selected piece
                    DeselectPiece();
                else
                // deselect the selected piece
                {
                    DeselectPiece();
                    return false;
                }

            piece.SetSelected(true);
            selectedPiece = piece;
            return true;
        }

        return false;
    }

    private bool DeselectPiece()
    {
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
            selectedPiece.SetCorrectColor();
            selectedPiece = null;
            return true;
        }
        return false;
    }

    private bool MoveSelectedPiece(Piece newPiecePosition)
    {
        if (selectedPiece == null)
            return false;
        selectedPiece.IsInteractable = false;
        DeselectPiece();

        ChangePieceOwner(ThisPlayerID, newPiecePosition, true);

        return true;
    }

    private void ChangePieceOwner(int ownerId, Piece piece, bool setInteractable = false)
    {
        piece.OwnerId = ThisPlayerID;
        piece.IsInteractable = setInteractable;
        piece.SetCorrectColor();
    }
}
