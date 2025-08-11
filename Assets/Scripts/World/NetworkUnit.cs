using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnit : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer _renderer;
    [SerializeField] private GameObject _selectionIndicator;

    private UnitConfig _config;
    private Player _owner;

    public int Id => GetInstanceID();
    public Player Owner => _owner;
    public float MoveRange => _config.moveRange;
    public float AttackRange => _config.attackRange;
    public Vector3 Position => transform.position;
    public int ConfigId => _config.GetInstanceID();

    public void Initialize(UnitConfig config, Player player)
    {
        _config = config;
        _owner = player;

        _renderer.material.color = NetworkPlayer.GetTeamColor(player);
        transform.localScale = config.scale;

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        _selectionIndicator.SetActive(selected);
    }

    [ServerRpc]
    public void DestroyServerRpc()
    {
        if (!IsServer)
        {
            return;
        }

        DestroyClientRpc();

        if (TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned )
        {
            netObj.Despawn();
        }
        Destroy(gameObject);
    }

    [ClientRpc]
    private void DestroyClientRpc()
    {
        if (TryGetComponent<NetworkObject>(out var netObj))
        {
            if (netObj.IsSpawned && !netObj.IsOwnedByServer)
            {
                netObj.Despawn();
            }
        }
        Destroy(gameObject);
    }

    public void SetPosition(Vector3 position)
    {
        if (IsServer)
        {
            SetPositionServerRpc(position);
        }
    }

    [ServerRpc]
    public void SetPositionServerRpc(Vector3 position)
    {
        if (!IsServer)
        {
            return;
        }

        SetPositionClientRpc(position);
    }

    [ClientRpc]
    private void SetPositionClientRpc(Vector3 position)
    {
        transform.position = position;
    }
}
