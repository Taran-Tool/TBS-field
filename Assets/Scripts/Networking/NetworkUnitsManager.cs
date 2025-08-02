using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUnitsManager : NetworkBehaviour
{
    public static NetworkUnitsManager instance
    {
        get; private set;
    }

    [SerializeField] private ArmyConfig _army;
    [SerializeField] private float _unitSpacing = 1.5f;

    private Dictionary<Player, List<NetworkUnit>> _playerUnits = new();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnArmies()
    {
        if (!IsServer)
            return;

        ArmyConfig armyToUse = GetArmyToUse();

        var spawnPoints = WorldGenerator.instance.GetTeamSpawnPoints();

        SpawnArmyForPlayer(Player.Player1, spawnPoints[Player.Player1], armyToUse);
        SpawnArmyForPlayer(Player.Player2, spawnPoints[Player.Player2], armyToUse);
    }

    private ArmyConfig GetArmyToUse()
    {
        // Если армия задана в инспекторе - использую её
        if (_army != null)
            return _army;

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
                unitsInRing = 1; // Центральный юнит

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
            return false;

        // Проверка коллизий с препятствиями
        Collider[] colliders = Physics.OverlapSphere(position, _unitSpacing / 2f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Obstacle") || collider.CompareTag("Unit"))
                return false;
        }

        return true;
    }

    private void SpawnUnit(UnitConfig config, Vector3 position, Player player)
    {
        // Получаю высоту земли в точке спавна
        float groundHeight = GetGroundHeight(position);

        GameObject unitObj = Instantiate(config.prefab, new Vector3(position.x, groundHeight, position.z), Quaternion.identity);

        unitObj.tag = "Unit";

        NetworkUnit unit = unitObj.GetComponent<NetworkUnit>();
        unit.Initialize(config, player);

        NetworkObject netObj = unitObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        _playerUnits[player].Add(unit);
    }
    private float GetGroundHeight(Vector3 position)
    {
        // Делаю рейкаст вниз от высокой точки для поиска земли - на случай если поверхность будет не идеально ровной
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

    public List<NetworkUnit> GetPlayerUnits(Player player) => _playerUnits[player];
}
