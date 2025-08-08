using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkActionsSystem : NetworkBehaviour
{
    public static NetworkActionsSystem instance
    {
        get; private set;
    }

    [SerializeField] private Material pathMaterial;

    private NetworkUnitSelectionSystem _selectionSystem;
    private NetworkUnitMoveSystem _movementSystem;
    private NetworkUnitAttackSystem _attackSystem;

    public NetworkUnitAttackSystem AttackSystem => _attackSystem;

    public NetworkUnitSelectionSystem UnitSelectionSystem => _selectionSystem;

    public Player LocalPlayer => NetworkCommandHandler.instance.GetLocalPlayer();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        var lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = pathMaterial;

        _selectionSystem = GetComponent<NetworkUnitSelectionSystem>();
        _movementSystem = GetComponent<NetworkUnitMoveSystem>();
        _attackSystem = GetComponent<NetworkUnitAttackSystem>();

        _selectionSystem.SetUnitsLayer("Unit");
        _movementSystem.SetLayers("Ground", "Obstacle", "Unit");
        _movementSystem.Initialize(lineRenderer);
    }

    private void Update()
    {
        if (!IsClient || !IsOwner)
        {
            return;
        }            

        var selectedUnit = _selectionSystem.SelectedUnit;
        if (selectedUnit != null)
        {
            _movementSystem.HandleMovement(selectedUnit);

            if (!_movementSystem.HasActivePath())
            {
                _attackSystem.ShowAttackRange(selectedUnit.transform.position, selectedUnit.AttackRange);
            }
        }
    }
}
