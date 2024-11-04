using TicTacToe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MatchUIManager : MonoBehaviour
{
    [SerializeField] Canvas _matchCanvas;

    [Header("GUIReferences")]
    [SerializeField] GameObject _startScreenObject;
    [SerializeField] GameObject _topPanel;
    [SerializeField] GameObject _matchmakedScreen;
    [SerializeField] GameObject _matchUIScreen;

    public MatchInfo matchInfo;

    public void OnStartClient()
    {
        gameObject.SetActive(true);

    }

    [ClientCallback]
    public void ShowStartScreen(MatchInfo matchInfo)
    {

    }
}
