using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public static class NetworkValidator
{
    public static bool ValidateAction(ulong clientId, int unitId)
    {
        if (!IsPlayerActive(clientId))
        {
            return false;
        }
            
        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        var player = GetPlayer(clientId);

        return unit != null &&
               unit.Owner == player &&
               NetworkTurnManager.instance.CurrentPlayer.Value == player;
    }

    public static bool ValidatePlayer(ulong clientId)
    {
        return IsPlayerActive(clientId) &&
               GetPlayer(clientId) == NetworkTurnManager.instance.CurrentPlayer.Value;
    }

    private static bool IsPlayerActive(ulong clientId)
        => NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId);

    private static Player GetPlayer(ulong clientId)
        => NetworkManager.Singleton.ConnectedClients[clientId]
            .PlayerObject.GetComponent<NetworkPlayer>().Team;
}
