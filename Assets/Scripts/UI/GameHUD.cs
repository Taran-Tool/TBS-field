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
    [SerializeField] private TMPro.TMP_Text _timerText;

    [Header("Action Indicators")]
    [SerializeField] private Image _moveIndicator;
    [SerializeField] private Image _attackIndicator;

    [Header("Victory Panel")]
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private TMPro.TMP_Text _victoryText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;

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

        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (NetworkTurnManager.instance == null)
        {
            return;
        }

        UpdateTimerDisplay();
        UpdateActionIndicators();
        UpdateTurns();
        UpdateText();
        PlayerInputs();

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

    private void UpdateText()
    {
        Player currentPlayer = NetworkTurnManager.instance.CurrentPlayer.Value;
        bool infiniteMovement = NetworkTurnManager.instance.InfiniteMovement.Value;

        Player player = NetworkPlayersManager.instance.GetLocalPlayer();

        string turnText = "";
        if (player == currentPlayer)
        {
            turnText = $"Ваш ход!";
        }
        else
        {
            turnText = $"Ход игрока: {currentPlayer}!";
        }

        if (infiniteMovement)
        {
            turnText += "\nВаши юниты могут перемещаться без ограничений!";
        }

        _turnText.text = turnText;
        _turnCount.text = $"{NetworkTurnManager.instance.CurrentTurn.Value}";
    }

    private void UpdateTimerDisplay()
    {
        float timer = NetworkTurnManager.instance.TurnTimer.Value;
        _timerText.text = Mathf.CeilToInt(timer).ToString();
    }

    private void UpdateTurns()
    {
        int curTurn = NetworkTurnManager.instance.CurrentTurn.Value;
        _turnCount.text = $"{curTurn}";
    }

    private void UpdateActionIndicators()
    {
        if (NetworkTurnManager.instance == null)
            return;

        bool isLocalTurn = NetworkTurnManager.instance.IsLocalPlayersTurn();
        bool canMove = isLocalTurn &&
                      (NetworkTurnManager.instance.ActionsRemaining.Value > 0 &&
                       !NetworkTurnManager.instance.HasMoved.Value) ;

        bool canAttack = isLocalTurn &&
                        NetworkTurnManager.instance.ActionsRemaining.Value > 0 &&
                        !NetworkTurnManager.instance.HasAttacked.Value;

        _moveIndicator.gameObject.SetActive(canMove);
        _attackIndicator.gameObject.SetActive(canAttack);
    }

    private void PlayerInputs()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SkipTurn();
        }
    }

    public void SkipTurn()
    {
        // Проверяю, что это локальный игрок и его ход
        if (NetworkTurnManager.instance != null &&
            NetworkTurnManager.instance.IsLocalPlayersTurn())
        {
            NetworkTurnManager.instance.EndTurnServerRpc();
        }
    }

    public void ShowVictoryPanel(GameResult result, bool isHost)
    {
        if (_victoryPanel == null)
            return;

        string message = result switch
        {
            GameResult.Player1Win => "Player 1 Победил!",
            GameResult.Player2Win => "Player 2 Победил!",
            GameResult.InfiniteMoves => "Ничья!",
            _ => "Конец игры"
        };

        _victoryText.text = message;
        _victoryPanel.SetActive(true);
    }

    public void HideVictoryPanel()
    {
        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }
    }
}
