using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator instance
    {
        get; private set;
    }

    [Header("Map Settings")]
    [SerializeField] private int _mapWidth = 64;
    [SerializeField] private int _mapHeight = 64;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private float _spawnZoneSize = 10f;
    [SerializeField] private Material _groundMaterial;

    [Header("Obstacles")]
    [SerializeField] private List<ObstacleConfig> _obstacleConfigs;

    private Vector2 _team1SpawnZone;
    private Vector2 _team2SpawnZone;
    private SpawnZoneType _spawnZoneType;
    private HashSet<Vector3> _occupiedPositions = new HashSet<Vector3>();
    private GameObject _ground;

    private enum SpawnZoneType
    {
        HorizontalOpposite, // ��������������� ������� �� X
        VerticalOpposite,   // ��������������� ������� �� Z
        DiagonalCorners     // ������������ ����
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

    public void GenerateMap()
    {
        if (!IsServer)
        {
            return;
        }            

        ClearMap();
        SetupSpawnZones();
        CreateGround();
        GenerateObstacles();
    }

    private void SetupSpawnZones()
    {
        // �������� ������� ��� ������������ ���
        _spawnZoneType = (SpawnZoneType) Random.Range(0, 3);

        switch (_spawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            // ����� � ������ �������
            _team1SpawnZone = new Vector2(1, _spawnZoneSize);
            _team2SpawnZone = new Vector2(_mapWidth - _spawnZoneSize - 1, _mapWidth - 1);
            break;

            case SpawnZoneType.VerticalOpposite:
            // ������ � ������� �������
            _team1SpawnZone = new Vector2(1, _spawnZoneSize);
            _team2SpawnZone = new Vector2(_mapHeight - _spawnZoneSize - 1, _mapHeight - 1);
            break;

            case SpawnZoneType.DiagonalCorners:
            // ��������� ������������ ����
            if (Random.value > 0.5f)
            {
                // ����� ������� � ������ ������
                _team1SpawnZone = new Vector2(1, _spawnZoneSize);
                _team2SpawnZone = new Vector2(_mapWidth - _spawnZoneSize - 1, _mapWidth - 1);
            }
            else
            {
                // ������ ������� � ����� ������
                _team1SpawnZone = new Vector2(_mapWidth - _spawnZoneSize - 1, _mapWidth - 1);
                _team2SpawnZone = new Vector2(1, _spawnZoneSize);
            }
            break;
        }
    }

    private void CreateGround()
    {
        // ������ ��� (������� ���)
        _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _ground.name = "Ground";
        _ground.transform.position = new Vector3(_mapWidth / 2f * _cellSize, -0.5f, _mapHeight / 2f * _cellSize);
        _ground.transform.localScale = new Vector3(_mapWidth * _cellSize, 1f, _mapHeight * _cellSize);

        if (_groundMaterial != null)
        {
            _ground.GetComponent<Renderer>().material = _groundMaterial;
        }
        else
        {
            var renderer = _ground.GetComponent<Renderer>();
            renderer.material.color = new Color(0.4f, 0.3f, 0.2f);
        }

        // ����� ��� ������� ��������
        var groundNetObj = _ground.AddComponent<NetworkObject>();
        groundNetObj.Spawn();

        // �������� ��� ��� �������������
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
        rotation = Quaternion.identity;

        Vector3 halfExtents = config.scale / 2f;
        float maxDimension = Mathf.Max(halfExtents.x, halfExtents.z);

        while (attempts < maxAttempts)
        {
            attempts++;

            // ��������� ������� � ������ ���������
            position = new Vector3(
                Random.Range(maxDimension, _mapWidth * _cellSize - maxDimension),
                0,
                Random.Range(maxDimension, _mapHeight * _cellSize - maxDimension)
            );

            // �������� ��� ������
            bool inTeam1Zone = CheckSpawnZone(position, _team1SpawnZone, maxDimension);
            bool inTeam2Zone = CheckSpawnZone(position, _team2SpawnZone, maxDimension);
            if (inTeam1Zone || inTeam2Zone)
            {
                continue;
            }              
            // ��������� �������
            rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // ������ �������� �������� � ������ ��������
            if (!Physics.CheckBox(position, halfExtents, rotation, LayerMask.GetMask("Obstacle")))
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckSpawnZone(Vector3 position, Vector2 spawnZone, float objectSize)
    {
        switch (_spawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            return position.x >= spawnZone.x - objectSize &&
                   position.x <= spawnZone.y + objectSize;

            case SpawnZoneType.VerticalOpposite:
            return position.z >= spawnZone.x - objectSize &&
                   position.z <= spawnZone.y + objectSize;

            case SpawnZoneType.DiagonalCorners:
            return (position.x >= spawnZone.x - objectSize &&
                    position.x <= spawnZone.y + objectSize) ||
                   (position.z >= spawnZone.x - objectSize &&
                    position.z <= spawnZone.y + objectSize);

            default:
            return false;
        }
    }

    private void CreateObstacle(ObstacleConfig config, Vector3 position, Quaternion rotation)
    {
        var obstacle = GameObject.CreatePrimitive(config.primitiveType);
        obstacle.transform.position = position + Vector3.up * (config.scale.y / 2f);
        obstacle.transform.localScale = config.scale;
        obstacle.transform.rotation = rotation;

        // ��������� ���������
        var renderer = obstacle.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard")) { color = config.color };

        // ��������� ����������
        var collider = obstacle.GetComponent<Collider>();
        collider.isTrigger = false;

        // ������� ������
        var netObj = obstacle.AddComponent<NetworkObject>();
        netObj.Spawn();

        obstacle.tag = "Obstacle";
        obstacle.name = $"{config.type}_{NetworkObjectId}";

        // ����������� �������
        RegisterObstaclePosition(position, config.scale);
    }

    private void RegisterObstaclePosition(Vector3 position, Vector3 size)
    {
        // ������������ 8 ����� �� �������� �������
        float halfX = size.x / 2f;
        float halfZ = size.z / 2f;

        _occupiedPositions.Add(position);
        _occupiedPositions.Add(position + new Vector3(halfX, 0, halfZ));
        _occupiedPositions.Add(position + new Vector3(-halfX, 0, halfZ));
        _occupiedPositions.Add(position + new Vector3(halfX, 0, -halfZ));
        _occupiedPositions.Add(position + new Vector3(-halfX, 0, -halfZ));
        _occupiedPositions.Add(position + new Vector3(halfX, 0, 0));
        _occupiedPositions.Add(position + new Vector3(-halfX, 0, 0));
        _occupiedPositions.Add(position + new Vector3(0, 0, halfZ));
        _occupiedPositions.Add(position + new Vector3(0, 0, -halfZ));
    }

    private void ClearMap()
    {
        // ������ ��� �����������
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        if (obstacles != null && obstacles.Length > 0)
        {
            foreach (var obj in obstacles)
            {
                if (obj == null)
                    continue;

                // ��������������� � ��������
                if (obj.TryGetComponent<NetworkObject>(out var netObj))
                {
                    if (netObj.IsSpawned)
                    {
                        netObj.Despawn();
                    }
                }
                Destroy(obj);
            }
        }

        // ���������� �������� ����
        if (_ground != null)
        {
            if (_ground.TryGetComponent<NetworkObject>(out var groundNetObj) && groundNetObj.IsSpawned)
            {
                groundNetObj.Despawn();
            }
            Destroy(_ground);
            _ground = null;
        }

        _occupiedPositions.Clear();
    }

    public Dictionary<Player, Vector3> GetTeamSpawnPoints()
    {
        return new Dictionary<Player, Vector3>
        {
            { Player.Player1, CalculateSpawnZoneCenter(_team1SpawnZone) },
            { Player.Player2, CalculateSpawnZoneCenter(_team2SpawnZone) }
        };
    }

    private Vector3 CalculateSpawnZoneCenter(Vector2 spawnZone)
    {
        float x, z;
        float halfZoneSize = _spawnZoneSize / 2f;

        switch (_spawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            // ��� ��������������� ������������ ������ �� X, ��������� �� Z
            x = spawnZone == _team1SpawnZone * 2
                ? spawnZone.x + halfZoneSize  // ����� �������
                : spawnZone.y - halfZoneSize;  // ������ �������
            z = _mapHeight * _cellSize / 2f;  // ����� �� ������
            break;

            case SpawnZoneType.VerticalOpposite:
            // ��� ������������� ������������ ������ �� Z, ��������� �� X
            x = _mapWidth * _cellSize / 2f;    // ����� �� ������
            z = spawnZone == _team1SpawnZone * 2
                ? spawnZone.x + halfZoneSize  // ������ �������
                : spawnZone.y - halfZoneSize;  // ������� �������
            break;

            case SpawnZoneType.DiagonalCorners:
            // ��� ������������� ������������ ������ �� ����� �����
            if (spawnZone == _team1SpawnZone)
            {
                // ����� ������ ����
                x = spawnZone.x + halfZoneSize * 2;
                z = spawnZone.x + halfZoneSize * 2;
            }
            else
            {
                // ������ ������� ����
                x = spawnZone.y - halfZoneSize * 2;
                z = spawnZone.y - halfZoneSize * 2;
            }
            break;

            default:
            x = z = halfZoneSize; // �������� �� ���������
            break;
        }

        return new Vector3(x, 0, z);
    }

    public Vector3 GetRandomSpawnPosition(Player team)
    {
        Vector2 zone = team == Player.Player1 ? _team1SpawnZone : _team2SpawnZone;
        float padding = _spawnZoneSize / 2f; // ������ �� ������ ����
        float x, z;

        switch (_spawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            // �������������� ���� (�����/������ �������)
            x = team == Player.Player1
                ? Random.Range(zone.x + padding, zone.y - padding) // ����� �������
                : Random.Range(zone.x + padding, zone.y - padding); // ������ �������

            // ����������� ����� �� Z � ��������� ��������� ���������
            float centerZ = _mapHeight * _cellSize / 2f;
            z = Random.Range(centerZ - padding, centerZ + padding);
            break;

            case SpawnZoneType.VerticalOpposite:
            // ������������ ���� (������/������� �������)
            // ����������� ����� �� X � ��������� ��������� ���������
            float centerX = _mapWidth * _cellSize / 2f;
            x = Random.Range(centerX - padding, centerX + padding);

            z = team == Player.Player1
                ? Random.Range(zone.x + padding, zone.y - padding) // ������ �������
                : Random.Range(zone.x + padding, zone.y - padding); // ������� �������
            break;

            case SpawnZoneType.DiagonalCorners:
            // ������������ ���� (����� ������/������ �������)
            if (team == Player.Player1)
            {
                // ����� ������ ����
                x = Random.Range(zone.x + padding, zone.x + _spawnZoneSize - padding);
                z = Random.Range(zone.x + padding, zone.x + _spawnZoneSize - padding);
            }
            else
            {
                // ������ ������� ����
                x = Random.Range(zone.y - _spawnZoneSize + padding, zone.y - padding);
                z = Random.Range(zone.y - _spawnZoneSize + padding, zone.y - padding);
            }
            break;

            default:
            x = z = padding;
            break;
        }

        // ����������, ��� ����� �� ������ �� ������� �����
        x = Mathf.Clamp(x, padding, _mapWidth * _cellSize - padding);
        z = Mathf.Clamp(z, padding, _mapHeight * _cellSize - padding);

        return new Vector3(x, 0, z);
    }
}
