using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimplePunLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private string gameSceneName = "GameScene";

    [SerializeField]
    private GameObject playerPrefab;

    public bool IsInLobby { get; private set; }

    public static SimplePunLauncher Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Configurar AuthValues apenas uma vez, usando GUID como antes
        PhotonNetwork.AuthValues = new AuthenticationValues(System.Guid.NewGuid().ToString());
    }

    private void Start()
    {
        if (playerPrefab == null)
            Debug.LogError("Player Prefab não está atribuído no SimplePunLauncher!", this);

        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            IsInLobby = false;
            Debug.Log($"Conectando à Photon com UserId: {PhotonNetwork.AuthValues.UserId}");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("Já conectado à Photon. Tentando entrar no lobby...");
            StartCoroutine(JoinLobbyWhenReady());
        }
    }

    public void FindMatch()
    {
        if (PhotonNetwork.IsConnected && IsInLobby)
        {
            Debug.Log("Procurando por uma partida...");
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            Debug.LogError("Não conectado ou não está no lobby. Não é possível procurar partida.");
        }
    }

    public void LeaveMatchmakingRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Cancelando procura. A sair da sala.");
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning(
            $"Falha ao juntar a uma sala aleatória: {message}. Criando uma nova sala..."
        );
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true,
        };
        PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Conectado ao Master Server. Estado atual: {PhotonNetwork.NetworkClientState}");

        if (!PhotonNetwork.InLobby)
            StartCoroutine(JoinLobbyWhenReady());
    }

    private IEnumerator JoinLobbyWhenReady()
    {
        Debug.Log("A aguardar estado ConnectedAndReady antes de entrar no lobby...");

        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

        if (!PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("JoinLobby chamado com sucesso.");
        }
        else
        {
            Debug.Log("Já no lobby ou entrando. JoinLobby não necessário.");
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Entrou no lobby. Pronto para procurar partida.");
        IsInLobby = true;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            $"Entrou na sala '{PhotonNetwork.CurrentRoom.Name}'. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}"
        );
        IsInLobby = false;
        CheckForGameStart();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log(
            $"Jogador {newPlayer.NickName} entrou. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}"
        );
        CheckForGameStart();
    }

    private void CheckForGameStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                Debug.Log("Sala cheia! MasterClient a carregar a cena do jogo...");
                PhotonNetwork.CurrentRoom.IsOpen = false; // Fecha a sala para que ninguém mais possa entrar
                PhotonNetwork.LoadLevel(gameSceneName);
            }
            else
            {
                Debug.Log(
                    $"A aguardar por mais jogadores... ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})"
                );
            }
        }
    }

    // Adicionados callbacks de cena para instanciar o jogador de forma fiável
    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            // Tenta encontrar um GameManager na cena para obter os pontos de spawn
            GameManager gm = FindObjectOfType<GameManager>();
            Transform spawnPoint = (gm != null) ? gm.GetSpawnPoint() : null;
            Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRotation =
                (spawnPoint != null) ? spawnPoint.rotation : Quaternion.identity;

            Debug.Log($"Cena do Jogo carregada. Instanciando jogador em {spawnPosition}");
            PhotonNetwork.Instantiate(this.playerPrefab.name, spawnPosition, spawnRotation, 0);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Desconectado da Photon: {cause}");
        IsInLobby = false;
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Jogador {otherPlayer.NickName} saiu da sala.");
        // Aqui pode-se adicionar lógica para lidar com um jogador a desistir a meio do jogo.
    }
}
