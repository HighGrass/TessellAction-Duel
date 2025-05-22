using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Piece : NetworkBehaviour
{
    private Vector3 originalPosition;
    private bool isHovered = false;
    private bool isSelected = false;
    public bool IsSelected => isSelected;
    private Material defaultMaterial;

    [SerializeField]
    private int ownerId = -1; // No owner by default
    private bool isInteractable = false;
    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    private List<Piece> neighbors = new List<Piece>();
    public List<Piece> Neighbors => neighbors;
    TurnManager turnManager;

    public int OwnerId
    {
        get => ownerId;
        set { ownerId = value; }
    }

    void Start()
    {
        turnManager = FindObjectOfType<TurnManager>();
        defaultMaterial = GetComponentInChildren<Renderer>().material;
        OwnerId = ownerId; // Initialize values set on editor

        if (ownerId > -1)
            isInteractable = true; // All initial player pieces are interactable

        neighbors = Physics
            .OverlapSphere(transform.position, 1, LayerMask.GetMask("Piece"))
            .Select(hit => hit.GetComponent<Piece>())
            .Where(piece => piece != null && piece != this)
            .ToList();
    }

    // Networked position variable
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );

    private NetworkVariable<Material> netMaterial = new NetworkVariable<Material>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    private Renderer renderer;

    public override void OnNetworkSpawn()
    {
        originalPosition = transform.position;
        renderer = GetComponent<Renderer>();

        // Sync initial position
        if (IsServer)
            netPosition.Value = originalPosition;

        netPosition.OnValueChanged += (oldPos, newPos) =>
        {
            transform.position = newPos;
        };
        netMaterial.OnValueChanged += (oldMat, newMat) =>
        {
            renderer.material = newMat;
        };

        SetCorrectColor();
    }

    public void SetHovered(bool hovered)
    {
        if (isSelected)
            return;

        if (hovered == isHovered)
            return;
        isHovered = hovered;
        Vector3 targetPosition = originalPosition + (isHovered ? Vector3.up * 0.2f : Vector3.zero);
        RequestMoveServerRpc(targetPosition);
    }

    public void SetSelected(bool selected)
    {
        if (selected == isSelected)
            return;
        isSelected = selected;
        Vector3 targetPosition = originalPosition + (isSelected ? Vector3.up * 0.5f : Vector3.zero);
        RequestMoveServerRpc(targetPosition);

        Player[] players = FindObjectsOfType<Player>();

        Player currentPlayer = players[turnManager.CurrentTurnIndex];

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
        List<Piece> possibleMoves = new List<Piece>();
        foreach (Piece piece in neighbors)
        {
            if (piece.OwnerId < 0)
                possibleMoves.Add(piece);
            else if (piece.OwnerId > -1 && piece.OwnerId != OwnerId) // Enemy piece
            {
                Vector3 difference = piece.originalPosition - originalPosition;
                Piece[] backPieces = FindPiecesByPosition(
                        piece.originalPosition + difference + new Vector3(0, 0.5f, 0)
                    // offset vector to make sure we collide with the back piece
                    )
                    .Where(piece => piece.OwnerId < 0)
                    .ToArray(); // should always be 1 only

                foreach (Piece backPiece in backPieces)
                    possibleMoves.Add(backPiece);
            }
        }
        Debug.Log(possibleMoves.Count + " possible moves");
        return possibleMoves;
    }

    private List<Piece> FindPiecesByPosition(Vector3 pos, float radius = 0.8f)
    {
        Collider[] hitColliders = Physics.OverlapSphere(pos, radius, LayerMask.GetMask("Piece"));
        return hitColliders
            .Select(hit => hit.GetComponent<Piece>())
            .Where(piece => piece != null && piece != this)
            .ToList();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector3 targetPosition, ServerRpcParams rpcParams = default)
    {
        // Server updates the networked position
        netPosition.Value = targetPosition;
    }

    private void ChangeServerMaterial(Material material)
    {
        // Server updates the networked material
        int materialIndex = PlayerMaterials.PiecesMaterials.FindIndex(mat => mat == material);
        Debug.LogWarning("materialIndex: " + materialIndex);
        RequestMaterialServerRpc(materialIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMaterialServerRpc(int materialId, ServerRpcParams rpcParams = default)
    {
        // Server updates the networked material
        netMaterial.Value = PlayerMaterials.RedPlayerMaterial;
    }

    public void HighlightPiece(bool highlight)
    {
        if (highlight)
            ChangeServerMaterial(PlayerMaterials.PossibleMoveMaterial);
        else
            SetCorrectColor();
    }

    public void SetCorrectColor()
    {
        Material newMaterial;
        if (OwnerId == 0)
            if (IsInteractable)
                newMaterial = PlayerMaterials.RedPlayerMaterial;
            else
                newMaterial = PlayerMaterials.RedPlayerInactiveMaterial;
        else if (OwnerId == 1)
            if (IsInteractable)
                newMaterial = PlayerMaterials.BluePlayerMaterial;
            else
                newMaterial = PlayerMaterials.BluePlayerInactiveMaterial;
        else
            newMaterial = defaultMaterial; // Default color

        ChangeServerMaterial(newMaterial);
    }
}
