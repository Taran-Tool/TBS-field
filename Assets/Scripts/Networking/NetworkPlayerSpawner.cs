using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    public static NetworkPlayerSpawner instance
    {
        get; private set;
    }

    public NetworkPlayer _hostPlayer;
    public NetworkPlayer _clientPlayer;

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

    public void SpawnHostPlayer()
    {
        if (!IsServer)
        {
            return;
        }
        
        _hostPlayer = SpawnPlayer(Player.Player1, GameNetworkManager.instance.LocalClientId);
  
        NetworkPlayersManager.instance.SetPlayers(_hostPlayer, null);
    }

    public void SpawnClientPlayer(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        _clientPlayer = SpawnPlayer(Player.Player2, clientId);

        NetworkPlayersManager.instance.SetPlayers(_hostPlayer, _clientPlayer);
    }

    private NetworkPlayer SpawnPlayer(Player team, ulong ownerClientId)
    {
        GameObject playerObj = Instantiate(Resources.Load<GameObject>("Prefabs/Player"));
        playerObj.name = "Player" + (ownerClientId + 1); //+1 так как нумерация с 0

        Vector3 spawnCenter = team == Player.Player1
        ? WorldGenerator.instance.Team1SpawnCenter.Value
        : WorldGenerator.instance.Team2SpawnCenter.Value;       
        
        playerObj.transform.position = spawnCenter;

        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
        
        var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
        networkPlayer.Team = team;

        netObj.SpawnAsPlayerObject(ownerClientId);

        if (ownerClientId == NetworkManager.ServerClientId)
        {
            Debug.Log("HostPlayer spawned and set");
        }
        else
        {
            Debug.Log("ClientPlayer spawned and set");
        }

        NetworkSyncHandler.instance.RegisterObjectServerRpc(netObj.NetworkObjectId, "Player");

        return networkPlayer;
    }

}
