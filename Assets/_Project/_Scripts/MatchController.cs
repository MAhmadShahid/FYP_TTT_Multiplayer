using UnityEngine;
using Mirror;
using TicTacToe;
using System.Collections.Generic;

public class MatchController : NetworkBehaviour
{

    internal SyncDictionary<NetworkIdentity, PlayerStruct> matchPlayers = new SyncDictionary<NetworkIdentity, PlayerStruct>();

    [Header("GUIReferences")]
    CanvasController _canvasController;
    [ReadOnly, SerializeField] MatchUIManager _uiManager;
    [ReadOnly, SerializeField] StageManager _stageManager;

    [SyncVar]
    public MatchInfo matchInfo;

    private void Start()
    {
        UtilityClass.LogMessages("Start function code");
    }

    #region Mirror Callbacks
    public override void OnStartServer()
    {
        UtilityClass.LogMessages("MatchController: OnStartServer");
        base.OnStartServer();
    }


    public override void OnStartClient()
    {
        UtilityClass.LogMessages("MatchController: OnStartClient");

        _canvasController = FindObjectOfType<CanvasController>();
        if (_canvasController != null)
            UtilityClass.LogMessages("Object found");

        //_canvasController.gameObject.SetActive(false);
        _uiManager.OnStartClient();
        _stageManager.OnStartClient();
    }

    public void AddPlayersToMatchController()
    {

    }

    #endregion
}