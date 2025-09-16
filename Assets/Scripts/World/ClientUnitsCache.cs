using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ClientUnitsCache
{
    private static Dictionary<int, NetworkUnit> _unitsCache = new Dictionary<int, NetworkUnit>();

    public static void RegisterUnit(NetworkUnit unit)
    {
        if (!_unitsCache.ContainsKey(unit.Id))
        {
            _unitsCache.Add(unit.Id, unit);
        }
    }

    public static void UnregisterUnit(int unitId)
    {
        _unitsCache.Remove(unitId);
    }

    public static NetworkUnit GetUnitById(int unitId)
    {
        _unitsCache.TryGetValue(unitId, out var unit);
        return unit;
    }

    public static List<NetworkUnit> GetPlayerUnits(NetworkPlayer player)
    {
        List<NetworkUnit> units = new List<NetworkUnit>();

        foreach (int unitId in player.UnitIds)
        {
            if (_unitsCache.TryGetValue(unitId, out var unit) && unit != null)
            {
                units.Add(unit);
            }
            else
            {
                Debug.LogWarning($"Юнит с ID {unitId} не найден в кэше!");
            }
        }

        return units;
    }

    public static void ClearCache()
    {
        _unitsCache.Clear();
    }
}
