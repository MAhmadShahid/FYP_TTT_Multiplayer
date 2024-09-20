using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CounterScript : MonoBehaviour
{
    int _counter, _minValue, _maxValue;

    // References
    [SerializeField] TextMeshProUGUI _counterNumber;
    [SerializeField] Button _addButton, _minusButton;

    private void Start()
    {
        _addButton.onClick.AddListener(() => OnCounterValueChanged(_counter + 1));
        _minusButton.onClick.AddListener(() => OnCounterValueChanged(_counter - 1));

        InitializeCounter(2, 2);
    }

    public void InitializeCounter(int minValue, int maxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;

        OnCounterValueChanged(minValue);
    }

    public int GetCounterValue() => _counter;
    public void SetCounterValue(int value) => OnCounterValueChanged(value);

    void OnCounterValueChanged(int newValue)
    {
        if (newValue < _minValue || newValue > _maxValue)
            return;

        _counter = newValue;

        UpdateUI();
    }

    void UpdateUI()
    {
        _counterNumber.text = _counter.ToString();

        _addButton.interactable = _counter < _maxValue;
        _minusButton.interactable = _counter > _minValue;
    }

    
}
