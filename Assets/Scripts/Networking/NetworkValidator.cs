using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public static class NetworkValidator
{
    public static bool ValidateAction(ulong clientId, int unitId)
    {
        // Проверяем подключен ли клиент
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            Debug.Log(11);
            return false;
        }

        // Получаем юнит
        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
        {
            Debug.Log(22);
            return false;
        }

        // Проверяем совпадение владельца
        var client = NetworkManager.Singleton.ConnectedClients[clientId];
        if (client.PlayerObject == null)
        {
            // Для хоста попробую найти PlayerObject через менеджер игроков
            var hostPlayer = NetworkCommandHandler.instance?.GetHostPlayer();
            if (hostPlayer != null && clientId == NetworkManager.Singleton.LocalClientId)
            {
                return ValidateUnitOwnership(hostPlayer.Team, unitId);
            }

            Debug.LogError($"PlayerObject not found for client {clientId}");
            return false;
        }


        var player = client.PlayerObject.GetComponent<NetworkPlayer>();
        return ValidateUnitOwnership(player.Team, unitId);
    }

    private static bool ValidateUnitOwnership(Player playerTeam, int unitId)
    {
        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
        {
            Debug.LogError($"Unit {unitId} not found");
            return false;
        }

        bool isValid = unit.Owner == playerTeam &&
                     NetworkTurnManager.instance.CurrentPlayer.Value == playerTeam;

        return isValid;
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
