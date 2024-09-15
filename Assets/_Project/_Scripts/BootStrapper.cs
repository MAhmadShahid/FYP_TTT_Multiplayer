using JetBrains.Annotations;
using Mirror;
using System.Collections.Generic;
using TicTacToe;
using UnityEngine;

public class BootStrapper : MonoBehaviour
{
    [SerializeField]
    NetworkManager _manager;
    NetworkManagerHUD _managerHUD;
    [SerializeField]
    CanvasController _canvasController;

    [Header("UI Configuration")]
    public bool applyStartingConfiguration = true;
    [SerializeField]
    List<GameObject> _disableUI;
    [SerializeField]
    List<GameObject> _enableUI;


    private void Start()
    {
        if (applyStartingConfiguration)
        {
            _disableUI.ForEach(x => x.SetActive(false));
            _enableUI.ForEach(x => x.SetActive(true));
        }  
    }

    public void OnPlayClicked()
    {
        _manager.StartClient();
    }
}
