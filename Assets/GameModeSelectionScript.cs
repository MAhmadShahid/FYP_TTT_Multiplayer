using System;
using System.Collections;
using System.Collections.Generic;
using TicTacToe;
using UnityEngine;
using UnityEngine.UI;

public class GameModeSelectionScript : MonoBehaviour
{
    [SerializeField] CanvasController _canvasController;

    // References
    [SerializeField] Button _classic, _blitz, _puzzle, _backButton;


    public void InitializeGameModeSelection(Action<GameMode> onGameModeSelected, OnlineScreens precedingScreen)
    {
        _classic.onClick.RemoveAllListeners();
        _blitz.onClick.RemoveAllListeners();
        _backButton.onClick.RemoveAllListeners();

        _classic.onClick.AddListener(() => onGameModeSelected(GameMode.Classic));
        _blitz.onClick.AddListener(() => onGameModeSelected(GameMode.Blitz));

        if(precedingScreen != OnlineScreens.None)
            _backButton.onClick.AddListener(() => _canvasController?.ShowScreen(precedingScreen));  

        _canvasController?.ShowScreen(OnlineScreens.GameMode);
    }
}
