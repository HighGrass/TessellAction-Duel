using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public enum InitialOwnerType
{
    None,
    Player1_MasterClient,
    Player2_Client,
}

public class Piece : MonoBehaviourPun, IPunObservable
{
    [Header("Initial Setup")]
    [Tooltip(
        "Define o dono inicial desta peça. Player1 é o MasterClient, Player2 é o segundo jogador."
    )]
    [SerializeField]
    private InitialOwnerType initialOwner = InitialOwnerType.None;

    [Tooltip("Define se a peça começa como interativa (se tiver um dono).")]
    [SerializeField]
    private bool startsInteractable = true;

    public InitialOwnerType InitialOwner => initialOwner;
    public bool StartsInteractable => startsInteractable;

    // --- Propriedades Locais e de Referência ---
    private Vector3 visualOffsetPosition;
    private bool isHovered = false;
    private bool isSelected = false;
    public bool IsSelected => isSelected;

    private Material defaultMaterial;
    private Renderer pieceRenderer;
    private List<Piece> neighbors = new List<Piece>();
    public List<Piece> Neighbors => neighbors;

    public Vector3 CurrentPosition { get; private set; }
    private int ownerIdInternal = -1;
    private bool isInteractableInternal = false;
    private int currentMaterialID = -1;

    public int OwnerId => ownerIdInternal;
    public bool IsInteractable => isInteractableInternal;

    void Awake()
    {
        pieceRenderer = GetComponentInChildren<Renderer>();
        if (pieceRenderer != null)
        {
            defaultMaterial = pieceRenderer.material; // Guarda o material original como default
        }
        else
        {
            Debug.LogError($"Renderer não encontrado na peça: {gameObject.name}", this);
        }
        CurrentPosition = transform.position; // Define a posição inicial
    }

    void Start()
    {
        // Apenas o MasterClient tem a autoridade para definir o estado inicial do tabuleiro.
        if (PhotonNetwork.IsMasterClient)
        {
            // O MasterClient lê a configuração do editor e a transmite para todos.
            int targetOwnerId = -1;
            bool isInteractable = this.startsInteractable;

            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
            {
                Photon.Realtime.Player otherPlayer = PhotonNetwork.PlayerListOthers[0];
                switch (initialOwner)
                {
                    case InitialOwnerType.Player1_MasterClient:
                        targetOwnerId = PhotonNetwork.MasterClient.ActorNumber;
                        break;
                    case InitialOwnerType.Player2_Client:
                        targetOwnerId = otherPlayer.ActorNumber;
                        break;
                    case InitialOwnerType.None:
                    default:
                        targetOwnerId = -1;
                        isInteractable = false;
                        break;
                }
            }
            else
            {
                targetOwnerId = -1;
                isInteractable = false;
            }

            Debug.Log(
                $"MasterClient a configurar a peça {gameObject.name}. Dono alvo: {targetOwnerId}"
            );
            SetOwnerState(targetOwnerId, isInteractable);
        }

        if (photonView.IsMine)
        {
            CurrentPosition = transform.position;
        }
        CalculateNeighbors();
        ApplyMaterial(); // Aplica o material inicial com base no estado sincronizado
    }

