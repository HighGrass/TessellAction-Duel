using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Piece : NetworkBehaviour
{
    private Vector3 originalPosition;
    private bool isHovered = false;
    private bool isSelected = false;

    // Networked position variable
    private NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );

    public override void OnNetworkSpawn()
    {
        originalPosition = transform.position;

        // Sync initial position
        if (IsServer)
            netPosition.Value = originalPosition;

        netPosition.OnValueChanged += (oldPos, newPos) =>
        {
            transform.position = newPos;
        };
    }

    public void SetHovered(bool hovered)
    {
        if (hovered == isHovered)
            return;
        isHovered = hovered;
        Vector3 targetPosition = originalPosition + (isHovered ? Vector3.up * 0.5f : Vector3.zero);
        RequestMoveServerRpc(targetPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMoveServerRpc(Vector3 targetPosition, ServerRpcParams rpcParams = default)
    {
        // Server updates the networked position
        netPosition.Value = targetPosition;
    }
}
