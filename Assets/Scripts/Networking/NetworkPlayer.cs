using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;


public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] public NetworkVariable<Player> team = new NetworkVariable<Player>();
    public Player Team
    {
        get => team.Value;
        set => team.Value = value;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                gameObject.tag = "HostPlayer";
                Team = Player.Player1;
            }
            else
            {
                Team = Player.Player2;
            }
            //отображаю GUI игрока согласно его команде
            GameHUD.instance.SetLocalPlayer(Team);
        }
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
}


public enum Player
{
    None = 0,    // Для нейтральных объектов/неопределенного состояния
    Player1 = 1, // Первый игрок (хост)
    Player2 = 2  // Второй игрок (клиент)
}