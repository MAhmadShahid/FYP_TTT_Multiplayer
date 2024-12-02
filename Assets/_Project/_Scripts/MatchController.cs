using UnityEngine;
using Mirror;
using TicTacToe;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;
using JetBrains.Annotations;
using Org.BouncyCastle.Asn1.Mozilla;

public class MatchController : NetworkBehaviour
{
    // sync data structures
    internal readonly SyncList<NetworkIdentity> playerTurnQueue = new SyncList<NetworkIdentity>();
    internal readonly SyncDictionary<NetworkIdentity, PlayerStruct> matchPlayers = new SyncDictionary<NetworkIdentity, PlayerStruct>();
    
    // a local queue implementation
    public List<NetworkIdentity> localQueue = new List<NetworkIdentity>();

    // mapping for ease
    public Dictionary<NetworkIdentity, NetworkConnectionToClient> connectionIdentityMapping = new Dictionary<NetworkIdentity, NetworkConnectionToClient>();

    public NetworkIdentity roomOwner;

    // local variables
    bool rematch = true;
    public Dictionary<int, NetworkIdentity> cellOwners = new Dictionary<int, NetworkIdentity>();

    // events that define different behaviour depending upon where called from
    // lobby or room manager defines these events
    public event Action<Guid> OnMatchEnd;
    public event Action<Guid, NetworkConnectionToClient> OnPlayerLeave;    

    #region Synced Variables
    [SyncVar]
    internal MatchInfo matchInfo;

    [SyncVar(hook = nameof(OnCurrentPlayerChanged))]
    [ReadOnly, SerializeField] internal NetworkIdentity currentPlayer;
    #endregion

    #region Client Side Modules
    [ReadOnly, SerializeField] MatchUIManager _uiManager;
    [ReadOnly, SerializeField] StageManager _stageManager;
    #endregion

    #region GUI References
    [Header("GUIReferences")]
    CanvasController _canvasController;
    [SerializeField] Button _rematchButton, _returnButton;
    #endregion



    private void Start()
    {
        UtilityClass.LogMessages("Start function code");
    }
     

    [ServerCallback]
    public override void OnStartServer()
    {
        UtilityClass.LogMessages("MatchController: OnStartServer");
        base.OnStartServer();
    }

    #region Client Side Setup

    public override void OnStartClient()
    {
        AssignClientHandlers();
        RequestQueueList();

        _uiManager.matchInfo = matchInfo;
        _stageManager.matchInfo = matchInfo;

        UtilityClass.LogMessages("MatchController: OnStartClient");
        UtilityClass.LogMessages($"MatchInfo: {matchInfo.name}\n Mode: {matchInfo.mode}\n");
        UtilityClass.LogMessages($"GridSize: {matchInfo.gridSize}\nPlayerCount: {matchInfo.playerCount}");


        _canvasController = FindObjectOfType<CanvasController>();
        if (_canvasController != null)
            Debug.LogWarning("MatchController couldn't find canvas controller");

        SetupClientSide();
    }

    public void AssignClientHandlers()
    {
        matchPlayers.OnRemove += OnClientPlayerLeave;
    }

    public void RemoveClientHandlers()
    {
        matchPlayers.OnRemove -= OnClientPlayerLeave;
    }
    
    [ClientCallback]
    public void SetupClientSide()
    {
        _rematchButton.gameObject.SetActive(false);
        _returnButton.gameObject.SetActive(false);


        _canvasController.gameObject.SetActive(false);
        StartCoroutine(_uiManager.OnStartClient());
        _stageManager.OnStartClient();
    }


    [Command(requiresAuthority = false)]
    public void RequestQueueList(NetworkConnectionToClient requestingClient = null)
    {
        if (requestingClient != null)
            OnClientRecieveQueueList(requestingClient, localQueue.ToArray());

    }

    [TargetRpc]
    public void OnClientRecieveQueueList(NetworkConnectionToClient targetClient, NetworkIdentity[] queue)
    {
        foreach(var item in queue)
            localQueue.Add(item);

        _uiManager.SetupMatchUI();
    }

