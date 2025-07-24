using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance
    {
        get; private set;
    }

    [Header("Player Info")]
    [SerializeField] private Image _playerIndicator;
    [SerializeField] private Text _turnText;

    public Player LocalPlayer
    {
        get; private set;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLocalPlayer(Player player)
    {
        LocalPlayer = player;
        UpdatePlayerVisuals();
    }

    private void UpdatePlayerVisuals()
    {
        Color teamColor = LocalPlayer switch
        {
            Player.Player1 => Color.blue,
            Player.Player2 => Color.red,
            _ => Color.gray
        };

        _playerIndicator.color = teamColor;
    }

    public void UpdateTurnDisplay(Player currentPlayer)
    {
        _turnText.text = $"{currentPlayer}'s Turn";
        _turnText.color = currentPlayer == LocalPlayer ? Color.green : Color.red;
    }
}
