using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitAttackSystem : NetworkBehaviour
{
    private GameObject _attackRangeIndicator;
    private List<NetworkUnit> _targetsInRange = new List<NetworkUnit>();
    private NetworkUnit _selectedUnit;

    public void ShowAttackRange(Vector3 position, float radius)
    {
        if (_attackRangeIndicator == null)
        {
            CreateIndicator();
        }            

        _attackRangeIndicator.transform.position = position;
        _attackRangeIndicator.transform.localScale = Vector3.one * radius * 2;
        _attackRangeIndicator.SetActive(true);

        FindTargetsInRange(position, radius);
    }

    private void CreateIndicator()
    {
        _attackRangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _attackRangeIndicator.GetComponent<Collider>().enabled = false;
        var mat = new Material(Shader.Find("Transparent/Diffuse"));
        mat.color = new Color(1, 0, 0, 0.2f);
        _attackRangeIndicator.GetComponent<Renderer>().material = mat;
    }

    private void FindTargetsInRange(Vector3 center, float radius)
    {
        List<NetworkUnit> validTargets = new List<NetworkUnit>();

        foreach (var unit in _targetsInRange)
        {
            if (unit != null && unit.gameObject != null)
            {
                HighlightTarget(unit, false);
                validTargets.Add(unit);
            }
        }
        _targetsInRange = validTargets;

        if (_selectedUnit == null || _selectedUnit.gameObject == null)
            return;

        foreach (var unit in NetworkUnitsManager.instance.GetAllUnits())
        {
            if (unit == null || unit.gameObject == null)
                continue;
            if (unit.Owner == _selectedUnit.Owner)
                continue;

            float distance = Vector3.Distance(center, unit.Position);
            if (distance <= radius && HasLineOfSight(_selectedUnit, unit))
            {
                _targetsInRange.Add(unit);
                HighlightTarget(unit, true);
            }
        }
    }

    private bool HasLineOfSight(NetworkUnit attacker, NetworkUnit target)
    {
        Vector3 direction = target.Position - attacker.Position;
        float distance = direction.magnitude;

        // Препятствия
        if (Physics.Raycast(attacker.Position, direction, distance,
            LayerMask.GetMask("Obstacle")))
        {
            return false;
        }

        return true;
    }

    private void HighlightTarget(NetworkUnit unit, bool highlight)
    {
        if (unit == null || unit.gameObject == null)
            return;

        Transform indicator = unit.transform.Find("Indicator");
        if (indicator == null)
            return;

        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        if (indicatorRenderer == null)
            return;

        // Меняем цвет и активность в зависимости от типа подсветки
        if (highlight)
        {
            indicatorRenderer.material.color = Color.red;
            indicator.gameObject.SetActive(true);
        }
        else
        {
            if (unit != null && unit.gameObject != null)
            {
                if (NetworkActionsSystem.instance != null &&
                    NetworkActionsSystem.instance.UnitSelectionSystem != null &&
                    unit == NetworkActionsSystem.instance.UnitSelectionSystem.SelectedUnit)
                {
                    indicatorRenderer.material.color = Color.green;
                }
                else if (NetworkCommandHandler.instance != null &&
                        unit.Owner == NetworkCommandHandler.instance.GetLocalPlayer())
                {
                    indicatorRenderer.material.color = Color.blue;
                }
                else
                {
                    indicator.gameObject.SetActive(false);
                }
            }
            else
            {
                indicator.gameObject.SetActive(false);
            }
        }
    }

    public void HandleAttackInput(NetworkUnit selectedUnit)
    {
        _selectedUnit = selectedUnit;

        if (Input.GetMouseButtonDown(1))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity,
                LayerMask.GetMask("Unit")))
            {
                var targetUnit = hit.collider.GetComponent<NetworkUnit>();
                if (targetUnit != null && targetUnit.Owner != selectedUnit.Owner)
                {
                    TryAttackUnit(selectedUnit, targetUnit);
                }
            }
        }
    }

    private void TryAttackUnit(NetworkUnit attacker, NetworkUnit target)
    {
        if (IsInAttackRange(attacker, target) && HasLineOfSight(attacker, target))
        {
            AttackUnitServerRpc(attacker.Id, target.Id);
        }
    }

    [ServerRpc]
    public void AttackUnitServerRpc(int attackerId, int targetId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }
        if (!NetworkValidator.ValidateAction(rpcParams.Receive.SenderClientId, attackerId))
        {
            return;
        }

        // Проверяю, не атаковал ли уже игрок в этот ход
        if (NetworkTurnManager.instance.HasAttacked.Value)
        {
            return;
        }

        var attacker = NetworkUnitsManager.instance.GetUnitById(attackerId);
        var target = NetworkUnitsManager.instance.GetUnitById(targetId);

        if (attacker == null || target == null)
        {
            return;
        }
        if (attacker.Owner == target.Owner)
        {
            return;
        }

        if (IsInAttackRange(attacker, target) && HasLineOfSight(attacker, target))
        {
            ExecuteAttack(attacker, target);
            NetworkTurnManager.instance.SpendActionServerRpc(ActionTypes.Attack);
            NetworkVictorySystem.instance.CheckVictory();
        }
    }

    private bool IsInAttackRange(NetworkUnit attacker, NetworkUnit target)
    {
        float distance = Vector3.Distance(attacker.Position, target.Position);
        return distance <= attacker.AttackRange;
    }

    private void ExecuteAttack(NetworkUnit attacker, NetworkUnit target)
    {
        if (attacker == null || target == null)
            return;
        // В реальной игре здесь должна быть анимация атаки
        if (target != null && target.gameObject != null)
        {
            target.DestroyServerRpc();
        }

        // Синхронизирую результат атаки
        NetworkSyncHandler.instance.SyncAttackResultClientRpc(target.Id);
    }

    public void ClearTargets()
    {
        foreach (var unit in _targetsInRange)
        {
            if (unit != null && unit.gameObject != null)
            {
                HighlightTarget(unit, false);
            }
        }
        _targetsInRange.Clear();

        if (_attackRangeIndicator != null)
        {
            _attackRangeIndicator.SetActive(false);
        }
    }
}
