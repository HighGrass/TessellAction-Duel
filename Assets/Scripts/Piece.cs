using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class Piece : NetworkBehaviour
{
    private Vector3 originalPosition;
    private bool isHovered = false;
    private bool isSelected = false;
    public bool IsSelected => isSelected;
    private Material defaultMaterial;

    [SerializeField]
    private int ownerId = -1;
    private bool isInteractable = false;
    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    private List<Piece> neighbors = new List<Piece>();
    public List<Piece> Neighbors => neighbors;

    TurnManager turnManager;
    private Renderer pieceRenderer;

    [Networked]
    private Vector3 netPosition { get; set; }

    [Networked]
    private int MaterialIndex { get; set; }

    public int OwnerId
    {
        get => ownerId;
        set => ownerId = value;
    }

    public override void Spawned()
    {
        originalPosition = transform.position;
        pieceRenderer = GetComponentInChildren<Renderer>();
        defaultMaterial = pieceRenderer.material;

        turnManager = FindObjectOfType<TurnManager>();

        if (ownerId > -1)
            isInteractable = true;

        neighbors = Physics
            .OverlapSphere(transform.position, 1f, LayerMask.GetMask("Piece"))
            .Select(hit => hit.GetComponent<Piece>())
            .Where(piece => piece != null && piece != this)
            .ToList();

        // Sync initial networked position
        if (Object.HasStateAuthority)
            netPosition = transform.position;

        SetCorrectColor();
    }

    public override void FixedUpdateNetwork()
    {
        transform.position = netPosition;

        // Apply material if index is valid
        if (MaterialIndex >= 0 && MaterialIndex < PlayerMaterials.PiecesMaterials.Count)
        {
            pieceRenderer.material = PlayerMaterials.PiecesMaterials[MaterialIndex];
        }
    }

    public void SetHovered(bool hovered)
    {
        if (isSelected || !isInteractable)
            return;

        if (hovered == isHovered)
            return;

        isHovered = hovered;
        Vector3 targetPosition = originalPosition + (isHovered ? Vector3.up * 0.2f : Vector3.zero);
        RequestMoveServerRpc(targetPosition);
    }

    public void SetSelected(bool selected)
    {
        if (!isInteractable || selected == isSelected)
            return;

        isSelected = selected;
        Vector3 targetPosition = originalPosition + (isSelected ? Vector3.up * 0.5f : Vector3.zero);
        RequestMoveServerRpc(targetPosition);

        Player currentPlayer = FindObjectsOfType<Player>()[turnManager.CurrentTurnIndex];

        foreach (Piece piece in GetPossibleMoves())
        {
            if (isSelected)
            {
                piece.HighlightPiece(true);
                currentPlayer.PossibleMoves = GetPossibleMoves().ToArray();
            }
            else
            {
                piece.HighlightPiece(false);
                currentPlayer.PossibleMoves = null;
            }
        }
    }

    public List<Piece> GetPossibleMoves()
    {
        List<Piece> possibleMoves = new();

        foreach (Piece piece in neighbors)
        {
            if (piece.OwnerId < 0)
                possibleMoves.Add(piece);
            else if (piece.OwnerId != OwnerId)
            {
                Vector3 diff = piece.originalPosition - originalPosition;
                var backPieces = FindPiecesByPosition(
                        piece.originalPosition + diff + new Vector3(0, 0.5f, 0)
                    )
                    .Where(p => p.OwnerId < 0)
                    .ToList();

                possibleMoves.AddRange(backPieces);
            }
        }

        Debug.Log($"{possibleMoves.Count} possible moves");
        return possibleMoves;
    }

    private List<Piece> FindPiecesByPosition(Vector3 pos, float radius = 0.8f)
    {
        return Physics
            .OverlapSphere(pos, radius, LayerMask.GetMask("Piece"))
            .Select(hit => hit.GetComponent<Piece>())
            .Where(p => p != null && p != this)
            .ToList();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RequestMoveServerRpc(Vector3 newPosition)
    {
        if (!Object.HasStateAuthority)
            return;

        netPosition = newPosition;
    }

    public void HighlightPiece(bool highlight)
    {
        ChangeServerMaterial(
            highlight ? PlayerMaterials.PossibleMoveMaterial : GetCorrectMaterial()
        );
    }

    private void ChangeServerMaterial(Material material)
    {
        int index = PlayerMaterials.PiecesMaterials.IndexOf(material);
        if (index >= 0)
            MaterialIndex = index;
    }

    public void SetCorrectColor()
    {
        ChangeServerMaterial(GetCorrectMaterial());
    }

    private Material GetCorrectMaterial()
    {
        if (OwnerId == 0)
            return IsInteractable
                ? PlayerMaterials.RedPlayerMaterial
                : PlayerMaterials.RedPlayerInactiveMaterial;
        else if (OwnerId == 1)
            return IsInteractable
                ? PlayerMaterials.BluePlayerMaterial
                : PlayerMaterials.BluePlayerInactiveMaterial;
        else
            return defaultMaterial;
    }
}
