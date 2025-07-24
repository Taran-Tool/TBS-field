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

    private NetworkPlayer _player1;
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
}
