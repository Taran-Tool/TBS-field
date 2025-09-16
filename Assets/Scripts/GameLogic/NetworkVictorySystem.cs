using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkVictorySystem : NetworkBehaviour
{
    public static NetworkVictorySystem instance
    {
        get; private set;
    }

    private List<IVictoryCondition> _conditions = new();

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
    public void InitializeServerRpc(string configName)
    {
        if (!IsServer)
        {
            return;
        }

        SetupConditions();
    }

    private void SetupConditions()
    {
        _conditions.Clear();

        _conditions.Add(new UnitCountVictoryCondition());
        _conditions.Add(new Turn15InfiniteMoves());         
    }

    public void CheckAllConditions()
    {

        if (!IsServer)
        {
            return;
        }
        
        foreach (var condition in _conditions)
        {
            var result = condition.CheckCondition();

            if (result.HasValue)
            {
                HandleVictoryResult(result.Value);
                break;
            }
        }
    }

    private void HandleVictoryResult(GameResult result)
    {
        switch (result)
        {
            case GameResult.InfiniteMoves:
            NetworkTurnManager.instance.InfiniteMovement.Value = true;
            break;

            default:
            NetworkSyncHandler.instance.AnnounceVictoryClientRpc(result);
            break;
        }
    }
}
