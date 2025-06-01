using Fusion;

public class TurnManager : NetworkBehaviour
{
    [Networked]
    public int CurrentTurnIndex { get; set; }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RequestEndTurnRpc()
    {
        CurrentTurnIndex = (CurrentTurnIndex + 1) % 4;
    }
}
