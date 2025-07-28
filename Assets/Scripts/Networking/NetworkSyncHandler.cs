using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkSyncHandler : NetworkBehaviour
{
    public static NetworkSyncHandler instance
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
    public void SyncGameStateServerRpc(ulong clientId)
    {
        Debug.Log("Start Sync!");
        //отправка только запросившему
        var rpcParams = CreateRpcParamsFor(clientId);
        //Синхронизирую начальное состояние
        //карта мира
        //SyncMapClientRpc(WorldGenerator.Instance.GetMapData());
        //SyncUnitsClientRpc(UnitManager.Instance.GetAllUnitsData());
        //юниты
        //var unitsData = UnitSystem.Instance.GetAllUnitsData();
        //ходы
        //var currentPlayer = TurnManager.Instance.CurrentPlayer;

        //последовательная синхронизация
        //SyncMapClientRpc(mapData, rpcParams);
        //SyncUnitsClientRpc(unitsData, rpcParams);
        //SyncGameStateClientRpc(currentPlayer, rpcParams);
    }

    private ClientRpcParams CreateRpcParamsFor(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }

    /*[ClientRpc]
    private void SyncMapClientRpc(MapData mapData, ClientRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            WorldGenerator.Instance.LoadMap(mapData);
        }            
    }*/

    /*[ClientRpc]
    private void SyncUnitsClientRpc(UnitData[] unitsData, ClientRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            UnitManager.Instance.SpawnUnitsFromData(unitsData);
        }            
    }*/

    /*[ClientRpc]
   private void SyncGameStateClientRpc(Player currentPlayer, ClientRpcParams rpcParams = default)
   {
       if (!IsServer)
       {
           TurnManager.Instance.SetCurrentPlayer(currentPlayer);
       }            
   }*/
}
