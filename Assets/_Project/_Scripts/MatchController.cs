using UnityEngine;
using Mirror;
using TicTacToe;
using System.Collections.Generic;

public class MatchController : NetworkBehaviour
{

    internal readonly SyncDictionary<NetworkIdentity, PlayerStruct> matchPlayers = new SyncDictionary<NetworkIdentity, PlayerStruct>();

    public readonly SyncList<NetworkIdentity> playerTurnQueue = new SyncList<NetworkIdentity>();

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

    [ServerCallback]
    public override void OnStartServer()
    {
        UtilityClass.LogMessages("MatchController: OnStartServer");
        base.OnStartServer();
    }

    [ServerCallback]
    public void DecidePlayerTurns()
    {

    }


    public override void OnStartClient()
    {
        _uiManager.matchInfo = matchInfo;
        UtilityClass.LogMessages("MatchController: OnStartClient");

        UtilityClass.LogMessages($"MatchInfo: {matchInfo.name}\n Mode: {matchInfo.mode}\n");
        UtilityClass.LogMessages($"GridSize: {matchInfo.gridSize}\nPlayerCount: {matchInfo.playerCount}");

        _canvasController = FindObjectOfType<CanvasController>();
        if (_canvasController != null)
            UtilityClass.LogMessages("Object found");

        _canvasController.gameObject.SetActive(false);
        StartCoroutine(_uiManager.OnStartClient());
        UtilityClass.LogMessages("starting stage manager");
        _stageManager.OnStartClient();
    }

    public void AddPlayersToMatchController()
    {

    }


    public void ShuffleList()
    {
        System.Random rand = new System.Random();
        int itemCount = playerTurnQueue.Count;

        while (itemCount > 0)
        {
            itemCount--;
            int randomNumber = rand.Next(itemCount + 1);
            
            var temp = playerTurnQueue[randomNumber];
            playerTurnQueue[randomNumber] = playerTurnQueue[itemCount];
            playerTurnQueue[itemCount] = temp;
        }
    }

    #endregion
}