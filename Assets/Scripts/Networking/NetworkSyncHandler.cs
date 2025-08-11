using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkSyncHandler : NetworkBehaviour
{
    public static NetworkSyncHandler instance
    {
        get; private set;
    }

    private const int BATCH_SIZE = 16;
    private Dictionary<ulong, NetworkObject> _pendingSyncObjects = new();
    private bool _isInitialSyncComplete = false;

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

    #region Initial Sync Methods
    [ServerRpc]
    public void RequestInitialSyncServerRpc(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }  
        StartCoroutine(SendInitialSyncData(clientId));
    }

    private IEnumerator SendInitialSyncData(ulong clientId)
    {
        // 0. Синхронизация земли
        var ground = GameObject.FindGameObjectWithTag("Ground");
        if (ground != null)
        {
            SyncGroundClientRpc(
                ground.transform.position,
                ground.transform.localScale,
                CreateRpcParamsFor(clientId)
            );
        }

        // 1. Синхронизация мира (препятствий)
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        yield return StartCoroutine(SyncObjectsBatch(obstacles, clientId, "Obstacle"));

        // 2. Синхронизация юнитов
        var units = GameObject.FindGameObjectsWithTag("Unit");
        yield return StartCoroutine(SyncObjectsBatch(units, clientId, "Unit"));

        // 3. Синхронизация состояния игры
        SyncGameStateClientRpc(
            NetworkTurnManager.instance.CurrentPlayer.Value,
            NetworkTurnManager.instance.CurrentTurn.Value,
            NetworkTurnManager.instance.TurnTimer.Value,
            NetworkTurnManager.instance.ActionsRemaining.Value,
            NetworkTurnManager.instance.IsSuddenDeath.Value,
            NetworkTurnManager.instance.InfiniteMovement.Value,
            NetworkTurnManager.instance.HasMoved.Value,
            NetworkTurnManager.instance.HasAttacked.Value,
            CreateRpcParamsFor(clientId)
        );

        // 4. Помечаем синхронизацию как завершенную
        CompleteInitialSyncClientRpc(CreateRpcParamsFor(clientId));

        Debug.Log($"Initial sync completed for client {clientId}");
    }

    [ClientRpc]
    private void SyncGroundClientRpc(Vector3 position, Vector3 scale, ClientRpcParams rpcParams = default)
    {
        var ground = GameObject.FindGameObjectWithTag("Ground");

        ground.transform.position = position;
        ground.transform.localScale = scale;
    }

    private IEnumerator SyncObjectsBatch(GameObject[] objects, ulong clientId, string objectType)
    {
        for (int i = 0; i < objects.Length; i += BATCH_SIZE)
        {
            int endIndex = Mathf.Min(i + BATCH_SIZE, objects.Length);
            var batchIds = new List<ulong>();

            for (int j = i; j < endIndex; j++)
            {
                var obj = objects[j];
                if (obj == null)
                {
                    continue;
                }                        

                var netObj = obj.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    continue;
                }            
                batchIds.Add(netObj.NetworkObjectId);
            }

            SyncObjectsBatchClientRpc(batchIds.ToArray(), objectType, CreateRpcParamsFor(clientId));
            yield return null;
        }
    }

    [ClientRpc]
    private void SyncObjectsBatchClientRpc(ulong[] networkObjectIds, string objectType, ClientRpcParams rpcParams = default)
    {
        foreach (var objectId in networkObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
            {
                if (!_pendingSyncObjects.ContainsKey(objectId))
                {
                    _pendingSyncObjects.Add(objectId, netObj);
                }
            }
        }
    }

    [ClientRpc]
    private void CompleteInitialSyncClientRpc(ClientRpcParams rpcParams = default)
    {
        _isInitialSyncComplete = true;
        Debug.Log("Initial sync complete");
    }
    #endregion

    #region Dynamic Object Sync
    [ServerRpc]
    public void RegisterObjectServerRpc(ulong objectId, string objectType)
    {
        if (!IsServer)
        {
            return;
        }            

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
        {
            RegisterObjectClientRpc(objectId, objectType);
        }
    }

    [ClientRpc]
    private void RegisterObjectClientRpc(ulong objectId, string objectType)
    {
        if (_isInitialSyncComplete &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
        {
            if (!_pendingSyncObjects.ContainsKey(objectId))
            {
                _pendingSyncObjects.Add(objectId, netObj);
            }
        }
    }
    #endregion

    #region Game State Sync
    [ClientRpc]
    public void SyncGameStateClientRpc(
        Player currentPlayer,
        int currentTurn,
        float timer,
        int actionsRemaining,
        bool isSuddenDeath,
        bool infiniteMovement,
        bool hasMoved,
        bool hasAttacked,
        ClientRpcParams rpcParams = default)
    {
        NetworkTurnManager.instance.CurrentPlayer.Value = currentPlayer;
        NetworkTurnManager.instance.CurrentTurn.Value = currentTurn;
        NetworkTurnManager.instance.TurnTimer.Value = timer;
        NetworkTurnManager.instance.ActionsRemaining.Value = actionsRemaining;
        NetworkTurnManager.instance.IsSuddenDeath.Value = isSuddenDeath;
        NetworkTurnManager.instance.InfiniteMovement.Value = infiniteMovement;
        NetworkTurnManager.instance.HasMoved.Value = hasMoved;
        NetworkTurnManager.instance.HasAttacked.Value = hasAttacked;
    }

    [ClientRpc]
    public void SyncUnitPositionClientRpc(int unitId, Vector3 position)
    {
        if (NetworkUnitsManager.instance.GetUnitById(unitId) is NetworkUnit unit)
        {
            unit.transform.position = position;
        }
    }

    [ClientRpc]
    public void SyncAttackResultClientRpc(int targetId)
    {
        var target = NetworkUnitsManager.instance?.GetUnitById(targetId);
        if (target == null)
        {
            return;
        }            

        if (target.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
        else
        {
            Destroy(target.gameObject);
        }
    }

    [ClientRpc]
    public void AnnounceVictoryClientRpc(GameResult result)
    {
        // Реализация отображения победы
        switch (result)
        {
            case GameResult.Player1Win:
            Debug.Log("Player 1 Wins!");
            break;
            case GameResult.Player2Win:
            Debug.Log("Player 2 Wins!");
            break;
            case GameResult.Draw:
            Debug.Log("Game ended in Draw!");
            break;
        }

        // GameUI.Instance.ShowVictoryScreen(result);
    }
    #endregion

    #region Helper Methods
    private ClientRpcParams CreateRpcParamsFor(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }

    public bool IsObjectSynced(ulong objectId)
    {
        return _pendingSyncObjects.ContainsKey(objectId) || _isInitialSyncComplete;
    }
    #endregion

    #region Network Events
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient && !IsServer)
        {
            RequestInitialSyncServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }
    #endregion
    
}
