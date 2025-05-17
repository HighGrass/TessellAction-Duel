using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "MainNetworkConfig", menuName = "Netcode/Network Config", order = 0)]
public class MainNetworkConfig : ScriptableObject
{
    [Header("Referência ao NetworkConfig original")]
    public NetworkConfig replicatedConfig;
}
