using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitMoveSystem : NetworkBehaviour
{
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float pathNodeHeight = 0.2f;
    [SerializeField] private float pathLineWidth = 0.2f;

    private LineRenderer _pathLineRenderer;
    private List<Vector3> _currentPath = new();

    public void Initialize(LineRenderer lineRenderer)
    {
        _pathLineRenderer = lineRenderer;
        _pathLineRenderer.startWidth = pathLineWidth;
        _pathLineRenderer.endWidth = pathLineWidth;
        _pathLineRenderer.enabled = false;
    }

    public void SetGroundLayer(string layerName)
    {
        if (IsClient)
        {
            return;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1 && _groundLayer.value == 0)
        {
            _groundLayer = 1 << layer;
        }
    }

    public void HandleMovement(NetworkUnit selectedUnit)
    {
        if (selectedUnit == null || !CanMove(selectedUnit))
        {
            return;
        }            

        if (Input.GetMouseButtonDown(1))
        {
            TryFindPath(selectedUnit);
        }

        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift))
        {
            TryMoveUnit(selectedUnit);
        }            
    }

    private bool CanMove(NetworkUnit unit)
    {
        return NetworkTurnManager.instance.CurrentPlayer.Value == unit.Owner &&
               NetworkTurnManager.instance.ActionsRemaining.Value > 0;
    }

    private void TryFindPath(NetworkUnit unit)
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _groundLayer))
        {
            //_currentPath = Pathfinder.FindPath(unit.transform.position, hit.point, unit.MoveRange);
            _currentPath = null;
            DrawPath(_currentPath);
        }
    }

    private void DrawPath(List<Vector3> path)
    {
        
        _pathLineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            _pathLineRenderer.SetPosition(i, path[i] + Vector3.up * pathNodeHeight);
        }
        _pathLineRenderer.enabled = true;
    }

    private void TryMoveUnit(NetworkUnit unit)
    {
        if (_currentPath.Count > 0)
        {
            MoveUnitServerRpc(unit.Id, _currentPath[^1]);
            ClearPath();
        }
    }

    [ServerRpc]
    public void MoveUnitServerRpc(int unitId, Vector3 target, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }
        //юнит
        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
        {
            return;
        }

        //проверка
        float distance = Vector3.Distance(unit.Position, target);
        if (distance > unit.MoveRange)
        {
            return;
        }

        //движение

        //синхронизация
        NetworkTurnManager.instance.SpendActionServerRpc(ActionTypes.Move);
        NetworkSyncHandler.instance.SyncUnitPositionClientRpc(unit.Id, unit.Position);
    }

    private void ClearPath()
    {
        _pathLineRenderer.positionCount = 0;
        _currentPath.Clear();
        _pathLineRenderer.enabled = false;
    }
}
