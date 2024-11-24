using UnityEngine;
using Mirror;
using TicTacToe;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;

public class MatchController : NetworkBehaviour
{
    public Dictionary<NetworkIdentity, NetworkConnectionToClient> connectionIdentityMapping = new Dictionary<NetworkIdentity, NetworkConnectionToClient>();
    internal readonly SyncDictionary<NetworkIdentity, PlayerStruct> matchPlayers = new SyncDictionary<NetworkIdentity, PlayerStruct>();
    public Dictionary<int, NetworkIdentity> cellOwners = new Dictionary<int, NetworkIdentity>();

    internal readonly SyncList<NetworkIdentity> playerTurnQueue = new SyncList<NetworkIdentity>();

    [Header("GUIReferences")]
    CanvasController _canvasController;
    [ReadOnly, SerializeField] MatchUIManager _uiManager;
    [ReadOnly, SerializeField] StageManager _stageManager;
    [SerializeField] Button _rematchButton, _returnButton;

    [SyncVar]
    internal MatchInfo matchInfo;

    [SyncVar(hook = nameof(OnCurrentPlayerChanged))]
    [ReadOnly, SerializeField] internal NetworkIdentity currentPlayer;

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


    public override void OnStartClient()
    {
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

    [ClientCallback]
    public void SetupClientSide()
    {
        _rematchButton.gameObject.SetActive(false);
        _returnButton.gameObject.SetActive(false);

        _canvasController.gameObject.SetActive(false);
        StartCoroutine(_uiManager.OnStartClient());
        _stageManager.OnStartClient();
    }

    [ServerCallback]
    public void OnPlayerDisconnects(NetworkConnectionToClient playerConnection)
    {
        NetworkIdentity playerIdentity = null;
        
        foreach( var identity in matchPlayers.Keys )
            if (connectionIdentityMapping[identity] == playerConnection)
                playerIdentity = identity;


        if( playerIdentity != null )
        {
            UtilityClass.LogMessages("Player to kick found");

            var playerIndex = GetIdentitiesIndexFromList(currentPlayer);
            matchPlayers.Remove(playerIdentity);
            playerTurnQueue.Remove(playerIdentity);

            if (playerTurnQueue.Count == 1)
            {
                UtilityClass.LogMessages("Match doesn't have enough players, wrapping up");
                WrapMatchUp(GameStatus.Forfeit, playerIdentity);
                return;
            }
                
            if(currentPlayer == playerIdentity)
                currentPlayer = playerTurnQueue[(playerIndex) % playerTurnQueue.Count]; // not adding 1 because queue size got reduced
        
        }
        else
            UtilityClass.LogMessages("Player to kick NOT FOUND");
    }


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
            currentPlayer = playerTurnQueue[(playerIndexInList + 1) % playerTurnQueue.Count];
        }

    }

    [ServerCallback]
    public void WrapMatchUp(GameStatus status, NetworkIdentity player = null)
    {
        switch(status)
        {
            case GameStatus.Won:
                RPC_ShowWinner(player);
                break;
            case GameStatus.Draw:
                RPC_ShowDraw();
                break;
            case GameStatus.Forfeit:
                RPC_PlayerForfeited();
                StartCoroutine(ServerEndMatch());
                break;
        }
        

    }

    [ServerCallback]
    public IEnumerator ServerEndMatch()
    {
        UtilityClass.LogMessages("Ending Match");
        RPC_EndMatch(); // client match controller will clean up.
        yield return new WaitForSeconds(0.1f); // and wait for it.
         
        LobbyManager._instance.OnPlayerDisconnected -= OnPlayerDisconnects;

        foreach(var playerIdentity in matchPlayers.Keys)
        {
            NetworkConnectionToClient conn = playerIdentity.connectionToClient;
            NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Destroy);

            // add server end match for room as well.
            // change player status for the lobby they came from

            if(matchInfo.lobby == Lobby.QuickLobby)
            {
                PlayerInfo playerInfo = LobbyManager._lobby[conn];
                playerInfo.playerState = PlayerState.QuickJoinLobby;
                LobbyManager._lobby[conn] = playerInfo;
            }

            PlayerManager.RemovePlayerFromLobby(conn);
        }

        yield return null;
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    public void RPC_EndMatch()
    {
        UtilityClass.LogMessages("Ending match on client side");

        _canvasController.InitializeCanvasOnline();
        _canvasController.gameObject.SetActive(true);  
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
    }

    [ClientRpc]
    public void RPC_ShowDraw()
    {
        _uiManager.ShowDrawScreen();
    }

    [ClientRpc]
    public void RPC_PlayerForfeited()
    {
        UtilityClass.LogMessages($"Player forfeited: isLocalPlayer");
    }


    [ClientRpc]
    public void RPC_OnPlayerDisconnected()
    {

    }
    #endregion

    #region Server Callbacks

    [ServerCallback]
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
        UtilityClass.LogMessages("Current Player Changed");
        int playerIndex = GetIdentitiesIndexFromList(newPlayerIdentity);
        _uiManager.UpdatePlayerTurnUI(playerIndex);
    }

    #endregion


    // Helping functions
    public int GetIdentitiesIndexFromList(NetworkIdentity playerIdentity)
    {
        for(int playerIndex = 0; playerIndex < playerTurnQueue.Count; playerIndex++)
            if (playerTurnQueue[playerIndex] == playerIdentity)
                return playerIndex;
        
        return -1;
    }

    public int GetCellValue(int row, int column)
    {
        return (row * matchInfo.gridSize) + column;
    }

    #endregion
}