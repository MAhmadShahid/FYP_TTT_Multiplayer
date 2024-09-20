using TicTacToe;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class AdvanceSettingsScript : MonoBehaviour
{
    // References
    [SerializeField] CanvasController _canvasController;
    [SerializeField] TextMeshProUGUI _screenTitle;
    [SerializeField] CounterScript _counter;
    [SerializeField] Button _confirmButton, _backButton;
    [SerializeField] List<Toggle> _gridSizeToggle = new List<Toggle>();

    int _currentGridSize = 3;

    private void Start()
    {
        for (int index = 0; index < _gridSizeToggle.Count; index++)
        {
            int currentCount = index;
            Toggle currentToggle = _gridSizeToggle[currentCount];
            currentToggle.onValueChanged.AddListener((bool newValue) =>
            {               
                if (currentToggle.isOn)
                {
                    Debug.Log($"Value Changed: {newValue}");
                    var colorSet = currentToggle.colors;
                    colorSet.normalColor = Color.black;
                    currentToggle.colors = colorSet;

                    int correspondingGridSize = Configurations.validStages.Keys.ElementAt(currentCount);
                    OnGridSizeChanged(correspondingGridSize);
                }
                else
                {
                    var colorSet = currentToggle.colors;
                    colorSet.normalColor = new Color(0.3764705882352941f, 0.3764705882352941f, 0.3764705882352941f);
                    currentToggle.colors = colorSet;
                }
            });
        }

        _gridSizeToggle[0].isOn = true;
    }

    public void IntializeAdvanceSettings(Action<int, int> OnSettingsConfirmed, GameMode mode, OnlineScreens precedingScreen, int gridSize = 3, int participants = 2)
    {
        if (Configurations.validStages.ContainsKey(gridSize))
            OnGridSizeChanged(gridSize);

        if (Configurations.validStages[gridSize].ContainsKey(participants))
            _counter.SetCounterValue(participants);

        _screenTitle.text = mode.ToString();

        _confirmButton.onClick.RemoveAllListeners();
        _backButton.onClick.RemoveAllListeners();

        
        _backButton.onClick.AddListener(() => _canvasController.ShowScreen(precedingScreen));
        _confirmButton.onClick.AddListener(() =>
        {
            int grid = _currentGridSize;
            int participantCount = _counter.GetCounterValue();

            OnSettingsConfirmed(grid, participantCount);
        });

        _canvasController.ShowScreen(OnlineScreens.AdvanceSettings);
    }

    void OnGridSizeChanged(int gridSize)
    {
        _currentGridSize = gridSize;

        var keyCollection = Configurations.validStages[gridSize].Keys;
        _counter.InitializeCounter(keyCollection.First(), keyCollection.Last());
    }
    
}
