using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class NetworkUnitsManager : NetworkBehaviour
{
    public static NetworkUnitsManager instance
    {
        get; private set;
    }

    [SerializeField] private ArmyConfig _army;
    [SerializeField] private float _unitSpacing = 1.5f;

    private Dictionary<int, NetworkUnit> _unitsById = new();
    private Dictionary<Player, List<NetworkUnit>> _playerUnits = new();

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

    public void SpawnArmies()
    {
        if (!IsServer)
        {
            return;
        }            

        ArmyConfig armyToUse = GetArmyToUse();

        var spawnPoints = WorldGenerator.instance.GetTeamSpawnPoints();

        SpawnArmyForPlayer(Player.Player1, spawnPoints[Player.Player1], armyToUse);
        SpawnArmyForPlayer(Player.Player2, spawnPoints[Player.Player2], armyToUse);
    }

    private ArmyConfig GetArmyToUse()
    {
        // Если армия задана в инспекторе - использую её
        if (_army != null)
        {
            return _army;
        }            

        // Иначе загружаю все армии из Resources
        ArmyConfig[] allArmies = Resources.LoadAll<ArmyConfig>("Configs/Armies");

        if (allArmies.Length == 0)
        {
            Debug.LogError("No armies found in Resources/Configs/Armies!");
            // в дальнейшем создать default армию и проверку ее наличия при старте игры, и возвращать ее здесь
            return null;
        }

        // Выбираю случайную армию
        return allArmies[Random.Range(0, allArmies.Length)];
    }
    private void SpawnArmyForPlayer(Player player, Vector3 spawnCenter, ArmyConfig army)
    {
        _playerUnits[player] = new List<NetworkUnit>();
        // Создаю список всех юнитов для спавна
        List<UnitConfig> unitsToSpawn = new List<UnitConfig>();
        foreach (var unitSlot in army.units)
        {
            for (int i = 0; i < unitSlot.count; i++)
            {
                unitsToSpawn.Add(unitSlot.unitConfig);
            }
        }

        // Расставляю юниты по концентрическим кругам для красоты
        int unitsSpawned = 0;
        int ringNumber = 0;

        while (unitsSpawned < unitsToSpawn.Count)
        {
            float currentRadius = ringNumber * _unitSpacing;
            int unitsInRing = Mathf.FloorToInt(2 * Mathf.PI * currentRadius / _unitSpacing);

            if (unitsInRing <= 0)
            {
                unitsInRing = 1; // Центральный юнит
            }                

            for (int i = 0; i < unitsInRing && unitsSpawned < unitsToSpawn.Count; i++)
            {
                float angle = i * (2 * Mathf.PI / unitsInRing); 
                // добавляю случайное смещение для хаоса
                Vector3 spawnPos = spawnCenter + new Vector3(
                    Mathf.Cos(angle) * (currentRadius + Random.Range(-0.3f, 0.3f)),
                    0,
                    Mathf.Sin(angle) * (currentRadius + Random.Range(-0.3f, 0.3f))
                );

                // Проверка позиции
                if (IsValidSpawnPosition(spawnPos))
                {
                    SpawnUnit(unitsToSpawn[unitsSpawned], spawnPos, player);
                    unitsSpawned++;
                }
            }

            ringNumber++;

            // Защита от бесконечного цикла
            if (ringNumber > 20)
                break;
        }
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Проверка на выход за границы карты
        if (!WorldGenerator.instance.IsPositionInsideMap(position))
        {
            return false;
        }            

        // Проверка коллизий с препятствиями
        Collider[] colliders = Physics.OverlapSphere(position, _unitSpacing / 2f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Obstacle") || collider.CompareTag("Unit"))
            {
                return false;
            }                
        }
        return true;
    }

    private void SpawnUnit(UnitConfig config, Vector3 position, Player team)
    {
        // Получаю высоту земли в точке спавна
        float groundHeight = GetGroundHeight(position);

        GameObject unitObj = Instantiate(config.prefab, new Vector3(position.x, groundHeight, position.z), Quaternion.identity);

        unitObj.tag = "Unit";
        unitObj.layer = LayerMask.NameToLayer("Unit");
        foreach (Transform child in unitObj.transform)
        {
            if (child.name!="Indicator")
            {
                child.gameObject.layer = LayerMask.NameToLayer("Unit");
            }            
        }

        NetworkUnit unit = unitObj.GetComponent<NetworkUnit>();
        unit.Initialize(config, team);

        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
        var player = NetworkPlayersManager.instance.GetPlayerByTeam(team);
        netObj.SpawnWithOwnership(player.OwnerClientId);
        SyncUnitLayersClientRpc(netObj.NetworkObjectId);

        if (!_playerUnits.ContainsKey(team))
        {
            _playerUnits[team] = new List<NetworkUnit>();
        }

        player.UnitIds.Add((int) netObj.NetworkObjectId);
        _playerUnits[team].Add(unit);
        _unitsById[unit.Id] = unit;

        NetworkSyncHandler.instance.RegisterObjectServerRpc(netObj.NetworkObjectId, "Unit");
    }

    [ClientRpc]
    private void SyncUnitLayersClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            GameObject unitObj = netObj.gameObject;

            // Устанавливаем тег и слой на клиентах
            unitObj.tag = "Unit";
            unitObj.layer = LayerMask.NameToLayer("Unit");

            foreach (Transform child in unitObj.transform)
            {
                if (child.name != "Indicator")
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Unit");
                }
            }
        }
    }

    private float GetGroundHeight(Vector3 position)
    {
        // Делаю рейкаст вниз c высокой точки для поиска земли - на случай если поверхность будет не идеально ровной
        float raycastHeight = 10f; // Начинаю проверку с этой высоты
        float groundHeight = 0f;

        RaycastHit hit;
        if (Physics.Raycast(
            new Vector3(position.x, raycastHeight, position.z),
            Vector3.down,
            out hit,
            Mathf.Infinity,
            LayerMask.GetMask("Ground")))
        {
            groundHeight = hit.point.y;
        }
        return groundHeight + 1f;
    }

    public void RemoveUnit(int unitId)
    {
        if (_unitsById.TryGetValue(unitId, out var unit))
        {
            var player = NetworkPlayersManager.instance.GetPlayerByTeam(unit.Owner);

            if (player != null)
            {
                player.UnitIds.Remove(unitId);
            }

            _unitsById.Remove(unitId);
        }
    }

    public void RemoveAllUnits()
    {
        foreach (var unit in _unitsById.Values)
        {
            if (unit != null && unit.gameObject != null)
            {
                if (unit.TryGetComponent<NetworkObject>(out var netObj))
                {
                    netObj.Despawn();
                }
                Destroy(unit.gameObject);
            }
        }

        _playerUnits.Clear();
        _unitsById.Clear();
    }

    public NetworkUnit GetUnitById(int unitId)
    {
        return _unitsById.GetValueOrDefault(unitId);
    }

    public List<NetworkUnit> GetPlayerUnits(Player player)
    {
        if (_playerUnits.TryGetValue(player, out var units))
        {
            return units.Where(u => u != null && u.gameObject != null).ToList();
        }
        return new List<NetworkUnit>();
    }

    public IEnumerable<NetworkUnit> GetAllUnits()
    {
        return _unitsById.Values;
    }
}
