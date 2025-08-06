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

        GameSystemFactory.Create<NetworkSceneManager>();

        NetworkSceneManager.instance.LoadGameScene();
    }

    private void OnHostClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected (Host mode): {clientId}");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnHostClientConnected;
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            //начальная синхронизация
            NetworkSyncHandler.instance.RequestInitialSyncServerRpc(clientId);
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
