using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    private List<ulong> playerIds = new List<ulong>();
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(
        0,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public int CurrentTurnIndex
    {
        get => currentTurnIndex.Value;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                playerIds.Add(client.ClientId);

            StartTurn();
        }
    }

    private void StartTurn()
    {
        ulong currentPlayerId = playerIds[currentTurnIndex.Value];
        Debug.Log($"[Server] Turno do jogador {currentPlayerId}");
        NotifyTurnClientRpc(currentPlayerId);
    }

    [ClientRpc]
    void NotifyTurnClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.LocalClientId == playerId)
            Debug.Log("[Client] É o seu turno!");
        else
            Debug.Log("[Client] Esperando outro jogador...");
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong caller = rpcParams.Receive.SenderClientId;
        if (caller != playerIds[currentTurnIndex.Value])
            return;

        currentTurnIndex.Value = (currentTurnIndex.Value + 1) % playerIds.Count;
        StartTurn();
    }
}
