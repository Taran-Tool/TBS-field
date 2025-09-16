using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitMoveSystem : NetworkBehaviour
{
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private LayerMask _unitLayer;
    [SerializeField] private float _pathNodeHeight = 0.2f;
    [SerializeField] private float _pathLineWidth = 0.2f;

    [SerializeField] private Color _validPathColor = Color.green;
    [SerializeField] private Color _partialPathColor = Color.yellow;

    [SerializeField] private float _doubleClickTime = 0.3f;

    private LineRenderer _pathLineRenderer;
    private Pathfinder _pathfinder;
    private Pathfinder.PathResult _currentPath;
    private float _lastRightClickTime;

    public void SetLayers(string groundLayerName, string obstacleLayerName, string unitLayerName)
    {
        if (IsClient)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (groundLayer != -1 && _groundLayer.value == 0)
        {
            _groundLayer = 1 << groundLayer;
        }

        int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        if (obstacleLayer != -1 && _obstacleLayer.value == 0)
        {
            _obstacleLayer = 1 << obstacleLayer;
        }

        int unitLayer = LayerMask.NameToLayer(unitLayerName);
        if (unitLayer != -1 && _unitLayer.value == 0)
        {
            _unitLayer = 1 << unitLayer;
        }
    }

    public void Initialize(LineRenderer lineRenderer)
    {
        _pathLineRenderer = lineRenderer;
        _pathLineRenderer.startWidth = _pathLineWidth;
        _pathLineRenderer.endWidth = _pathLineWidth;
        _pathLineRenderer.enabled = false;

        _pathfinder = new Pathfinder(_groundLayer, _obstacleLayer, _unitLayer);
    }

    public void HandleMovement(NetworkUnit selectedUnit)
    {
        
        if (selectedUnit == null || !CanMove(selectedUnit))
        {
            ClearPath();
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick(selectedUnit);
        }
        else if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
        {
            ClearPath();
        }
    }

    private void HandleRightClick(NetworkUnit unit)
    {
        bool isDoubleClick = Time.time - _lastRightClickTime < _doubleClickTime;
        _lastRightClickTime = Time.time;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _groundLayer))
        {
            if (isDoubleClick)
            {
                TryExecuteMove(unit, hit.point);
            }
            else
            {
                PreviewPath(unit, hit.point);
            }
        }
    }

    private void PreviewPath(NetworkUnit unit, Vector3 targetPoint)
    {
        float moveRange = GetEffectiveMoveRange(unit);

        _currentPath = _pathfinder.FindPath(
            unit.transform.position,
            targetPoint,
            moveRange,
            unit.Id
        );

        DrawPath(_currentPath);
        UpdateAttackRangeIndicator(unit);
    }

    private void TryExecuteMove(NetworkUnit unit, Vector3 targetPoint)
    {
        if (unit == null)
        {
            return;
        }

        if (!NetworkPlayersManager.instance.IsUnitOwnedByLocalPlayer(unit))
        {
            return;
        }

        if (Vector3.Distance(unit.transform.position, targetPoint) < 0.5f)
        {
            ClearPath();
            return;
        }

        if (_currentPath.Path == null || _currentPath.Path.Count == 0)
        {
            _currentPath = _pathfinder.FindPath(
                unit.transform.position,
                targetPoint,
                unit.MoveRange,
                unit.Id
            );
        }
        
        if (_currentPath.Path.Count > 0)
        {
            MoveUnitServerRpc(unit.Id, 
                _currentPath.Path[^1], 
                new ServerRpcParams
                {
                    Receive = new ServerRpcReceiveParams
                    {
                        SenderClientId = NetworkManager.Singleton.LocalClientId
                    }
                });
            ClearPath();
        }
    }

    private void UpdateAttackRangeIndicator(NetworkUnit unit)
    {
        if (unit == null || NetworkActionsSystem.instance == null ||
        NetworkActionsSystem.instance.AttackSystem == null)
        {
            return;
        }

        Vector3 correctedPoint = _currentPath.Path[^1] + new Vector3(0, unit.transform.localScale.y, 0);
        
        Vector3 attackRangePos = (_currentPath.Path != null && _currentPath.Path.Count > 0)
        ? correctedPoint
        : unit.transform.position;

        NetworkActionsSystem.instance.AttackSystem.ShowAttackRange(
            attackRangePos,
            unit.AttackRange
        );
    }

    private void DrawPath(Pathfinder.PathResult pathResult)
    {
        if (pathResult.Path == null || pathResult.Path.Count == 0)
        {
            ClearPath();
            return;
        }

        Vector3 startPos = NetworkActionsSystem.instance.UnitSelectionSystem.SelectedUnit.transform.position;
        _pathLineRenderer.positionCount = pathResult.Path.Count + 1;

        _pathLineRenderer.SetPosition(0, startPos + Vector3.up * _pathNodeHeight);
        for (int i = 0; i < pathResult.Path.Count; i++)
        {
            _pathLineRenderer.SetPosition(i + 1, pathResult.Path[i] + Vector3.up * _pathNodeHeight);
        }

        _pathLineRenderer.material.color = pathResult.ReachedDestination
            ? _validPathColor
            : _partialPathColor;

        _pathLineRenderer.enabled = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveUnitServerRpc(int unitId, Vector3 target, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }

        NetworkUnit unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
        {
            return;
        }

        if (!NetworkValidator.ValidateMove(rpcParams.Receive.SenderClientId, unitId))
        {
            return;
        }

        float moveRange = GetEffectiveMoveRange(unit);

        Pathfinder.PathResult validatedPath = _pathfinder.FindPath(
            unit.Position,
            target,
            moveRange,
            unitId
        );

        if (validatedPath.Path.Count == 0)
        {
            return;
        }

        Vector3 calculatedPosition = validatedPath.Path[^1];

        Vector3 finalPosition = GetFinalUnitPosition(calculatedPosition, unit);

        unit.SetPosition(finalPosition);
        NetworkTurnManager.instance.SpendActionServerRpc(ActionTypes.Move);
        NetworkSyncHandler.instance.SyncUnitPositionClientRpc(unit.Id, finalPosition);
    }

    private Vector3 GetFinalUnitPosition(Vector3 targetPos, NetworkUnit unit)
    {
        float groundHeight = GetGroundHeight(targetPos);

        float unitHeight = unit.transform.localScale.y;

        return new Vector3(
            targetPos.x,
            groundHeight + unitHeight,
            targetPos.z
        );
    }

    private float GetGroundHeight(Vector3 position)
    {
        float raycastHeight = 10f;
        float groundHeight = 0f;

        if (Physics.Raycast(
            new Vector3(position.x, raycastHeight, position.z),
            Vector3.down,
            out RaycastHit hit,
            Mathf.Infinity,
            _groundLayer))
        {
            groundHeight = hit.point.y;
        }

        return groundHeight;
    }

    private void ClearPath()
    {
        _pathLineRenderer.positionCount = 0;
        _pathLineRenderer.enabled = false;
        _currentPath = new Pathfinder.PathResult();
    }

    private bool CanMove(NetworkUnit unit)
    {
        if (!NetworkTurnManager.instance.IsLocalPlayersTurn())
        {
            return false;
        }

        return unit.Owner == NetworkActionsSystem.instance.LocalPlayer &&
           (NetworkTurnManager.instance.ActionsRemaining.Value > 0 ||
            NetworkTurnManager.instance.InfiniteMovement.Value) && 
            !NetworkTurnManager.instance.HasMoved.Value;
    }

    public bool HasActivePath()
    {
        return _currentPath.Path != null && _currentPath.Path.Count > 0;
    }

    private float GetEffectiveMoveRange(NetworkUnit unit)
    {
        if (NetworkTurnManager.instance != null &&
            NetworkTurnManager.instance.InfiniteMovement.Value)
        {
            return unit.MoveRange + 1000f;
        }
        return unit.MoveRange;
    }
}
