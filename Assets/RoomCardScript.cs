using System;
using System.Collections;
using System.Collections.Generic;
using TicTacToe;
using TMPro;
using UnityEditor.SceneTemplate;
using UnityEngine;

public class RoomCardScript : MonoBehaviour
{
    public int currentPlayerCount;
    public Room stats;

    // References
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI gameMode;
    public TextMeshProUGUI gridSize;
    public TextMeshProUGUI playerRatioText;

    public void SetupUI()
    {
        roomName.text = stats.roomName;
        gameMode.text = stats.gameMode.ToString();
        gridSize.text = $"{stats.gridSize}x{stats.gridSize}";
        playerRatioText.text = $"{stats.currentPlayerCount}/{stats.totalPlayersAllowed}";
    }

    public void SetupUI(string name)
    {
        roomName.text = name;
        gameMode.text = GameMode.Classic.ToString();
        gridSize.text = "3x3";

        playerRatioText.text = $"1/2";
    }

    //public void SetupUI(string name, GameMode mode, int size, int totalAllowedPlayer, float blitzSecondLimit = 0)
    //{
    //    roomName.text = name;
    //    gameMode.text = GameMode.Classic.ToString();
    //    gridSize.text = $"{size}x{size}";
    //    totalPlayerCount.text = $"{totalAllowedPlayer}";
    //    currentPlayerCountText.text = "1";

    //    blitzSeconds.gameObject.SetActive(mode == GameMode.Blitz && blitzSecondLimit > 0);
    //    blitzSeconds.text = blitzSecondLimit.ToString();    
    //}

    public void OnPlayerCountChanged(int playerCount)
    {
        currentPlayerCount = playerCount;
        playerRatioText.text = $"{stats.currentPlayerCount}/{stats.totalPlayersAllowed}";
    }
}
