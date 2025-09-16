using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;


public class NetworkSceneManager : NetworkBehaviour
{
    public static NetworkSceneManager instance
    {
        get; private set;
    }

    [Header("Scene Names")]
    [SerializeField] private string _mainMenuScene = "MainMenu";
    [SerializeField] private string _gameScene = "GameScene";

    private Scene _currentScene;

    public Scene currentScene => _currentScene;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadMainMenu()
    {
        if (IsServer)
        {
            GameNetworkManager.instance.SceneManager.LoadScene(_mainMenuScene, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(_mainMenuScene);
            _currentScene = SceneManager.GetActiveScene();
        }
    }

    public void LoadGameScene()
    {
        if (IsServer)
        {            
            GameNetworkManager.instance.SceneManager.LoadScene(_gameScene, LoadSceneMode.Single);
            GameNetworkManager.instance.SceneManager.OnLoadComplete += OnGameSceneLoaded;
            _currentScene = SceneManager.GetActiveScene();
        }
    }

    private void OnGameSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == _gameScene)
        {
            GameNetworkManager.instance.SceneManager.OnLoadComplete -= OnGameSceneLoaded;
            
            if (IsServer)
            {
                _currentScene = SceneManager.GetActiveScene();

                CreateBaseSystems();                              

                Debug.Log("SceneM: generating map");
                WorldGenerator.instance.GenerateMap();

                Debug.Log($"SceneM: Spawning host player: {clientId}");
                NetworkPlayerSpawner.instance.SpawnHostPlayer();

                Debug.Log("SceneM: Waiting for remote client connect!");
                GameNetworkManager.instance.OnClientConnectedCallback += OnClientConnected;
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer && clientId != NetworkManager.ServerClientId)
        {
            Debug.Log($"SceneM: Spawning client player: {clientId}");
            NetworkPlayerSpawner.instance.SpawnClientPlayer(clientId);

            InitPlayersSystems();

            GameNetworkManager.instance.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void CreateBaseSystems()
    {
        if (!IsServer)
        {
            return;
        }

        Debug.Log("SceneM: Creating base systems");
        GameSystemFactory.Create<NetworkPlayersManager>();
        GameSystemFactory.Create<NetworkPlayerSpawner>();
        
        GameSystemFactory.Create<NetworkSyncHandler>();
        GameSystemFactory.Create<WorldGenerator>();
        GameSystemFactory.Create<NetworkUnitsManager>();
        GameSystemFactory.Create<NetworkTurnManager>();
        GameSystemFactory.Create<NetworkActionsSystem>();
        GameSystemFactory.Create<NetworkVictorySystem>();
    }

    private void InitPlayersSystems()
    {
        if (!IsServer)
        {
            return;
        }

        Debug.Log("SceneM: Initializing game systems...");

        if (NetworkUnitsManager.instance != null)
        {
            NetworkUnitsManager.instance.SpawnArmies();
        }

        if (NetworkTurnManager.instance != null)
        {
            NetworkTurnManager.instance.InitializeServerRpc("Default");
        }

        if (NetworkVictorySystem.instance != null)
        {
            NetworkVictorySystem.instance.InitializeServerRpc("Default");
        }

        Debug.Log("SceneM: Game systems initialized successfully!");
    }

    private void OnDrawGizmos()
    {
        var spawnPoints = WorldGenerator.instance.GetTeamSpawnPoints();
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnPoints[Player.Player1], 10f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(spawnPoints[Player.Player2], 10f);   
    }
}
