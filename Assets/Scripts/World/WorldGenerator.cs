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
        HorizontalOpposite, // Противоположные стороны по X
        VerticalOpposite,   // Противоположные стороны по Z
        DiagonalCorners     // Диагональные углы
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
        // Случайно выбираю тип расположения зон
        _spawnZoneType = (SpawnZoneType) Random.Range(0, 3);

        switch (_spawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            // Левая и правая стороны
            _team1SpawnZone = new Vector2(1, _spawnZoneSize);
            _team2SpawnZone = new Vector2(_mapWidth - _spawnZoneSize - 1, _mapWidth - 1);
            break;

            case SpawnZoneType.VerticalOpposite:
            // Нижняя и верхняя стороны
            _team1SpawnZone = new Vector2(1, _spawnZoneSize);
            _team2SpawnZone = new Vector2(_mapHeight - _spawnZoneSize - 1, _mapHeight - 1);
            break;

            case SpawnZoneType.DiagonalCorners:
            // Случайные диагональные углы
            if (Random.value > 0.5f)
            {
                // Левый верхний и правый нижний
                _team1SpawnZone = new Vector2(1, _spawnZoneSize);
                _team2SpawnZone = new Vector2(_mapWidth - _spawnZoneSize - 1, _mapWidth - 1);
            }
            else
            {
                // Правый верхний и левый нижний
                _team1SpawnZone = new Vector2(_mapWidth - _spawnZoneSize - 1, _mapWidth - 1);
                _team2SpawnZone = new Vector2(1, _spawnZoneSize);
            }
            break;
        }
    }

    private void CreateGround()
    {
        // Создаю пол (плоский куб)
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

        // Делаю пол сетевым объектом
        var groundNetObj = _ground.AddComponent<NetworkObject>();
        groundNetObj.Spawn();

        // Добавляю тег для идентификации
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

            // Случайная позиция с учетом габаритов
            position = new Vector3(
                Random.Range(maxDimension, _mapWidth * _cellSize - maxDimension),
                0,
                Random.Range(maxDimension, _mapHeight * _cellSize - maxDimension)
            );

            // Проверка зон спавна
            bool inTeam1Zone = CheckSpawnZone(position, _team1SpawnZone, maxDimension);
            bool inTeam2Zone = CheckSpawnZone(position, _team2SpawnZone, maxDimension);
            if (inTeam1Zone || inTeam2Zone)
            {
                continue;
            }              
            // Случайный поворот
            rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

            // Полная проверка коллизий с учетом поворота
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

        // Настройка материала
        var renderer = obstacle.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard")) { color = config.color };

        // Настройка коллайдера
        var collider = obstacle.GetComponent<Collider>();
        collider.isTrigger = false;

        // Сетевой объект
        var netObj = obstacle.AddComponent<NetworkObject>();
        netObj.Spawn();

        obstacle.tag = "Obstacle";
        obstacle.name = $"{config.type}_{NetworkObjectId}";

        // Регистрация позиции
        RegisterObstaclePosition(position, config.scale);
    }

    private void RegisterObstaclePosition(Vector3 position, Vector3 size)
    {
        // Регистрируем 8 точек по границам объекта
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
        // Удаляю все препятствия
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        if (obstacles != null && obstacles.Length > 0)
        {
            foreach (var obj in obstacles)
            {
                if (obj == null)
                    continue;

                // Десинхронизация и удаление
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

        // Безопасное удаление пола
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
            // Для горизонтального расположения смещаю по X, центрирую по Z
            x = spawnZone == _team1SpawnZone * 2
                ? spawnZone.x + halfZoneSize  // Левая сторона
                : spawnZone.y - halfZoneSize;  // Правая сторона
            z = _mapHeight * _cellSize / 2f;  // Центр по высоте
            break;

            case SpawnZoneType.VerticalOpposite:
            // Для вертикального расположения смещаю по Z, центрирую по X
            x = _mapWidth * _cellSize / 2f;    // Центр по ширине
            z = spawnZone == _team1SpawnZone * 2
                ? spawnZone.x + halfZoneSize  // Нижняя сторона
                : spawnZone.y - halfZoneSize;  // Верхняя сторона
            break;

            case SpawnZoneType.DiagonalCorners:
            // Для диагонального расположения смещаю от обоих краев
            if (spawnZone == _team1SpawnZone)
            {
                // Левый нижний угол
                x = spawnZone.x + halfZoneSize * 2;
                z = spawnZone.x + halfZoneSize * 2;
            }
            else
            {
                // Правый верхний угол
                x = spawnZone.y - halfZoneSize * 2;
                z = spawnZone.y - halfZoneSize * 2;
            }
            break;

            default:
            x = z = halfZoneSize; // Значение по умолчанию
            break;
        }

        return new Vector3(x, 0, z);
    }

    public Vector3 GetRandomSpawnPosition(Player team)
    {
        Vector2 zone = team == Player.Player1 ? _team1SpawnZone : _team2SpawnZone;
        float padding = _spawnZoneSize / 2f; // Отступ от границ зоны
        float x, z;

        switch (_spawnZoneType)
        {
            case SpawnZoneType.HorizontalOpposite:
            // Горизонтальные зоны (левая/правая стороны)
            x = team == Player.Player1
                ? Random.Range(zone.x + padding, zone.y - padding) // Левая сторона
                : Random.Range(zone.x + padding, zone.y - padding); // Правая сторона

            // Центральная линия по Z с небольшим случайным смещением
            float centerZ = _mapHeight * _cellSize / 2f;
            z = Random.Range(centerZ - padding, centerZ + padding);
            break;

            case SpawnZoneType.VerticalOpposite:
            // Вертикальные зоны (нижняя/верхняя стороны)
            // Центральная линия по X с небольшим случайным смещением
            float centerX = _mapWidth * _cellSize / 2f;
            x = Random.Range(centerX - padding, centerX + padding);

            z = team == Player.Player1
                ? Random.Range(zone.x + padding, zone.y - padding) // Нижняя сторона
                : Random.Range(zone.x + padding, zone.y - padding); // Верхняя сторона
            break;

            case SpawnZoneType.DiagonalCorners:
            // Диагональные углы (левый нижний/правый верхний)
            if (team == Player.Player1)
            {
                // Левый нижний угол
                x = Random.Range(zone.x + padding, zone.x + _spawnZoneSize - padding);
                z = Random.Range(zone.x + padding, zone.x + _spawnZoneSize - padding);
            }
            else
            {
                // Правый верхний угол
                x = Random.Range(zone.y - _spawnZoneSize + padding, zone.y - padding);
                z = Random.Range(zone.y - _spawnZoneSize + padding, zone.y - padding);
            }
            break;

            default:
            x = z = padding;
            break;
        }

        // Гарантирую, что точка не выйдет за пределы карты
        x = Mathf.Clamp(x, padding, _mapWidth * _cellSize - padding);
        z = Mathf.Clamp(z, padding, _mapHeight * _cellSize - padding);

        return new Vector3(x, 0, z);
    }
}
