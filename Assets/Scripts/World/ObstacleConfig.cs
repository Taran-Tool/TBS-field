using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleConfig", menuName = "Game/World/Obstacle Config")]
public class ObstacleConfig : ScriptableObject
{
    public enum ObstacleType
    {
        Rock, Tree, Building, Ruin, Barrier, Fallen
    }

    public ObstacleType type;
    public PrimitiveType primitiveType = PrimitiveType.Cube;
    public Color color = Color.gray;
    public Vector3 scale = Vector3.one;
    [Range(1, 10)] public int minCount = 5;
    [Range(1, 20)] public int maxCount = 10;
}
