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

    public NetworkPlayer GetLocalNetworkPlayer()
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

    public NetworkPlayer GetHostPlayer()
    {
        return GameObject.FindGameObjectWithTag("HostPlayer")?.GetComponent<NetworkPlayer>();
    }

    public Player GetLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
        {
            return Player.None;
        }

        return NetworkManager.Singleton.IsHost ? Player.Player1 : Player.Player2;
    }

    public bool IsLocalPlayersUnit(NetworkUnit unit)
    {
        if (unit == null)
        {
            return false;
        }

        NetworkPlayer localPlayer = GetLocalNetworkPlayer();
        return localPlayer != null && unit.Owner == localPlayer.Team;
    }

    public bool IsUnitOwnedByLocalPlayer(NetworkUnit unit)
    {
        return unit != null && unit.Owner == GetLocalPlayer();
    }
}
