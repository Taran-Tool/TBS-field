using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitAttackSystem : NetworkBehaviour
{
    private GameObject _attackRangeIndicator;

    public void ShowAttackRange(Vector3 position, float radius)
    {
        if (_attackRangeIndicator == null)
        {
            CreateIndicator();
        }            

        _attackRangeIndicator.transform.position = position;
        _attackRangeIndicator.transform.localScale = Vector3.one * radius * 2;
        _attackRangeIndicator.SetActive(true);
    }

    private void CreateIndicator()
    {
        _attackRangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _attackRangeIndicator.GetComponent<Collider>().enabled = false;
        var mat = new Material(Shader.Find("Transparent/Diffuse"));
        mat.color = new Color(1, 0, 0, 0.2f);
        _attackRangeIndicator.GetComponent<Renderer>().material = mat;
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
            NetworkTurnManager.instance.SpendActionServerRpc(ActionTypes.Attack);
            NetworkVictorySystem.instance.CheckVictory();
        }

        bool IsInAttackRange(NetworkUnit attacker, NetworkUnit target)
        {
            float distance = Vector3.Distance(attacker.Position, target.Position);
            return distance <= attacker.AttackRange;

            // TODO: Добавить проверку на препятствия (LineOfSight)
        }

        void ExecuteAttack(NetworkUnit attacker, NetworkUnit target)
        {
            // В реальной игре здесь должна быть анимация атаки
            target.DestroyServerRpc();

            // Синхронизируем результат атаки
            NetworkSyncHandler.instance.SyncAttackResultClientRpc(target.Id);
        }
    }
}
