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

    private NetworkVariable<Player> _owner = new NetworkVariable<Player>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Color> _unitColor = new NetworkVariable<Color>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _isSelected = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _moveRange = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _attackRange = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Vector3> _unitScale = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server
    );

    public int Id => (int) NetworkObject.NetworkObjectId;
    public Player Owner => _owner.Value;
    public float MoveRange => _moveRange.Value;
    public float AttackRange => _attackRange.Value;
    public Vector3 Position => transform.position;
    public int ConfigId => _config.GetInstanceID();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _owner.OnValueChanged += OnOwnerChanged;
        _unitColor.OnValueChanged += OnColorChanged;
        _isSelected.OnValueChanged += OnSelectionChanged;

        _moveRange.OnValueChanged += OnStatsChanged;
        _attackRange.OnValueChanged += OnStatsChanged;
        _unitScale.OnValueChanged += OnScaleChanged;

        if (_renderer != null)
        {
            _renderer.material.color = _unitColor.Value;
        }
        _selectionIndicator.SetActive(_isSelected.Value);

        transform.localScale = _unitScale.Value;

        if (IsClient)
        {
            ClientUnitsCache.RegisterUnit(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _owner.OnValueChanged -= OnOwnerChanged;
        _unitColor.OnValueChanged -= OnColorChanged;
        _isSelected.OnValueChanged -= OnSelectionChanged;

        _moveRange.OnValueChanged -= OnStatsChanged;
        _attackRange.OnValueChanged -= OnStatsChanged;
        _unitScale.OnValueChanged -= OnScaleChanged;

        if (IsClient)
        {
            ClientUnitsCache.UnregisterUnit(Id);
        }
    }

    private void OnOwnerChanged(Player previous, Player current)
    {
        UpdateColor();
    }

    private void OnColorChanged(Color previous, Color current)
    {
        if (_renderer != null)
        {
            _renderer.material.color = current;
        }
    }

    private void OnStatsChanged(float previous, float current)
    {
        //вдруг нужно будет
    }

    private void OnScaleChanged(Vector3 previous, Vector3 current)
    {
        transform.localScale = current;
    }

    public void Initialize(UnitConfig config, Player player)
    {
        _config = config;
        _owner.Value = player;

        _unitColor.Value = NetworkPlayer.GetTeamColor(player);

        _moveRange.Value = config.moveRange;
        _attackRange.Value = config.attackRange;
        _unitScale.Value = config.scale;

        _isSelected.Value = false;
        _selectionIndicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (IsServer)
        {
            if (NetworkValidator.IsUnitOwnedByClient(OwnerClientId, Id))
            {
                _isSelected.Value = selected;
            }
        }
        else
        {
            SetSelectedServerRpc(selected);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetSelectedServerRpc(bool selected, ServerRpcParams rpcParams = default)
    {
        if (NetworkValidator.IsUnitOwnedByClient(rpcParams.Receive.SenderClientId, Id))
        {
            _isSelected.Value = selected;
        }
    }

    [ServerRpc(RequireOwnership = false)]
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

    }

    public void SetPosition(Vector3 position)
    {
        if (IsServer)
        {
            SetPositionServerRpc(position);
        }
    }

    [ServerRpc(RequireOwnership = false)]
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

    private void UpdateColor()
    {
        if (IsServer)
        {
            _unitColor.Value = NetworkPlayer.GetTeamColor(_owner.Value);
        }
    }

    private void OnSelectionChanged(bool previous, bool current)
    {
        _selectionIndicator.SetActive(current);
    }
}
