using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkTurnManager : NetworkBehaviour
{
    public static NetworkTurnManager instance
    {
        get; private set;
    }

    public NetworkVariable<Player> CurrentPlayer = new(Player.None);
    public NetworkVariable<int> CurrentTurn = new(1);
    public NetworkVariable<float> TurnTimer = new(0f);
    public NetworkVariable<int> ActionsRemaining = new(0);

    public NetworkVariable<bool> IsSuddenDeath = new(false);
    public NetworkVariable<bool> InfiniteMovement = new(false);

    public GameRulesConfig Rules => _rules;

    [SerializeField] private GameRulesConfig _rules;

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
        ResetTurnState();
        RandomizeFirstPlayer();
    }

    private void RandomizeFirstPlayer()
    {
        CurrentPlayer.Value = Random.Range(0, 2) == 0 ? Player.Player1 : Player.Player2;
        SyncTurnState();
    }

    [ServerRpc]
    public void SpendActionServerRpc(ActionType actionType)
    {
        if (!IsServer)
        {
            return;
        }

        ActionsRemaining.Value--;

        if (ActionsRemaining.Value <= 0)
        {
            EndTurn();
        }
        else
        {
            SyncTurnState();
        }            
    }

    [ServerRpc]
    public void EndTurnServerRpc()
    {
        if (!IsServer)
        {
            return;
        }

        EndTurn();
    }

    private void EndTurn()
    {
        if (CurrentPlayer.Value == Player.Player1)
        {
            CurrentTurn.Value++;
            CheckSuddenDeath();
        }

        SwitchPlayer();
        ResetTurnState();
        SyncTurnState();
    }

    private void CheckSuddenDeath()
    {
        if (_rules.enableTurnLimitCondition &&
        _rules.enableSuddenDeath &&
        CurrentTurn.Value >= _rules.suddenDeathTurn)
        {
            IsSuddenDeath.Value = true;
            // ѕровер€ем условие ничьей
            var victorySystem = NetworkVictorySystem.instance;
            if (victorySystem != null)
            {
                victorySystem.CheckVictory();
            }
        }
    }

    private void SwitchPlayer()
    {
        CurrentPlayer.Value = CurrentPlayer.Value == Player.Player1 ?
            Player.Player2 : Player.Player1;
    }

    private void ResetTurnState()
    {
        TurnTimer.Value = _rules.turnDuration;
        ActionsRemaining.Value = 2; // 2 действи€ за ход
    }

    private void SyncTurnState()
    {
        NetworkSyncHandler.instance.SyncGameStateClientRpc(
            CurrentPlayer.Value,
            CurrentTurn.Value,
            TurnTimer.Value,
            ActionsRemaining.Value,
            IsSuddenDeath.Value,
            InfiniteMovement.Value
        );
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }            

        TurnTimer.Value -= Time.deltaTime;
        if (TurnTimer.Value <= 0)
        {
            EndTurn();
        }            
    }

    [ServerRpc]
    public void SetInfiniteMovementServerRpc(bool enabled)
    {
        if (!IsServer)
        {
            return;
        }

        InfiniteMovement.Value = enabled;
        SyncTurnState();
    }
}
