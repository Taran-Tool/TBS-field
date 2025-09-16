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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Initial Sync Methods
    //Старый механизм
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

        // 3. Помечаем синхронизацию как завершенную
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
            if (GameNetworkManager.instance.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
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

        if (GameNetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
        {
            RegisterObjectClientRpc(objectId, objectType);
        }
    }

    [ClientRpc]
    private void RegisterObjectClientRpc(ulong objectId, string objectType)
    {
        if (_isInitialSyncComplete &&
            GameNetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObj))
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
        bool isHost = NetworkPlayersManager.instance.GetHostPlayer()?.Team ==
                 NetworkPlayersManager.instance.GetLocalPlayer();

        if (GameHUD.instance != null)
        {
            GameHUD.instance.ShowVictoryPanel(result, isHost);
        }
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
    #endregion
}