    void CalculateNeighbors()
    {
        neighbors = Physics
            .OverlapSphere(transform.position, 1.5f, LayerMask.GetMask("Piece"))
            .Select(hit => hit.GetComponent<Piece>())
            .Where(p => p != null && p != this)
            .ToList();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Só o proprietário da PhotonView envia os dados
            stream.SendNext(CurrentPosition);
            stream.SendNext(ownerIdInternal);
            stream.SendNext(isInteractableInternal);
            stream.SendNext(currentMaterialID);
        }
        else
        {
            // Outros clientes recebem os dados
            CurrentPosition = (Vector3)stream.ReceiveNext();
            ownerIdInternal = (int)stream.ReceiveNext();
            isInteractableInternal = (bool)stream.ReceiveNext();
            currentMaterialID = (int)stream.ReceiveNext();

            ApplyMaterial(); // Aplica o material com base nos dados recebidos
        }
    }

    void Update()
    {
        if (photonView == null)
        {
            Debug.LogError($"PhotonView é NULO no GameObject: {gameObject.name}.");
            enabled = false;
            return;
        }

        Vector3 targetPosition = CurrentPosition + visualOffsetPosition;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
    }

    public void SetHovered(bool hovered)
    {
        if (isSelected || this.isHovered == hovered)
            return;
        this.isHovered = hovered;
        visualOffsetPosition = hovered ? Vector3.up * 0.2f : Vector3.zero;
    }

    public void SetSelected(bool selected)
    {
        if (this.isSelected == selected)
            return;
        this.isSelected = selected;
        isHovered = false; // Seleção cancela o hover

        if (selected)
        {
            visualOffsetPosition = Vector3.up * 0.4f;
            Player localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                ClearPossibleMoveHighlights(localPlayer);
                localPlayer.PossibleMoves = GetPossibleMoves().ToArray();
                HighlightPossibleMoves(localPlayer, true);
            }
        }
        else
        {
            visualOffsetPosition = Vector3.zero;
            Player localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                HighlightPossibleMoves(localPlayer, false);
                localPlayer.PossibleMoves = null;
            }
        }
    }

    private Player GetLocalPlayer() =>
        FindObjectsOfType<Player>().FirstOrDefault(p => p.photonView.IsMine);

    private void ClearPossibleMoveHighlights(Player player)
    {
        if (player.PossibleMoves != null)
        {
            foreach (Piece move in player.PossibleMoves)
            {
                if (move != null)
                    move.HighlightPieceVisual(false);
            }
        }
    }

    private void HighlightPossibleMoves(Player player, bool highlight)
    {
        if (player.PossibleMoves != null)
        {
            foreach (Piece move in player.PossibleMoves)
            {
                if (move != null)
                    move.HighlightPieceVisual(highlight);
            }
        }
    }

    public void HighlightPieceVisual(bool state)
    {
        if (pieceRenderer == null)
            return;

        if (state)
        {
            pieceRenderer.material = PlayerMaterials.PossibleMoveMaterial;
        }
        else
        {
            ApplyMaterial();
        }
    }

    public List<Piece> GetPossibleMoves()
    {
        List<Piece> possibleMoves = new List<Piece>();
        CalculateNeighbors();

        foreach (Piece neighbor in neighbors)
        {
            if (neighbor.OwnerId == -1 || !neighbor.IsInteractable)
            {
                possibleMoves.Add(neighbor);
            }
            // Lógica de Salto (Captura)
            else if (neighbor.OwnerId != this.OwnerId)
            {
                Vector3 jumpDirection = (
                    neighbor.CurrentPosition - this.CurrentPosition
                ).normalized;

                foreach (Piece landingSpot in neighbor.Neighbors)
                {
                    Vector3 landingDirection = (
                        landingSpot.CurrentPosition - neighbor.CurrentPosition
                    ).normalized;

                    if (
                        Vector3.Dot(jumpDirection, landingDirection) > 0.95f
                        && (landingSpot.OwnerId == -1 || !landingSpot.IsInteractable)
                    )
                    {
                        possibleMoves.Add(landingSpot);
                    }
                }
            }
        }
        return possibleMoves;
    }

    // --- RPCs para Modificar Estado Sincronizado ---

    public void RequestMove(Vector3 newTargetPosition)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("ExecuteMoveRpc", RpcTarget.All, newTargetPosition);
        }
    }

    [PunRPC]
    private void ExecuteMoveRpc(Vector3 newPosition, PhotonMessageInfo info)
    {
        CurrentPosition = newPosition;
        visualOffsetPosition = Vector3.zero; // Reseta offset visual após movimento

        if (photonView.IsMine)
        {
            transform.position = CurrentPosition;
        }

        Debug.Log(
            $"Peça {photonView.ViewID} movida para {newPosition} por Actor:{info.Sender?.ActorNumber}"
        );
    }

    public void SetOwnerState(int newOwnerActorNumber, bool newInteractable)
    {
        int newMaterialId = CalculateMaterialID(newOwnerActorNumber, newInteractable);
        photonView.RPC(
            "UpdateOwnerStateRpc",
            RpcTarget.All,
            newOwnerActorNumber,
            newInteractable,
            newMaterialId
        );
    }

    [PunRPC]
    private void UpdateOwnerStateRpc(
        int newOwner,
        bool newInteractable,
        int newMatId,
        PhotonMessageInfo info
    )
    {
        Debug.Log(
            $"Peça {photonView.ViewID}: UpdateOwnerStateRpc de {info.Sender?.ActorNumber}. Novo Dono: {newOwner}, Interativo: {newInteractable}, MatID: {newMatId}"
        );

        bool oldOwnerWasMine = photonView.IsMine;

        ownerIdInternal = newOwner;
        isInteractableInternal = newInteractable;
        currentMaterialID = newMatId;

        ApplyMaterial();

        if (PhotonNetwork.IsMasterClient || oldOwnerWasMine)
        {
            if (newOwner != -1)
            {
                Photon.Realtime.Player newOwnerPlayer = PhotonNetwork.CurrentRoom.GetPlayer(
                    newOwner
                );
                if (newOwnerPlayer != null)
                {
                    if (photonView.ControllerActorNr != newOwnerPlayer.ActorNumber)
                    {
                        photonView.TransferOwnership(newOwnerPlayer);
                        Debug.Log(
                            $"Peça {photonView.ViewID}: Propriedade transferida para ActorNr {newOwner} por {PhotonNetwork.LocalPlayer.ActorNumber}"
                        );
                    }
                }
                else // Novo dono não encontrado
                {
                    Debug.LogWarning(
                        $"Peça {photonView.ViewID}: Novo dono ActorNr {newOwner} não encontrado. Tentando dar ao MasterClient."
                    );
                    if (
                        PhotonNetwork.MasterClient != null
                        && photonView.ControllerActorNr != PhotonNetwork.MasterClient.ActorNumber
                    )
                    {
                        photonView.TransferOwnership(PhotonNetwork.MasterClient);
                    }
                }
            }
            else // Peça torna-se neutra (ownerId = -1)
            {
                if (
                    PhotonNetwork.MasterClient != null
                    && photonView.ControllerActorNr != PhotonNetwork.MasterClient.ActorNumber
                )
                {
                    photonView.TransferOwnership(PhotonNetwork.MasterClient); // MasterClient assume controlo de peças neutras
                    Debug.Log(
                        $"Peça {photonView.ViewID}: Tornou-se neutra. Propriedade para MasterClient."
                    );
                }
            }
        }
    }

    private int CalculateMaterialID(int owner, bool interactable)
    {
        if (owner == -1)
            return 0;

        bool isPlayer1 = (owner == PhotonNetwork.MasterClient.ActorNumber);

        if (isPlayer1)
        {
            return interactable ? 1 : 2;
        }
        else
        {
            return interactable ? 3 : 4;
        }
    }

    private void ApplyMaterial()
    {
        if (pieceRenderer == null)
            return;

        Material materialToApply = defaultMaterial;

        switch (currentMaterialID)
        {
            case 0:
                materialToApply = defaultMaterial;
                break;
            case 1:
                materialToApply = PlayerMaterials.RedPlayerMaterial;
                break;
            case 2:
                materialToApply = PlayerMaterials.RedPlayerInactiveMaterial;
                break;
            case 3:
                materialToApply = PlayerMaterials.BluePlayerMaterial;
                break;
            case 4:
                materialToApply = PlayerMaterials.BluePlayerInactiveMaterial;
                break;
            default:
                materialToApply = defaultMaterial;
                break;
        }
        pieceRenderer.material = materialToApply;
    }
}
