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

        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnUserChanged += OnUserChanged;
        }

        SetupPlayerProperties();

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

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnUserChanged -= OnUserChanged;
        }
    }

    private void OnUserChanged()
    {
        Debug.Log("Utilizador mudou!");
        SetupPlayerProperties();
    }

    private void SetupPlayerProperties()
    {
        if (!string.IsNullOrEmpty(AuthManager.UserId))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["GameUserId"] = AuthManager.UserId;
            props["Username"] = AuthManager.Username;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log(
                $"Propriedade GameUserId configurada: {AuthManager.UserId} (Username: {AuthManager.Username})"
            );
        }
        else
        {
            // Se UserId é null (logout), limpar as propriedades
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["GameUserId"] = null;
            props["Username"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.LogWarning(
                "⚠️ AuthManager.UserId é null - limpando propriedades do Photon (logout)"
            );
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

        SetupPlayerProperties();

        IsInLobby = true;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            $"Entrou na sala '{PhotonNetwork.CurrentRoom.Name}'. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}"
        );
        IsInLobby = false;

        StartCoroutine(CheckForDuplicatesAfterDelay(0.5f));
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log(
            $"Jogador {newPlayer.NickName} entrou. Jogadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}"
        );

        // Aguardar para as propriedades do novo jogador sincronizarem
        StartCoroutine(CheckForDuplicatesAfterDelay(1.0f));
    }

    private IEnumerator CheckForDuplicatesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Verificar se ainda estamos na sala
        if (!PhotonNetwork.InRoom)
            yield break;

        Debug.Log("Verificando duplicações após delay...");

        if (CheckForDuplicateUsers())
        {
            Debug.LogWarning("Detectado jogador da mesma conta na sala!");
            ShowDuplicateUserError();
            PhotonNetwork.LeaveRoom();
            yield break;
        }

        // Se não há duplicações, continuar com o jogo
        CheckForGameStart();
    }

    private bool CheckForDuplicateUsers()
    {
        if (string.IsNullOrEmpty(AuthManager.UserId))
        {
            Debug.LogWarning("AuthManager.UserId é null, não é possível verificar duplicações");
            return false;
        }

        string myUserId = AuthManager.UserId;
        string myUsername = AuthManager.Username;

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            Debug.Log(
                $"Verificando jogador: {player.NickName} (Actor {player.ActorNumber}, IsLocal: {player.IsLocal})"
            );

            if (player.IsLocal)
            {
                continue;
            }

            if (player.CustomProperties.TryGetValue("GameUserId", out object otherUserId))
            {
                string otherUserIdStr = otherUserId?.ToString();
                string otherUsername = player.CustomProperties.TryGetValue(
                    "Username",
                    out object username
                )
                    ? username?.ToString()
                    : "N/A";

                Debug.Log($"GameUserId: {otherUserIdStr ?? "null"}, Username: {otherUsername}");

                // Só verificar duplicação se ambos os UserIds não são null/empty
                if (
                    !string.IsNullOrEmpty(myUserId)
                    && !string.IsNullOrEmpty(otherUserIdStr)
                    && otherUserIdStr == myUserId
                )
                {
                    return true;
                }
                else if (string.IsNullOrEmpty(otherUserIdStr))
                {
                    Debug.Log($"Outro jogador não tem UserId válido (provavelmente não logado)");
                }
                else
                {
                    Debug.Log($"UserIds diferentes: {myUserId} vs {otherUserIdStr}");
                }
            }
            else
            {
                Debug.LogWarning($"Jogador {player.NickName} não tem propriedade GameUserId");
            }
        }

        Debug.Log("✅ Nenhuma duplicação detectada - todos os jogadores têm contas diferentes");
        return false;
    }

    // Método de debug para testar o sistema anti-duplicação
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugCheckDuplicates()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                string gameUserId = player.CustomProperties.TryGetValue(
                    "GameUserId",
                    out object userId
                )
                    ? userId?.ToString()
                    : "N/A";
                string username = player.CustomProperties.TryGetValue("Username", out object uname)
                    ? uname?.ToString()
                    : "N/A";
                Debug.Log(
                    $"- {player.NickName} (Actor {player.ActorNumber}): GameUserId = {gameUserId}, Username = {username}, IsLocal = {player.IsLocal}"
                );
            }
        }

        bool hasDuplicates = CheckForDuplicateUsers();
    }

    // Método para forçar atualização das propriedades
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceUpdateProperties()
    {
        Debug.Log("Forçando atualização das propriedades...");
        SetupPlayerProperties();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestUserChangedEvent()
    {
        Debug.Log("Testando evento OnUserChanged...");
        OnUserChanged();
    }

    private void ShowDuplicateUserError()
    {
        if (ErrorMessageManager.Instance != null)
        {
            ErrorMessageManager.Instance.ShowError(
                "Cannot play against the same account! Try again."
            );
        }
        else
        {
            Debug.LogError("ERRO: Não é possível jogar contra a mesma conta!");
        }
    }

    private void CheckForGameStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                Debug.Log("Sala cheia! MasterClient a carregar a cena do jogo...");
                PhotonNetwork.CurrentRoom.IsOpen = false; 
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

    public override void OnLeftRoom()
    {
        Debug.Log("Saímos da sala. A voltar ao lobby...");
        IsInLobby = false;

        // Se estamos no menu de matchmaking, tentar voltar ao lobby
        if (SceneManager.GetActiveScene().name == "MatchmakingMenu")
        {
            if (PhotonNetwork.IsConnected)
            {
                StartCoroutine(JoinLobbyWhenReady());
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Desconectado da Photon: {cause}");
        IsInLobby = false;
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (SceneManager.GetActiveScene().name == "MatchmakingMenu")
        {
            PhotonNetwork.LeaveRoom();
        }
        else if (SceneManager.GetActiveScene().name == "GameScene")
        {
            // Jogador desconectou durante o jogo
            HandlePlayerDisconnectedDuringGame();
        }
    }

    private void HandlePlayerDisconnectedDuringGame()
    {
        if (AuthManager.Instance != null)
        {
            // Jogador que ficou = vitória (+50 pontos)
            AuthManager.Instance.SendGameResult("win", 50);
        }

        // Voltar ao menu de matchmaking após um pequeno delay
        StartCoroutine(ReturnToMatchmakingAfterDisconnect());
    }

    private System.Collections.IEnumerator ReturnToMatchmakingAfterDisconnect()
    {
        // Pequeno delay para garantir que a atualização da BD é enviada
        yield return new WaitForSeconds(1.0f);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene("MatchmakingMenu");
        }
    }
}
