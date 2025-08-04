using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkActionSystem : NetworkBehaviour
{
    public static NetworkActionSystem instance
    {
        get; private set;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }            
        else
        {
            Destroy(gameObject);
        }            
    }

    [ServerRpc]
    public void MoveUnitServerRpc(int unitId, Vector3 target, ServerRpcParams rpcParams)
    {
        if (!IsServer)
        {
            return;
        }

        if (!NetworkValidator.ValidateAction(rpcParams.Receive.SenderClientId, unitId))
        {
            return;
        }

        NetworkUnitBehaviorController.instance.MoveUnitServerRpc(unitId, target, rpcParams);
    }

    [ServerRpc]
    public void AttackUnitServerRpc(int attackerId, int targetId, ServerRpcParams rpcParams)
    {
        if (!IsServer)
        {
            return;
        }

        if (!NetworkValidator.ValidateAction(rpcParams.Receive.SenderClientId, attackerId))
        {
            return;
        }

        NetworkUnitBehaviorController.instance.AttackUnitServerRpc(attackerId, targetId, rpcParams);
    }

    [ServerRpc]
    public void SkipTurnServerRpc(ServerRpcParams rpcParams)
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

public enum ActionType
{
    Move,
    Attack
}
