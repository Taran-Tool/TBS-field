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
    private GameRulesConfig _rules;

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

        _rules = Resources.Load<GameRulesConfig>($"Rules/{configName}");
        SetupConditions();
    }

    private void SetupConditions()
    {
        if (_rules.enableUnitCountCondition)
        {
            _conditions.Add(new UnitCountVictoryCondition());
        }

        if (_rules.enableTurnLimitCondition && _rules.enableSuddenDeath)
        {
            _conditions.Add(new Turn15DrawCondition());
        }           
    }

    public void CheckVictory()
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
                NetworkSyncHandler.instance.AnnounceVictoryClientRpc(result.Value);
                break;
            }
        }
    }
}
