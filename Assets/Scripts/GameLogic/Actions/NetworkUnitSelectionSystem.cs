using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitSelectionSystem : NetworkBehaviour
{
    [SerializeField] private LayerMask _unitLayer;
    private NetworkUnit _selectedUnit;
    private int _currentUnitIndex = -1;

    public NetworkUnit SelectedUnit => _selectedUnit;



    private void Update()
    {
        if (!IsClient || !IsOwner)
        {
            return;
        }
        HandleUnitSelection();
        HandleKeyboardSelection();
    }

    private void HandleUnitSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _unitLayer))
            {
                var unit = hit.collider.GetComponent<NetworkUnit>();
                if (unit != null && NetworkCommandHandler.instance.IsLocalPlayersUnit(unit))
                {
                    SelectUnit(unit);
                }
            }
        }
    }

    private void HandleKeyboardSelection()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            SelectNextUnit();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SelectPreviousUnit();
        }
    }

    private void SelectNextUnit()
    {
        var units = GetLocalPlayerUnits();
        if (units.Count == 0)
        {
            return;
        }               

        _currentUnitIndex = (_currentUnitIndex + 1) % units.Count;
        SelectUnit(units[_currentUnitIndex]);
    }

    private void SelectPreviousUnit()
    {
        var units = GetLocalPlayerUnits();
        if (units.Count == 0)
        {
            return;
        }

        _currentUnitIndex = (_currentUnitIndex - 1 + units.Count) % units.Count;
        SelectUnit(units[_currentUnitIndex]);
    }

    private List<NetworkUnit> GetLocalPlayerUnits()
    {
        NetworkPlayer localPlayer = NetworkCommandHandler.instance.GetLocalNetworkPlayer();
        return localPlayer != null ?
            NetworkUnitsManager.instance.GetPlayerUnits(localPlayer.Team) :
            new List<NetworkUnit>();
    }

    public void SetUnitsLayer(string layerName)
    {
        if (IsClient)
        {
            return;
        }
        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1 && _unitLayer.value == 0)
        {
            _unitLayer = 1 << layer;
        }        
    }

    private void SelectUnit(NetworkUnit unit)
    {
        _selectedUnit?.SetSelected(false);
        _selectedUnit = unit;
        unit.SetSelected(true);
    }
}
