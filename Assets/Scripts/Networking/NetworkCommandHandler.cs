using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkCommandHandler : NetworkBehaviour
{
    public static NetworkCommandHandler instance
    {
        get; private set;
    }

    private NetworkPlayer _player1; //хост
    private NetworkPlayer _player2;

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

    public void SetPlayers(NetworkPlayer p1, NetworkPlayer p2)
    {
        _player1 = p1;
        _player2 = p2;
    }

    public NetworkPlayer GetPlayerByTeam(Player team)
    {
        return team == Player.Player1 ? _player1 : _player2;
    }

    public NetworkPlayer GetLocalPlayer()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            return _player1;
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            return _player2;
        }            

        return null;
    }
}
