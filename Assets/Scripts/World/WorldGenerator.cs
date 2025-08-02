using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WorldGenerator : NetworkBehaviour
{
    public enum SpawnZoneType
    {
        HorizontalOpposite, // Противоположные стороны по X
        VerticalOpposite,   // Противоположные стороны по Z
        DiagonalCorners     // Диагональные углы
    }

    public static WorldGenerator instance
    {
        get; private set;
    }

    [Header("Map Settings")]
    [SerializeField] private int _mapWidth = 32;
    [SerializeField] private int _mapHeight = 32;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private float _spawnZoneSize = 10f;
    [SerializeField] private Material _groundMaterial;

    [Header("Obstacles")]
    [SerializeField] private List<ObstacleConfig> _obstacleConfigs;

    private SpawnZoneType _currentSpawnZoneType;
    private Vector3 _team1SpawnCenter;
    private Vector3 _team2SpawnCenter;
    private GameObject _ground;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void GenerateMap()
    {
        if (!IsServer)
            return;

        ClearMap();
        RandomizeSpawnZoneType();
        SetupSpawnZones();
        CreateGround();
        GenerateObstacles();
    }

    private void RandomizeSpawnZoneType()
    {
        _currentSpawnZoneType = (SpawnZoneType) Random.Range(0, 3);
        Debug.Log($"Selected spawn type: {_currentSpawnZoneType}");
    }

    private void SetupSpawnZones()
    {
        switch (_currentSpawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            _team1SpawnCenter = new Vector3(
                _spawnZoneSize / 2f,
                0,
                _mapHeight * _cellSize / 2f
            );
            _team2SpawnCenter = new Vector3(
                _mapWidth * _cellSize - _spawnZoneSize / 2f,
                0,
                _mapHeight * _cellSize / 2f
            );
            break;

            case SpawnZoneType.VerticalOpposite:
            _team1SpawnCenter = new Vector3(
                _mapWidth * _cellSize / 2f,
                0,
                _spawnZoneSize / 2f
            );
            _team2SpawnCenter = new Vector3(
                _mapWidth * _cellSize / 2f,
                0,
                _mapHeight * _cellSize - _spawnZoneSize / 2f
            );
            break;

            case SpawnZoneType.DiagonalCorners:
            _team1SpawnCenter = new Vector3(
                _spawnZoneSize / 2f,
                0,
                _spawnZoneSize / 2f
            );
            _team2SpawnCenter = new Vector3(
                _mapWidth * _cellSize - _spawnZoneSize / 2f,
                0,
                _mapHeight * _cellSize - _spawnZoneSize / 2f
            );
            break;
        }
    }

    private void CreateGround()
    {
        _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ground.name = "Ground";
        _ground.transform.position = new Vector3(
            _mapWidth * _cellSize / 2f,
            -0.5f,
            _mapHeight * _cellSize / 2f
        );
        _ground.transform.localScale = new Vector3(
            _mapWidth * _cellSize,
            1f,
            _mapHeight * _cellSize
        );

        if (_groundMaterial != null)
            _ground.GetComponent<Renderer>().material = _groundMaterial;

        _ground.AddComponent<NetworkObject>().Spawn();
        _ground.tag = "Ground";
    }

    private void GenerateObstacles()
    {
        foreach (var config in _obstacleConfigs)
        {
            int count = Random.Range(config.minCount, config.maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                if (TryFindValidPosition(config, out Vector3 position, out Quaternion rotation))
                {
                    CreateObstacle(config, position, rotation);
                }
            }
        }
    }

    private bool TryFindValidPosition(ObstacleConfig config, out Vector3 position, out Quaternion rotation)
    {
        int attempts = 0;
        const int maxAttempts = 100;
        position = Vector3.zero;
        rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        Vector3 halfExtents = config.scale / 1.25f;
        float obstacleRadius = Mathf.Max(halfExtents.x, halfExtents.z);

        while (attempts < maxAttempts)
        {
            attempts++;

            position = new Vector3(
                Random.Range(halfExtents.x, _mapWidth * _cellSize - halfExtents.x),
                0,
                Random.Range(halfExtents.z, _mapHeight * _cellSize - halfExtents.z)
            );

            // Проверка зон спавна игроков
            if (Vector3.Distance(position, _team1SpawnCenter) <= _spawnZoneSize + obstacleRadius ||
                Vector3.Distance(position, _team2SpawnCenter) <= _spawnZoneSize + obstacleRadius)
            {
                continue;
            }

            // Проверка коллизий
            if (!Physics.CheckBox(position, halfExtents, rotation, LayerMask.GetMask("Obstacle")))
            {
                return true;
            }
        }
        return false;
    }

    private void CreateObstacle(ObstacleConfig config, Vector3 position, Quaternion rotation)
    {
        var obstacle = GameObject.CreatePrimitive(config.primitiveType);
        obstacle.transform.position = position + Vector3.up * (config.scale.y / 2f);
        obstacle.transform.localScale = config.scale;
        obstacle.transform.rotation = rotation;

        obstacle.GetComponent<Renderer>().material.color = config.color;
        obstacle.GetComponent<Collider>().isTrigger = false;
        obstacle.AddComponent<NetworkObject>().Spawn();
        obstacle.tag = "Obstacle";
        obstacle.name = $"{config.type}_{NetworkObjectId}";
    }

    private void ClearMap()
    {
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obj in obstacles)
        {
            if (obj.TryGetComponent<NetworkObject>(out var netObj))
                netObj.Despawn();
            Destroy(obj);
        }

        if (_ground != null)
        {
            if (_ground.TryGetComponent<NetworkObject>(out var groundNetObj))
                groundNetObj.Despawn();
            Destroy(_ground);
        }
    }

    public Dictionary<Player, Vector3> GetTeamSpawnPoints()
    {
        return new Dictionary<Player, Vector3>
        {
            { Player.Player1, _team1SpawnCenter },
            { Player.Player2, _team2SpawnCenter }
        };
    }

    /*private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(_team1SpawnCenter, _spawnZoneSize);
        Gizmos.DrawSphere(_team2SpawnCenter, _spawnZoneSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_team1SpawnCenter, 1f);
        Gizmos.DrawWireSphere(_team2SpawnCenter, 1f);
    }*/

    public float GetSpawnZoneSize()
    {
        return _spawnZoneSize;
    }

    public bool IsPositionInsideMap(Vector3 position)
    {
        float offset = 1f;
        return position.x >= offset &&
               position.x <= _mapWidth * _cellSize - offset &&
               position.z >= offset &&
               position.z <= _mapHeight * _cellSize - offset;
    }
}
