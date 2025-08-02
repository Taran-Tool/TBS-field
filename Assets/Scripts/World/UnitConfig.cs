using UnityEngine;

[CreateAssetMenu(fileName = "UnitConfig", menuName = "Game/World/Unit Config")]
public class UnitConfig : ScriptableObject
{
    public enum UnitType
    {
        Melee, Ranged
    }

    [Header("Basic Settings")]
    public string unitName = "Unit";
    public GameObject prefab;
    public UnitType type;

    [Header("Stats")]
    public float moveRange = 2f;
    public float attackRange = 5f; 

    [Header("Visual")]
    public Vector3 scale = Vector3.one;
}
