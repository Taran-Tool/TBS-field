using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;


public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] public NetworkVariable<Player> team = new(writePerm: NetworkVariableWritePermission.Server);
    [SerializeField] public GameObject cameraControllerPrefab;

    public NetworkList<int> UnitIds
    {
        get; private set;
    }

    public Player Team
    {
        get => team.Value;
        set => team.Value = value;
    }

    private static GameObject _localPlayerCamera;

    private void Awake()
    {
        UnitIds = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            GameHUD.instance?.SetLocalPlayer(Team);
            StartCoroutine(WaitAndInitialize());
        }
    }

    private IEnumerator WaitAndInitialize()
    {
        while (NetworkPlayersManager.instance == null)
        {
            yield return null;
        }

        SpawnCameraController(transform);
    }

    public static Color GetTeamColor(Player player)
    {
        return player switch
        {
            Player.Player1 => Color.red,
            Player.Player2 => Color.blue,
            _ => Color.gray
        };
    }

    public void SpawnCameraController(Transform playerObjectTransform)
    {
        if (_localPlayerCamera != null)
        {
            return;
        }
        
        var camera = Instantiate(cameraControllerPrefab);

        camera.name = NetworkPlayersManager.instance.GetLocalPlayer().ToString() + "_Camera";
        camera.GetComponent<CameraController>().Initialize(playerObjectTransform);
    }

    public override void OnNetworkDespawn()
    {
        // Очищаем ссылку при отключении
        if (IsClient && _localPlayerCamera != null)
        {
            Destroy(_localPlayerCamera);
            _localPlayerCamera = null;
        }
        base.OnNetworkDespawn();
    }
}

public enum Player
{
    None = 0,    // Для нейтральных объектов/неопределенного состояния
    Player1 = 1, // Первый игрок (хост)
    Player2 = 2  // Второй игрок (клиент)
}

