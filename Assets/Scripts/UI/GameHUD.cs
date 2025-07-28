using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public static GameHUD instance
    {
        get; private set;
    }

    [Header("Player Info")]
    [SerializeField] private Image _playerIndicator;
    [SerializeField] private TMPro.TMP_Text _turnText;
    [SerializeField] private TMPro.TMP_Text _turnCount;

    public Player LocalPlayer
    {
        get; private set;
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

    public void SetLocalPlayer(Player player)
    {
        LocalPlayer = player;
        UpdatePlayerVisuals();
    }

    private void UpdatePlayerVisuals()
    {
        Color teamColor = LocalPlayer switch
        {
            Player.Player1 => Color.red,
            Player.Player2 => Color.blue,
            _ => Color.gray
        };

        _playerIndicator.color = teamColor;
    }

    public void UpdateTurnDisplay(Player currentPlayer)
    {
        _turnText.text = $"{currentPlayer}'s Turn";
        _turnText.color = currentPlayer == LocalPlayer ? Color.red : Color.blue;
    }

    public void ReturnToMenu()
    {
        //отключение

        //загрузка сцены
        NetworkSceneManager.instance.LoadMainMenu();
    }
}
