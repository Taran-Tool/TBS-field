using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitBehaviorController : NetworkBehaviour
{
    public static NetworkUnitBehaviorController instance
    {
        get; private set;
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    [ServerRpc]
    public void MoveUnitServerRpc(int unitId, Vector3 target, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }

        if (!NetworkValidator.ValidateAction(rpcParams.Receive.SenderClientId, unitId))
        {
            return;
        }

        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
        {
            return;
        }

        if (TryCalculatePath(unit, target, out var path))
        {
            ExecuteMove(unit, path);
            NetworkTurnManager.instance.SpendActionServerRpc(ActionType.Move);
        }
    }

    private bool TryCalculatePath(NetworkUnit unit, Vector3 target, out List<Vector3> path)
    {
        path = new List<Vector3>();

        // �������� ���������� (��� ����� �����������)
        float distance = Vector3.Distance(unit.Position, target);
        if (distance > unit.MoveRange)
        {
            return false;
        }            

        // TODO: ����������� ����������� A* ��� ������ �������� ������ ����
        // ���� ���������� ���������� ������ - ������ �����
        path.Add(target);
        return true;
    }

    private void ExecuteMove(NetworkUnit unit, List<Vector3> path)
    {
        // � �������� ���� ����� ������ ���� �������� ��������
        // ��� ������������ ������ ������������� � �������� �����
        unit.SetPosition(path[path.Count - 1]);

        // �������������� ������� ��� ���� ��������
        NetworkSyncHandler.instance.SyncUnitPositionClientRpc(unit.Id, unit.Position);
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

        if (IsInAttackRange(attacker, target))
        {
            ExecuteAttack(attacker, target);
            NetworkTurnManager.instance.SpendActionServerRpc(ActionType.Attack);
            NetworkVictorySystem.instance.CheckVictory();
        }
    }

    private bool IsInAttackRange(NetworkUnit attacker, NetworkUnit target)
    {
        float distance = Vector3.Distance(attacker.Position, target.Position);
        return distance <= attacker.AttackRange;

        // TODO: �������� �������� �� ����������� (LineOfSight)
    }

    private void ExecuteAttack(NetworkUnit attacker, NetworkUnit target)
    {
        // � �������� ���� ����� ������ ���� �������� �����
        target.DestroyServerRpc();

        // �������������� ��������� �����
        NetworkSyncHandler.instance.SyncAttackResultClientRpc(target.Id);
    }

    [ServerRpc]
    public void SkipTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }

        if (NetworkValidator.ValidatePlayer(rpcParams.Receive.SenderClientId))
        {
            NetworkTurnManager.instance.EndTurnServerRpc();
        }
    }
}
