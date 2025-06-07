using System.Collections.Generic;
using System.Linq;
using Photon.Pun; 
using UnityEngine;

public class Player : MonoBehaviourPun
{
    private Camera mainCamera;
    private TurnManager turnManager;

    private Piece selectedPiece;
    private Piece hoveredPiece;

    public Piece[] PossibleMoves { get; set; }
    public int LocalPlayerActorNumber => PhotonNetwork.LocalPlayer.ActorNumber;

    void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();

        if (!photonView.IsMine)
        {
            if (mainCamera != null)
                mainCamera.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        turnManager = FindObjectOfType<TurnManager>();
    }

    void Update()
    {
        if (!photonView.IsMine || turnManager == null)
            return;

        if (turnManager.CurrentTurnIndex != LocalPlayerActorNumber)
        {
            if (selectedPiece != null)
                DeselectCurrentPiece();
            if (hoveredPiece != null)
            {
                hoveredPiece.SetHovered(false);
                hoveredPiece = null;
            }
            return;
        }

        HandleHover();
        HandleClick();
    }

    private void HandleHover()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit hit);
        Piece pieceUnderMouse = hit.collider?.GetComponent<Piece>();

        // Limpa o hover da peça anterior
        if (hoveredPiece != null && hoveredPiece != pieceUnderMouse)
        {
            hoveredPiece.SetHovered(false);
            hoveredPiece = null;
        }

        
        Piece pieceToHover = null;
        if (selectedPiece != null) // Modo de Movimento
        {
            // 
            if (
                pieceUnderMouse != null
                && PossibleMoves != null
                && PossibleMoves.Contains(pieceUnderMouse)
            )
            {
                pieceToHover = pieceUnderMouse;
            }
        }
        else // Modo de Seleção
        {
            if (
                pieceUnderMouse != null
                && pieceUnderMouse.OwnerId == LocalPlayerActorNumber
                && pieceUnderMouse.IsInteractable
            )
            {
                pieceToHover = pieceUnderMouse;
            }
        }

        if (pieceToHover != null)
        {
            hoveredPiece = pieceToHover;
            hoveredPiece.SetHovered(true);
        }
    }
    private void HandleClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit hit);
        Piece clickedPiece = hit.collider?.GetComponent<Piece>();

        // Nenhuma peça selecionada.
        if (selectedPiece == null)
        {
            // Clicou numa peça sua e válida para a selecionar.
            if (clickedPiece != null && clickedPiece.OwnerId == LocalPlayerActorNumber && clickedPiece.IsInteractable)
            {
                SelectNewPiece(clickedPiece);
            }
        }
        // Uma peça já está selecionada.
        else
        {
            if (clickedPiece != null && PossibleMoves != null && PossibleMoves.Contains(clickedPiece))

                AttemptMove(selectedPiece, clickedPiece);

            else if (clickedPiece != null && clickedPiece != selectedPiece && clickedPiece.OwnerId == LocalPlayerActorNumber && clickedPiece.IsInteractable)

                SelectNewPiece(clickedPiece);

            else

                DeselectCurrentPiece();

        }
    }

    private void SelectNewPiece(Piece piece)
    {
        // Limpa a seleção anterior antes de selecionar a nova.
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
        }

        selectedPiece = piece;
        selectedPiece.SetSelected(true);
    }

    private void DeselectCurrentPiece()
    {
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false); // Isto vai limpar os destaques
            selectedPiece = null;
            PossibleMoves = null;
        }
    }

    private void AttemptMove(Piece from, Piece to)
    {
        // Envia o pedido ao MasterClient para executar a jogada.
        turnManager.photonView.RPC(
            "RequestMoveRpc",
            RpcTarget.MasterClient,
            from.photonView.ViewID,
            to.photonView.ViewID
        );

        // Limpa o estado local imediatamente para dar feedback.
        DeselectCurrentPiece();
    }
}
