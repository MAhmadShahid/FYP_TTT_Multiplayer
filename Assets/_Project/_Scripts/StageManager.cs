using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TicTacToe;

public class StageManager : MonoBehaviour
{
    [SerializeField] MatchController _matchController;
    [SerializeField] GameObject _templateStagePrefab; 
    GameObject _currentStageObject;

    public MatchInfo matchInfo;

    public void OnStartClient()
    {
        _currentStageObject = Instantiate(_templateStagePrefab);
        var stageScript = _currentStageObject.GetComponentInChildren<StageVisualScript>();
        stageScript.CreateGridLines(matchInfo.gridSize);

        // do grid initialization
    }
}
