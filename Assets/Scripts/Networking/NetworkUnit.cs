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

    public void Initialize(UnitConfig config, Player player)
    {
        _config = config;
        _owner = player;

        // Визуальные настройки
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
        DestroyClientRpc();
        Destroy(gameObject);
    }

    [ClientRpc]
    private void DestroyClientRpc()
    {
        Destroy(gameObject);
    }
}
