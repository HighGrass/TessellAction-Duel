using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private Piece hoveredPiece;
    private Camera mainCamera;
    private TurnManager turnManager;

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
            return;

        if (turnManager.CurrentTurnIndex != (int)NetworkManager.Singleton.LocalClientId)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var piece = hit.collider.GetComponent<Piece>();
            if (piece != null)
            {
                if (hoveredPiece != null && hoveredPiece != piece)
                    hoveredPiece.SetHovered(false);

                hoveredPiece = piece;
                hoveredPiece.SetHovered(true);

                // Move on click
                if (Input.GetMouseButtonDown(0))
                {
                    piece.RequestMoveServerRpc(hit.point);
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
}
