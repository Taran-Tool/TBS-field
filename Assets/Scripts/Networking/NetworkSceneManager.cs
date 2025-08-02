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

    private string _currentActiveScene;

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

    //главное меню
    public void LoadMainMenu()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(_mainMenuScene, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(_mainMenuScene);
        }
    }

    //сетевая загрузка игровой сцены
    public void LoadGameScene()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnGameSceneLoaded;
            NetworkManager.Singleton.SceneManager.LoadScene(_gameScene, LoadSceneMode.Single);
        }
    }

    private void OnGameSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == _gameScene)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnGameSceneLoaded;
        }
        if (IsServer)
        {
            InitializeGameWorld();
        }
    }

    private void InitializeGameWorld()
    {
        // 1. Генерация мира
        WorldGenerator.instance.GenerateMap();
        // 2. Спавн игроков
        NetworkPlayerSpawner.instance.SpawnPlayers();
        // 3. Спавн юнитов
        NetworkUnitsManager.instance.SpawnArmies();
        // 3. Инициализация систем
        //TurnManager.Instance.StartNewGame();
        Debug.Log("Turn manager");
        Debug.Log("Game world initialized on server");
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
