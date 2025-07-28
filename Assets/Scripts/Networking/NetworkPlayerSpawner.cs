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

    public void SpawnPlayers()
    {
        if (IsServer)
        {
            //сервер создаст обоих игроков
            var p1 = SpawnPlayer(Player.Player1);
            var p2 = SpawnPlayer(Player.Player2);

            //определяю игроков
            NetworkCommandHandler.instance.SetPlayers(p1, p2);
        }        
    }

    private NetworkPlayer SpawnPlayer(Player team)
    {

        GameObject playerObj = Instantiate(Resources.Load<GameObject>("Prefabs/Player"));

        ulong ownerClientId = team == Player.Player1 ? 0UL : 1UL;
        playerObj.name = "" + ownerClientId;

        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
        var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
        networkPlayer.Team = team;


        netObj.SpawnWithOwnership(ownerClientId);

        return networkPlayer;
    }
}
