using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayersManager : NetworkBehaviour
{
    public static NetworkPlayersManager instance
    {
        get; private set;
    }

    private NetworkVariable<ulong> _player1Id = new NetworkVariable<ulong>();
    private NetworkVariable<ulong> _player2Id = new NetworkVariable<ulong>();

    private NetworkPlayer _player1; //хост
    private NetworkPlayer _player2;

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Подписываемся на изменения NetworkVariables
        _player1Id.OnValueChanged += OnPlayer1IdChanged;
        _player2Id.OnValueChanged += OnPlayer2IdChanged;

        // Если уже есть значения, обрабатываем их
        if (_player1Id.Value != 0)
        {
            FindPlayerById(_player1Id.Value, ref _player1);
        }
        if (_player2Id.Value != 0)
        {
            FindPlayerById(_player2Id.Value, ref _player2);
        }
    }

    public void SetPlayers(NetworkPlayer p1, NetworkPlayer p2)
    {
        _player1 = p1;
        _player2 = p2;
        _player1Id.Value = p1 != null ? p1.NetworkObjectId : 0;
        _player2Id.Value = p2 != null ? p2.NetworkObjectId : 0;
    }

    private void OnPlayer1IdChanged(ulong oldId, ulong newId)
    {
        if (newId != 0)
        {
            FindPlayerById(newId, ref _player1);
        }
    }

    private void OnPlayer2IdChanged(ulong oldId, ulong newId)
    {
        if (newId != 0)
        {
            FindPlayerById(newId, ref _player2);
        }
    }

    private void FindPlayerById(ulong networkObjectId, ref NetworkPlayer player)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            player = netObj.GetComponent<NetworkPlayer>();
        }
    }

    public NetworkPlayer GetPlayerByTeam(Player team)
    {
        return team == Player.Player1 ? _player1 : _player2;
    }

    public NetworkPlayer GetHostPlayer()
    {
        return GetPlayerByTeam(Player.Player1);
    }

    public Player GetLocalPlayer()
    {
        return GameNetworkManager.instance.IsHost ? Player.Player1 : Player.Player2;
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

    public NetworkPlayer GetLocalNetworkPlayer()
    {
        foreach (var player in new[] { _player1, _player2 })
        {
            if (player != null && player.IsOwner)
            {
                return player;
            }
        }

        return null;
    }

    public bool IsUnitOwnedByLocalPlayer(NetworkUnit unit)
    {
        if (unit == null)
        {
            return false;
        }
        return unit.Owner == GetLocalPlayer();
    }
}
