using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private GameObject _joinPanel;
    [SerializeField] private TMPro.TMP_InputField _ipInputField;

    public void ShowJoinGameMenu()
    {
        _hostPanel.SetActive(false);
        _joinPanel.SetActive(true);
        _ipInputField.text = "localhost";
    }

    public void ReturnToMainMenu()
    {
        _joinPanel.SetActive(false);
        _hostPanel.SetActive(true);
    }

    public void OnHostButtonClick()
    {
        GameNetworkManager.instance.StartHostGame();
    }

    public void OnJoinButtonClick()
    {
        GameNetworkManager.instance.JoinGame(_ipInputField.text);
        NetworkSceneManager.instance.LoadGameScene();
    }

    public void OnQuitButtonClick()
    {
        Application.Quit();
    }
}
