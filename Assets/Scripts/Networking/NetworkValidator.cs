using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public static class NetworkValidator
{
    public static bool ValidateSelect(ulong clientId, int unitId)
    {
        if (!IsClientConnected(clientId))
            return false;

        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
            return false;

        var playerTeam = GetPlayerTeam(clientId);
        if (playerTeam == Player.None)
            return false;

        bool isUnitOwned = unit.Owner == playerTeam;

        bool isPlayersTurn = NetworkTurnManager.instance.CurrentPlayer.Value == playerTeam;

        bool isValid = isUnitOwned && isPlayersTurn;

        if (!isValid)
            Debug.LogWarning($"Validation failed: Unit {unitId} owned by {unit.Owner}, player team: {playerTeam}, current turn: {NetworkTurnManager.instance.CurrentPlayer.Value}");

        return isValid;
    }

    public static bool ValidateMove(ulong clientId, int unitId)
    {
        if (!IsClientConnected(clientId))
            return false;

        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
            return false;

        var playerTeam = GetPlayerTeam(clientId);
        if (playerTeam == Player.None)
            return false;

        bool isUnitOwned = unit.Owner == playerTeam;

        bool isPlayersTurn = NetworkTurnManager.instance.CurrentPlayer.Value == playerTeam;

        bool canMove = !NetworkTurnManager.instance.HasMoved.Value ||
                  NetworkTurnManager.instance.InfiniteMovement.Value;

        bool isValid = isUnitOwned && isPlayersTurn && canMove;

        if (!isValid)
            Debug.LogWarning($"Validation failed: Unit {unitId} owned by {unit.Owner}, player team: {playerTeam}, current turn: {NetworkTurnManager.instance.CurrentPlayer.Value}");

        return isValid;
    }

    public static bool ValidateAttack(ulong clientId, int unitId)
    {
        if (!IsClientConnected(clientId))
            return false;

        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
            return false;

        var playerTeam = GetPlayerTeam(clientId);
        if (playerTeam == Player.None)
            return false;

        bool isUnitOwned = unit.Owner == playerTeam;

        bool isPlayersTurn = NetworkTurnManager.instance.CurrentPlayer.Value == playerTeam;

        bool canAttack = !NetworkTurnManager.instance.HasAttacked.Value;

        bool isValid = isUnitOwned && isPlayersTurn && canAttack;

        if (!isValid)
            Debug.LogWarning($"Validation failed: Unit {unitId} owned by {unit.Owner}, player team: {playerTeam}, current turn: {NetworkTurnManager.instance.CurrentPlayer.Value}");

        return isValid;
    }

    private static bool IsClientConnected(ulong clientId)
    {
        return GameNetworkManager.instance != null &&
               GameNetworkManager.instance.ConnectedClients.ContainsKey(clientId);
    }

    public static Player GetPlayerTeam(ulong clientId)
    {
        try
        {
            if (GameNetworkManager.instance == null ||
                !GameNetworkManager.instance.ConnectedClients.TryGetValue(clientId, out var client) ||
                client.PlayerObject == null)
            {
                return Player.None;
            }

            var player = client.PlayerObject.GetComponent<NetworkPlayer>();
            return player != null ? player.Team : Player.None;
        }
        catch
        {
            return Player.None;
        }
    }

    public static bool ValidatePlayer(ulong clientId)
    {
        if (!IsClientConnected(clientId))
            return false;

        var playerTeam = GetPlayerTeam(clientId);
        return playerTeam != Player.None &&
               playerTeam == NetworkTurnManager.instance.CurrentPlayer.Value;
    }

    public static bool CanModifyTurnState(ulong clientId)
    {
        return ValidatePlayer(clientId) &&
               NetworkTurnManager.instance.CurrentPlayer.Value == GetPlayer(clientId);
    }

    public static bool ValidateAction(ulong clientId, ActionTypes actionType)
    {
        if (!ValidatePlayer(clientId))
            return false;

        return actionType switch
        {
            ActionTypes.Move => !NetworkTurnManager.instance.HasMoved.Value,
            ActionTypes.Attack => !NetworkTurnManager.instance.HasAttacked.Value,
            _ => false
        };
    }

    public static bool CanMoveUnit(ulong clientId, int unitId)
    {
        return ValidateMove(clientId, unitId) &&
               ValidateAction(clientId, ActionTypes.Move);
    }

    public static bool CanAttackUnit(ulong clientId, int attackerId, int targetId)
    {
        if (!ValidateAttack(clientId, attackerId) ||
            !ValidateAction(clientId, ActionTypes.Attack))
            return false;

        var attacker = NetworkUnitsManager.instance.GetUnitById(attackerId);
        var target = NetworkUnitsManager.instance.GetUnitById(targetId);

        if (attacker == null || target == null || attacker.Owner == target.Owner)
            return false;

        float distance = Vector3.Distance(attacker.Position, target.Position);
        return distance <= attacker.AttackRange && HasLineOfSight(attacker, target);
    }

    private static bool HasLineOfSight(NetworkUnit attacker, NetworkUnit target)
    {
        Vector3 direction = target.Position - attacker.Position;
        float distance = direction.magnitude;

        if (Physics.Raycast(attacker.Position, direction, distance, LayerMask.GetMask("Obstacle")))
            return false;

        return true;
    }

    public static Player GetPlayer(ulong clientId)
    {
        try
        {
            if (GameNetworkManager.instance == null ||
                !GameNetworkManager.instance.ConnectedClients.TryGetValue(clientId, out var client) ||
                client.PlayerObject == null)
            {
                return Player.None;
            }

            var player = client.PlayerObject.GetComponent<NetworkPlayer>();
            return player != null ? player.Team : Player.None;
        }
        catch
        {
            return Player.None;
        }
    }

    public static bool IsUnitOwnedByClient(ulong clientId, int unitId)
    {
        var unit = NetworkUnitsManager.instance.GetUnitById(unitId);
        if (unit == null)
            return false;

        var playerTeam = GetPlayerTeam(clientId);
        return unit.Owner == playerTeam;
    }
}
