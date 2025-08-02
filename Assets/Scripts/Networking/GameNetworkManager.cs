using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Events;
using System;

public class GameNetworkManager : NetworkManager
{
    public static GameNetworkManager instance
    {
        get; private set;
    }

    public UnityEvent<Player> OnLocalPlayerReady = new();

    //singleton
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //создаю хост
    public void StartHostGame()
    {
        NetworkManager.Singleton.OnServerStarted += OnHostStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnHostClientConnected;
        NetworkManager.Singleton.StartHost();
    }

    private void OnHostStarted()
    {
        Debug.Log("Host started");
        NetworkManager.Singleton.OnServerStarted -= OnHostStarted;
        //создаю игровой мир
        if (IsServer)
        {
            //cоздаю помощников и менеджеров
            SpawnCommandHandler();
            SpawnSyncHandler();
            SpawnSceneManager();
            SpawnPlayerSpawner();
            SpawnWorldGenerator();
            SpawnUnitsManager();
        }
    }

    private void SpawnCommandHandler()
    {
        GameObject handlerObj = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkCommandHandler"));
        handlerObj.name = "NetworkCommandHandler";
        handlerObj.GetComponent<NetworkObject>().Spawn();
    }

    private void SpawnSyncHandler()
    {
        GameObject handlerObj = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkSyncHandler"));
        handlerObj.name = "NetworkSyncHandler";
        handlerObj.GetComponent<NetworkObject>().Spawn();
    }

    private void SpawnSceneManager()
    {
        GameObject handlerObj = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkSceneManager"));
        handlerObj.name = "NetworkSceneManager";
        handlerObj.GetComponent<NetworkObject>().Spawn();
    }

    private void SpawnPlayerSpawner()
    {
        GameObject handlerObj = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkPlayerSpawner"));
        handlerObj.name = "NetworkPlayerSpawner";
        handlerObj.GetComponent<NetworkObject>().Spawn();
    }

    private void SpawnWorldGenerator()
    {
        GameObject handlerObj = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkWorldGenerator"));
        handlerObj.name = "NetworkWorldGenerator";
        handlerObj.GetComponent<NetworkObject>().Spawn();
    }

    private void SpawnUnitsManager()
    {
        GameObject handlerObj = Instantiate(Resources.Load<GameObject>("Prefabs/NetworkUnitsManager"));
        handlerObj.name = "NetworkUnitsManager";
        handlerObj.GetComponent<NetworkObject>().Spawn();
    }

    private void OnHostClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected (Host mode): {clientId}");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnHostClientConnected;
        if (IsServer)
        {
            //начальная синхронизация
            NetworkSyncHandler.instance.SyncGameStateServerRpc(clientId);
        }
    }

    public void JoinGame(string ipAddress)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("player_data");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, 7777);
        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected (Client mode): {clientId}");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (IsClient && !IsServer)
        {
            RegisterLocalPlayer();
        }
    }

    private void RegisterLocalPlayer()
    {
        var localClienId = NetworkManager.Singleton.LocalClientId;
        var playerObj = NetworkManager.Singleton.ConnectedClients[localClienId].PlayerObject;
        var player = playerObj.GetComponent<NetworkPlayer>();

        OnLocalPlayerReady.Invoke(player.Team);
    }



}
