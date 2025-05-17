using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetcodeDebugUI : MonoBehaviour
{
    private NetworkManager nm;
    private UnityTransport utp;

    private void Awake()
    {
        nm = NetworkManager.Singleton;
        utp = nm.GetComponent<UnityTransport>();

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        nm.OnServerStarted += () => Debug.Log("Servidor à escuta");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} ligado");
        if (clientId == nm.LocalClientId)
            Debug.Log("Este cliente é local");
        else
            Debug.Log("Este cliente não é local");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} desligou");
    }

    private void ConfigureTransport(string address = "127.0.0.1", ushort port = 7777)
    {
        if (utp == null)
            return;

        utp.ConnectionData.Address = address;
        utp.ConnectionData.Port = port;
    }

    void OnGUI()
    {
        if (nm == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 220, 200), "Netcode", GUI.skin.window);

        if (!nm.IsClient && !nm.IsServer)
        {
            if (GUILayout.Button("Start Host"))
            {
                ConfigureTransport();
                nm.StartHost();
                Debug.Log("Host iniciado");
            }
            if (GUILayout.Button("Start Client"))
            {
                ConfigureTransport("127.0.0.1", 7777);
                nm.StartClient();
                Debug.Log("Client iniciado");
            }
            if (GUILayout.Button("Start Server"))
            {
                ConfigureTransport();
                nm.StartServer();
                Debug.Log("Server iniciado");
            }
        }
        else
        {
            string mode =
                nm.IsHost ? "Host"
                : nm.IsServer ? "Server"
                : "Client";

            GUILayout.Label($"Running as: {mode}");
        }

        GUILayout.EndArea();
    }
}