    #endregion


    #region Disconnect Logic

    [ServerCallback]
    public IEnumerator OnServerPlayerLeave(NetworkConnectionToClient playerConnection)
    {
        NetworkIdentity playerIdentity = null;

        foreach (var identity in matchPlayers.Keys)
            if (identity.connectionToClient == playerConnection)
                playerIdentity = identity;

        if (playerIdentity != null)
        {
            Debug.Log("Player found");
            int playerQueueIndex = GetIdentitiesIndexFromList(playerIdentity);
            matchPlayers.Remove(playerIdentity); // remove player -> replicate -> all clients handle what to do
            localQueue.Remove(playerIdentity);

            // update current player
            if(currentPlayer == playerIdentity)
                currentPlayer = localQueue[playerQueueIndex % localQueue.Count];

            if (localQueue.Count == 1)
                WrapMatchUp(GameStatus.Forfeit, playerIdentity);

            // wait for client to recieve handling messages
            yield return null;
            NetworkServer.RemovePlayerForConnection(playerConnection, RemovePlayerOptions.Destroy);
        }
        else
        {
            
            UtilityClass.LogMessages("Player to kick NOT FOUND");
        }
            

    }

    [ClientCallback]
    public void OnClientPlayerLeave(NetworkIdentity playerIdentity, PlayerStruct playerStruct)
    {
        // if player leaving is local player
        if(playerIdentity.isLocalPlayer)
        {
            RemoveClientHandlers();
            OnClientEndMatch();
        }
        else
        {
            // update ui
            int playerRemovingIndex = GetIdentitiesIndexFromList(playerIdentity);
            localQueue.Remove(playerIdentity);
            _uiManager.OnPlayerLeft(playerRemovingIndex % localQueue.Count);
        }
    }

