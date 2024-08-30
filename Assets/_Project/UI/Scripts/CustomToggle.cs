using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CustomToggle : MonoBehaviour
{
    Toggle _toggle;
    Image _image;

    [SerializeField]
    bool _isOn = false;
    [SerializeField]
    Color _normalColor;
    [SerializeField]
    Color _pressedColor;

    // event setup
    public delegate void ToggleHandler(bool value);
    public event ToggleHandler OnToggled;

    void Awake()
    {
        _toggle = transform.AddComponent<Toggle>();
        _toggle.onValueChanged.AddListener(SetActiveState);

        _image = transform.GetComponent<Image>();
        _image.color = _normalColor;
    }

    public bool GetToggleValue() => _toggle.isOn;

    public void SetToggleValue(bool newValue)
    {
        Debug.Log($"Active: {newValue}");
        _isOn = newValue;
        _image.color = _isOn ? _pressedColor : _normalColor;

        OnToggled?.Invoke(newValue);
    }


    #region Custom Toggle Functionality
    void SetActiveState(bool isActive)
    {
        Debug.Log($"Toggle: {isActive}");
        if (isActive && !_isOn)
            SetToggleValue(true);
        else if(isActive &&  _isOn)
            SetToggleValue(false);
    }

    #endregion

}
