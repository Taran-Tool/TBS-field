using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public static class GameSystemFactory
{
    private static readonly Dictionary<Type, Func<NetworkObject>> _factories = new()
    {
        {
            typeof(NetworkPlayersManager),
            () => CreatePrefab("Prefabs/NetworkPlayersManager", "NetworkPlayersManager")
        },
        {
            typeof(NetworkSyncHandler),
            () => CreatePrefab("Prefabs/NetworkSyncHandler", "NetworkSyncHandler")
        }
        ,
        {
            typeof(NetworkSceneManager),
            () => CreatePrefab("Prefabs/NetworkSceneManager", "NetworkSceneManager")
        }
        ,
        {
            typeof(NetworkPlayerSpawner),
            () => CreatePrefab("Prefabs/NetworkPlayerSpawner", "NetworkPlayerSpawner")
        }
        ,
        {
            typeof(WorldGenerator),
            () => CreatePrefab("Prefabs/NetworkWorldGenerator", "NetworkWorldGenerator")
        }
        ,
        {
            typeof(NetworkUnitsManager),
            () => CreatePrefab("Prefabs/NetworkUnitsManager", "NetworkUnitsManager")
        }
        ,
        {
            typeof(NetworkTurnManager),
            () => CreatePrefab("Prefabs/NetworkTurnManager", "NetworkTurnManager")
        }
        ,
        {
            typeof(NetworkActionsSystem),
            () => CreatePrefab("Prefabs/NetworkActionsSystem", "NetworkActionsSystem")
        }
        ,
        {
            typeof(NetworkVictorySystem),
            () => CreatePrefab("Prefabs/NetworkVictorySystem", "NetworkVictorySystem")
        }
    };

    public static T Create<T>() where T : NetworkBehaviour
    {
        if (_factories.TryGetValue(typeof(T), out var factory))
        {
            return factory().GetComponent<T>();
        }
        throw new ArgumentException($"No factory for type {typeof(T)}");
    }

    private static NetworkObject CreatePrefab(string path, string name)
    {
        var prefab = Resources.Load<GameObject>(path);
        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = name;
        instance.GetComponent<NetworkObject>().Spawn();
        return instance.GetComponent<NetworkObject>();
    }
}
