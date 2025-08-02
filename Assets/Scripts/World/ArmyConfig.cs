using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ArmyConfig", menuName = "Game/World/Army Config")]
public class ArmyConfig : ScriptableObject
{
    [Serializable]
    public class UnitSlot
    {
        public UnitConfig unitConfig;
        [Range(1, 16)] public int count = 4;
    }

    public string armyName = "Default Army";
    public UnitSlot[] units = new UnitSlot[2];
}
