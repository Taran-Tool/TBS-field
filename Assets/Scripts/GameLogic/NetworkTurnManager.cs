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

    public NetworkVariable<bool> InfiniteMovement = new(false);
    public NetworkVariable<bool> infiniteMode = new(false);

    public NetworkVariable<bool> HasMoved = new(false);
    public NetworkVariable<bool> HasAttacked = new(false);

    [SerializeField] private GameRulesConfig _rules;

    private bool _isInitialized = false;

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
        
        RandomizeFirstPlayer();
        SetStartData();

        _isInitialized = true;
    }

    private void RandomizeFirstPlayer()
    {        
        Player randomPlayer = Random.Range(0, 2) == 0 ? Player.Player1 : Player.Player2;
        CurrentPlayer.Value = randomPlayer;
    }

    private void SetStartData()
    {
        if (!IsServer)
        {
            return;
        }
        CurrentTurn.Value = 1;

        ResetTurnState();    
    }

    private void ResetTurnState()
    {
        if (!IsServer)
        {
            return;
        }

        TurnTimer.Value = _rules.turnDuration;
        ActionsRemaining.Value = 2;  
        HasMoved.Value = false;
        HasAttacked.Value = false;
    }

    

    [ServerRpc]
    public void SpendActionServerRpc(ActionTypes actionType)
    {
        if (!IsServer)
        {
            return;
        }

        // Проверяю, можно ли выполнить это действие
        if ((actionType == ActionTypes.Move && HasMoved.Value) ||
            (actionType == ActionTypes.Attack && HasAttacked.Value))
        {
            return;
        }

        // Отмечаю выполненное действие
        if (actionType == ActionTypes.Move)
        {
            HasMoved.Value = true;
        }
        else if (actionType == ActionTypes.Attack)
        {
            HasAttacked.Value = true;
        }

        ActionsRemaining.Value = ActionsRemaining.Value - 1;

        if (ActionsRemaining.Value <= 0)
        {
            EndTurn();
        }           
    }

    [ServerRpc(RequireOwnership = false)]
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
        if (!IsServer)
        {
            return;
        }

        if (CurrentPlayer.Value == Player.Player1)
        {
            CurrentTurn.Value = CurrentTurn.Value + 1;
        }

        if (_rules.enableInfiniteMoves &&
            CurrentTurn.Value >= _rules.infiniteMovesTurn)
        {
            infiniteMode.Value = true;
        }

        SwitchPlayer();
        ResetTurnState();

        NetworkVictorySystem.instance?.CheckAllConditions();
    }

    private void SwitchPlayer()
    {
        if (!IsServer)
        {
            return;
        }
        CurrentPlayer.Value = CurrentPlayer.Value == Player.Player1 ?
            Player.Player2 : Player.Player1;
    }

    private void Update()
    {
        if (!IsServer || !_isInitialized)
        {
            return;
        }

        TurnTimer.Value -= Time.deltaTime;
        if (TurnTimer.Value <= 0)
        {
            EndTurn();
        }            
    }

    public bool IsLocalPlayersTurn()
    {
        return CurrentPlayer.Value == NetworkPlayersManager.instance.GetLocalPlayer();
    }

}