    [ClientCallback]
    public void OnClientEndMatch()
    {
        _stageManager.CleanUpStageVisuals();
        _canvasController.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    [ServerCallback]
    public void OnServerPlayerDisconnects(NetworkConnectionToClient playerDisconnected)
    {
        StartCoroutine(OnServerPlayerLeave(playerDisconnected));
    }


    [Command(requiresAuthority = false)]
    public void CommandRequestToLeave(NetworkConnectionToClient player = null)
    {
        // lobby or room manager defines these events
        Debug.Log("Player requested to leave");
        OnPlayerLeave?.Invoke(matchInfo.matchID, player);
    }

    [TargetRpc]
    public void RPC_PlayerLeft(NetworkConnectionToClient targetClient, int currentPlayerIndex)
    {
        foreach (var player in matchPlayers.Values)
            Debug.Log(player.name);

        _uiManager.OnPlayerLeft(currentPlayerIndex);
    }
    #endregion




    #region Commands & RPCS

    [Command(requiresAuthority = false)]
    public void CommandMakePlay(int cellValue, NetworkConnectionToClient playedBy = null)
    {
        if (currentPlayer != playedBy.identity || cellOwners.ContainsKey(cellValue))
            return;

        cellOwners[cellValue] = currentPlayer;
        RPC_UpdateClientBoardState(cellValue, playedBy.identity);

        if (CheckWin(cellValue, playedBy.identity))
            WrapMatchUp(GameStatus.Won, currentPlayer);
        // if the board is full
        else if(cellOwners.Keys.Count == matchInfo.gridSize * matchInfo.gridSize)
            WrapMatchUp(GameStatus.Draw);
        else
        {
            int playerIndexInList = GetIdentitiesIndexFromList(currentPlayer);
            currentPlayer = localQueue[(playerIndexInList + 1) % localQueue.Count];
        }

    }

    [ServerCallback]
    public void WrapMatchUp(GameStatus status, NetworkIdentity player = null)
    {
        switch(status)
        {
            case GameStatus.Won:
                RPC_ShowWinner(player);
                // StartCoroutine(ServerEndMatch(0));
                break;
            case GameStatus.Draw:
                RPC_ShowDraw();
                break;
            case GameStatus.Forfeit:
                RPC_PlayerForfeited();
                StartCoroutine(ServerEndMatch(0));
                break;
        }
        
        if(status != GameStatus.Forfeit)
        // implement counter
        {

        }
    }

    [ServerCallback]
    public IEnumerator ServerEndMatch(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        RPC_EndMatch();                         // client match controller will clean up.
        
        yield return new WaitForSeconds(0.1f); // and wait for it.

        foreach(var playerIdentity in matchPlayers.Keys)
        {
            NetworkConnectionToClient conn = playerIdentity.connectionToClient;
            
            if(conn != null)
                NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Destroy);
        }

        yield return null;

        OnMatchEnd?.Invoke(matchInfo.matchID);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    public void RPC_EndMatch()
    {
        UtilityClass.LogMessages("Ending match on client side");

        OnClientEndMatch();
    }

    [TargetRpc]
    public void TargetRPC_EndMatch(NetworkConnectionToClient player)
    {
        _canvasController.InitializeCanvasOnline();
        _canvasController.gameObject.SetActive(true);

        _stageManager.CleanUpStageVisuals();
        // gameObject.SetActive(false);
        Destroy(gameObject);
    }

    [ClientRpc]
    public void RPC_UpdateClientBoardState(int cellValue, NetworkIdentity playedBy)
    {
        cellOwners[cellValue] = playedBy;
        _stageManager.PutMarker(cellValue, GetIdentitiesIndexFromList(playedBy));
    }

    [ClientRpc]
    public void RPC_ShowWinner(NetworkIdentity winner)
    {
        var player = matchPlayers[winner];
        _uiManager.ShowWinnerScreen(player);
        Debug.Log($"Winner: {player.name}");

        //_rematchButton.interactable = true;
        //_rematchButton.gameObject.SetActive(true);

        _returnButton.interactable = true;
        _returnButton.gameObject.SetActive(true);
    }

    [ClientRpc]
    public void RPC_ShowDraw()
    {
        _uiManager.ShowDrawScreen();

        _rematchButton.interactable = true;
        _rematchButton.gameObject.SetActive(true);

        _returnButton.interactable = true;
        _returnButton.gameObject.SetActive(true);

    }

    [ClientRpc]
    public void RPC_PlayerForfeited() // missing
    {
        UtilityClass.LogMessages($"Player forfeited: isLocalPlayer");
    }

    #endregion

    #region Server Callbacks

    [ServerCallback]
    public void ShuffleList()
    {
        System.Random rand = new System.Random();
        int itemCount = localQueue.Count;

        while (itemCount > 0)
        {
            itemCount--;
            int randomNumber = rand.Next(itemCount + 1);

            var temp = localQueue[randomNumber];
            localQueue[randomNumber] = localQueue[itemCount];
            localQueue[itemCount] = temp;
        }
    }

    [ServerCallback]
    public bool CheckWin(int cellValue, NetworkIdentity currentPlayer)
    {
        Tuple<Tuple<int, int>, Tuple<int, int>> horizontalRule = new Tuple<Tuple<int, int>, Tuple<int, int>>(
        new Tuple<int, int>(0, -1),        // Left = { Row = 0, Column = -1 }
        new Tuple<int, int>(0, 1)          // Right = { Row = 0, Column = 1 }
        );


        // Tuple<int, int> horizontalRule = new Tuple<int, int>( -1, 1 );
        Tuple<Tuple<int, int>, Tuple<int, int>> verticalRule = new Tuple<Tuple<int, int>, Tuple<int, int>>(
            new Tuple<int, int>(-1, 0),        // Left = { Row = 0, Column = -1 }
            new Tuple<int, int>(1, 0)          // Right = { Row = 0, Column = 1 }
        );

        Tuple<Tuple<int, int>, Tuple<int, int>> diagonalRule1 = new Tuple<Tuple<int, int>, Tuple<int, int>>(
            new Tuple<int, int>(-1, -1),        // Left = { Row = 0, Column = -1 }
            new Tuple<int, int>(1, 1)          // Right = { Row = 0, Column = 1 }
        );

        Tuple<Tuple<int, int>, Tuple<int, int>> diagonalRule2 = new Tuple<Tuple<int, int>, Tuple<int, int>>(
            new Tuple<int, int>(-1, 1),        // Left = { Row = 0, Column = -1 }
            new Tuple<int, int>(1, -1)          // Right = { Row = 0, Column = 1 }
        );

        return CheckWinCondition(currentPlayer, cellValue, horizontalRule, "Horizontal") ||
                    CheckWinCondition(currentPlayer, cellValue, verticalRule, "Vertical") ||
                    CheckWinCondition(currentPlayer, cellValue, diagonalRule1, "Diagonal1") ||
                    CheckWinCondition(currentPlayer, cellValue, diagonalRule2, "Diagonal2");
    }

    [ServerCallback]
    bool CheckWinCondition(NetworkIdentity player, int currentCellMarked, Tuple<Tuple<int, int>, Tuple<int, int>> ruling, string rulingType)
    {

        int currentRow = currentCellMarked / matchInfo.gridSize;
        int startingColumn = currentCellMarked % matchInfo.gridSize;
        int score = 1;

        Debug.Log($"Current Row: {currentRow}; Column: {startingColumn}");

        // check horizontal rule
        bool left = true, right = true;
        Tuple<int, int> leftRule = ruling.Item1;
        Tuple<int, int> rightRule = ruling.Item2;

        int leftRowCounter = leftRule.Item1, leftColumnCounter = leftRule.Item2;
        int rightRowCounter = rightRule.Item1, rightColumnCounter = rightRule.Item2;
        score = 1; // reset score            

        while (left || right)
        {
            // check left column
            if (left)
            {
                int leftCellColumn = startingColumn + leftColumnCounter;
                int leftCellRow = currentRow + leftRowCounter;
                if (leftCellColumn >= 0 && leftCellRow >= 0)
                {
                    int leftCellValue = GetCellValue(leftCellRow, leftCellColumn);
                    if (cellOwners.ContainsKey(leftCellValue) && cellOwners[leftCellValue] == player)
                    {
                        score++;
                        leftRowCounter += leftRule.Item1;
                        leftColumnCounter += leftRule.Item2;
                        
                    }
                    else
                        left = false;
                }
                else
                    left = false;
            }

            // check right column
            if (right)
            {
                int rightCellColumn = startingColumn + rightColumnCounter;
                int rightCellRow = currentRow + rightRowCounter;
                if (rightCellColumn < matchInfo.gridSize && rightCellRow < matchInfo.gridSize)
                {
                    int rightCellValue = GetCellValue(rightCellRow, rightCellColumn);
                    if (cellOwners.ContainsKey(rightCellValue) && cellOwners[rightCellValue] == player)
                    {
                        score++;
                        rightRowCounter += rightRule.Item1;
                        rightColumnCounter += rightRule.Item2;
                    }
                    else
                        right = false;
                }
                else
                    right = false;
            }
        }

        if (score == Configurations.validStages[matchInfo.gridSize][matchInfo.playerCount])
        {
            Debug.Log("Wins with ruline" + rulingType);
            return true;
        }
            

        return false;
    }



    #endregion

    #region Client Callbacks

    [ClientCallback]
    public void OnCurrentPlayerChanged(NetworkIdentity _, NetworkIdentity newPlayerIdentity)
    {
        int playerIndex = GetIdentitiesIndexFromList(newPlayerIdentity);
        _uiManager.UpdatePlayerTurnUI(playerIndex);
    }

    #endregion

    
    // Helping functions
    public int GetIdentitiesIndexFromList(NetworkIdentity playerIdentity)
    {
        for(int playerIndex = 0; playerIndex < localQueue.Count; playerIndex++)
            if (localQueue[playerIndex] == playerIdentity)
                return playerIndex;
        
        return -1;
    }

    public int GetCellValue(int row, int column)
    {
        return (row * matchInfo.gridSize) + column;
    }

}