using System;
using System.Collections;
using System.Collections.Generic;
using TicTacToe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TierSelectionScript : MonoBehaviour
{
    [SerializeField] CanvasController _canvasController;
    [SerializeField] Button _startButton, _backButton;
    [SerializeField] TextMeshProUGUI _screenTitle;

    [SerializeField] List<Toggle> _toggles = new List<Toggle>();

    GridTier _tier = GridTier.None;
    // Start is called before the first frame update
    void Start()
    {
        for(int index = 0; index < _toggles.Count; index++)
        {
            int toggleIndex = index;
            _toggles[toggleIndex].onValueChanged.AddListener((bool newValue) =>
            {
                if (_toggles[toggleIndex].isOn)
                {
                    var colorSet = _toggles[toggleIndex].colors;
                    colorSet.normalColor = Color.black;
                    _toggles[toggleIndex].colors = colorSet;

                    _tier = (GridTier)(toggleIndex + 1);
                }
                else
                {
                    var colorSet = _toggles[toggleIndex].colors;
                    colorSet.normalColor = new Color(0.3764705882352941f, 0.3764705882352941f, 0.3764705882352941f);
                    _toggles[toggleIndex].colors = colorSet;
                }
            });
        }

        _toggles[0].isOn = true;
    }

    public void InitializeTierSelection(Action<GridTier> onStartPressed, OnlineScreens precedingScreen, GameMode mode)
    {
        _screenTitle.text = $"{mode}";

        _startButton.onClick.RemoveAllListeners();
        _backButton.onClick.RemoveAllListeners();

        _startButton.onClick.AddListener(() => onStartPressed(_tier));
        _backButton.onClick.AddListener(() => _canvasController.ShowScreen(precedingScreen));
    }

}
