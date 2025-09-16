using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Events;
using System;

public class GameNetworkManager:NetworkManager
{
    public static GameNetworkManager instance
    {
        get; private set;
    }
    public UnityEvent<Player> OnLocalPlayerReady = new();

    private bool _isClientConnected = false;
    public bool isClientConnected => _isClientConnected;

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

    //Старт хоста
    public void StartHostGame()
    {
        OnServerStarted += OnHostStarted;
        OnClientConnectedCallback += OnClientConnected;
        StartHost();
    }

    private void OnHostStarted()
    {
        Debug.Log("GNM: Host started");
        OnServerStarted -= OnHostStarted;
        NetworkSceneManager.instance.LoadGameScene();
    }

    //Старт клиента
    public void JoinGame(string ipAddress)
    {
        OnClientConnectedCallback += OnClientConnected;
        NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes("player_data");
        GetComponent<UnityTransport>().SetConnectionData(ipAddress, 7777);
        StartClient();
        NetworkSceneManager.instance.LoadGameScene();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"GNM: Client connected: {clientId} (IsHost: {IsHost}, IsServer: {IsServer})");

        if (clientId == LocalClientId && IsServer)
        {
            if (IsHost)
            {
                Debug.Log($"GNM: Local client connected: {clientId}");
            }            
        }
        if (clientId != LocalClientId && IsServer)
        {
            Debug.Log($"GNM: Remote client connected: {clientId}");
            _isClientConnected = true;
        }        
    }
}