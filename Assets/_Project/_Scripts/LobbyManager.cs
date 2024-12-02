using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TicTacToe;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager _instance;
    public static Dictionary<NetworkConnectionToClient, PlayerInfo> _lobby = new Dictionary<NetworkConnectionToClient, PlayerInfo>();

    internal Dictionary<GameMode, Dictionary<GridTier, Queue<NetworkConnectionToClient>>> _queues = new Dictionary<GameMode, Dictionary<GridTier, Queue<NetworkConnectionToClient>>>();
    internal Dictionary<Guid, HashSet<NetworkConnectionToClient>> _matches = new Dictionary<Guid, HashSet<NetworkConnectionToClient>>();

    public event Action<NetworkConnectionToClient> OnPlayerDisconnected;

    System.Random _random = new System.Random();
    LinkedList<NetworkConnectionToClient> queue = new LinkedList<NetworkConnectionToClient>();

    #region Testing References
    [SerializeField]
    GameObject _demoMarker;
    [SerializeField]
    GameObject _stageObject;
    #endregion

    [SerializeField] GameObject _matchController;

    #region quick join matchmaking parameters
    float _matchmakingRate = 10.0f;

    Dictionary<GridTier, Tuple<int, int>> constraints = new Dictionary<GridTier, Tuple<int, int>>
    {
        [GridTier.Low] = new Tuple<int, int>(2, 4),
        [GridTier.High] = new Tuple<int, int>(2, 6)
    };
    

    #endregion
    // keep track of when player started waiting

    private void Awake()
    {
        if(_instance != null && _instance != this)
            Destroy(_instance);
        else
            _instance = this;

        Dictionary<GridTier, Queue<NetworkConnectionToClient>> classicQueue = new Dictionary<GridTier, Queue<NetworkConnectionToClient>>();
        classicQueue[GridTier.Low] = new Queue<NetworkConnectionToClient>();
        classicQueue[GridTier.High] = new Queue<NetworkConnectionToClient>();

        Dictionary<GridTier, Queue<NetworkConnectionToClient>> blitzQueue = new Dictionary<GridTier, Queue<NetworkConnectionToClient>>();
        blitzQueue[GridTier.Low] = new Queue<NetworkConnectionToClient>();
        blitzQueue[GridTier.High] = new Queue<NetworkConnectionToClient>();

        _queues.Add(GameMode.Classic, classicQueue);
        _queues.Add(GameMode.Blitz, blitzQueue);


    }

    [ServerCallback]
    public void OnStartServer()
    {
        //Debug.Log("Fired on start server: Lobby Manager");
        InvokeRepeating("StartMatchmaking", 1.0f, _matchmakingRate);
    }

    #region Online Quick Join

    [ServerCallback]
    void StartMatchmaking()
    {
        //Debug.Log("Invoked Matchmaking ...");
        // run these in parallel and wait for all of them to finish
        Coroutine classic = StartCoroutine(PerformNormalModeMatchmaking(GameMode.Classic));
        Coroutine blitz = StartCoroutine(PerformNormalModeMatchmaking(GameMode.Blitz));
    }

    IEnumerator PerformNormalModeMatchmaking(GameMode mode)
    {
        //Debug.Log($"Matchmaking for: {mode}");

        foreach ( var keyValuePair in _queues[mode] ) // var queue in _queues[mode].Values
        {
            //UtilityClass.LogMessages($"Key: {keyValuePair.Key} Value: {keyValuePair}");
            var queue = keyValuePair.Value;
            int randomTryOnSameBatch = 2; // make sure to reset this as well
            int randomInteger = 0;

            var tierConstraint = constraints[keyValuePair.Key];
            var minPlayerCount = tierConstraint.Item1;
            var maxPlayerCount = tierConstraint.Item2;

            while (queue.Count >= minPlayerCount)
            {
                // if only bare minimum avaialable, proceed to matchmaking
                if (queue.Count == minPlayerCount)
                {
                    ExtractPlayerForMatch(mode, keyValuePair.Key, minPlayerCount, keyValuePair.Value);
                    break;
                }

                // decide how many players will be in this match instance
                randomInteger = _random.Next(minPlayerCount, maxPlayerCount);

                if (randomTryOnSameBatch <= 0)
                {
                    ExtractPlayerForMatch(mode, keyValuePair.Key, queue.Count, keyValuePair.Value);
                }
                else
                {
                    if(queue.Count < randomInteger)
                    {
                        randomTryOnSameBatch--;
                        continue;
                    }
                    else
                    {
                        ExtractPlayerForMatch(mode, keyValuePair.Key, randomInteger, keyValuePair.Value);
                        randomTryOnSameBatch = 2;
                    }                    
                }
                    
            }
        }

        yield return null;
    }

    [ServerCallback]

    void ExtractPlayerForMatch(GameMode mode, GridTier gridTier, int numberofPlayers, Queue<NetworkConnectionToClient> queue)
    {
        // in case of any race condition (queues get rebuilt upon player disconnection)
        if (numberofPlayers > queue.Count)
            return;

        Guid matchID = Guid.NewGuid();
        _matches.Add(matchID, new HashSet<NetworkConnectionToClient>());

        for (int playerIndex = 0; playerIndex < numberofPlayers; playerIndex++)
        {
            NetworkConnectionToClient playerConnection;
            
            if(!queue.TryDequeue(out playerConnection))
            {
                Debug.LogWarning("Race Condition: Queue has less number of player than required. This may be caused when the last player has disconnected (queues get rebuilt upon player disconnection)");
                AbortMatch(matchID);
                return;
            }

            PlayerInfo player = _lobby[playerConnection];

            if(player.playerState == PlayerState.Leaving)
            {
                AbortMatch(matchID);
                return;
            }

            player.playerState = PlayerState.UndergoingMatchmaking;
            _lobby[playerConnection] = player;

            _matches[matchID].Add(playerConnection);

            Debug.Log($"Adding: {playerConnection} to match: {matchID}");
        }

        StartMatch(matchID, mode, gridTier);
    }

    [ServerCallback]
    void StartMatch(Guid matchID, GameMode mode, GridTier gridTier)
    {

        // information for each player in match
        List<PlayerStruct> playerList = new List<PlayerStruct>();
        Dictionary<NetworkIdentity, PlayerStruct> matchPlayers = new Dictionary<NetworkIdentity, PlayerStruct>();
        Dictionary<NetworkIdentity, NetworkConnectionToClient> connectionIdentityMapping = new Dictionary<NetworkIdentity, NetworkConnectionToClient>();

        foreach (var conn in _matches[matchID])
        {
            PlayerInfo info = _lobby[conn];
            PlayerStruct player = PlayerManager.GetPlayerStructureFromConnection(conn);

            if (info.playerState == PlayerState.Leaving)
            {
                AbortMatch(matchID);
                return;
            }

            info.playerState = PlayerState.Playing;
            _lobby[conn] = info;

            playerList.Add(player);
        }
            

        foreach(NetworkConnectionToClient playerConnection in _matches[matchID])
        {
            GameObject currentPlayerPrefab = Instantiate(NetworkManager.singleton.playerPrefab);
            currentPlayerPrefab.GetComponent<NetworkMatch>().matchId = matchID;
            NetworkServer.AddPlayerForConnection(playerConnection, currentPlayerPrefab);
            
            playerConnection.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Start, playerStructInfo = playerList.ToArray() });
            connectionIdentityMapping.Add(currentPlayerPrefab.GetComponent<NetworkIdentity>(), playerConnection);
            matchPlayers.Add(currentPlayerPrefab.GetComponent<NetworkIdentity>(), PlayerManager.GetPlayerStructureFromConnection(playerConnection));
        }

        // decide on grid size
        int gridSize = ReturnGridSizeInTier(gridTier, playerList.Count);

        MatchInfo matchInfo = new MatchInfo 
        {
            matchID = matchID,
            mode = mode, 
            gridSize = gridSize, 
            playerCount = playerList.Count, 
            lobby = Lobby.QuickLobby 
        };
        
        // instantiating + assigning match id
        GameObject matchControllerObject = Instantiate(_matchController);
        matchControllerObject.GetComponent<NetworkMatch>().matchId = matchID;

        // prepping all relevant data
        MatchController matchController = matchControllerObject.GetComponent<MatchController>();
        matchController.matchInfo = matchInfo;

        foreach (var player in matchPlayers)
        {
            matchController.matchPlayers.Add(player.Key, player.Value);
            matchController.playerTurnQueue.Add(player.Key);
            matchController.connectionIdentityMapping.Add(player.Key, connectionIdentityMapping[player.Key]);
        }

        matchController.ShuffleList();

        // spawning on client side after data is loaded
        NetworkServer.Spawn(matchControllerObject);
        // updating current player
        matchController.currentPlayer = matchController.playerTurnQueue[0];
        _instance.OnPlayerDisconnected += matchController.OnServerPlayerDisconnects;

        matchController.OnMatchEnd += (matchID) =>
        {
            _instance.OnPlayerDisconnected -= matchController.OnServerPlayerDisconnects;

            foreach (var player in matchController.matchPlayers.Keys)
            {
                PlayerInfo playerInfo = _lobby[player.connectionToClient];
                playerInfo.playerState = PlayerState.QuickJoinLobby;
                LobbyManager._lobby[player.connectionToClient] = playerInfo;
                PlayerManager.RemovePlayerFromLobby(player.connectionToClient);

                player.connectionToClient.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Reset });
            }
        };

        matchController.OnPlayerLeave += (matchID, clientConnection) =>
        {

            StartCoroutine(matchController.OnServerPlayerLeave(clientConnection));

            PlayerInfo playerInfo = _lobby[clientConnection];
            playerInfo.playerState = PlayerState.QuickJoinLobby;
            LobbyManager._lobby[clientConnection] = playerInfo;
            PlayerManager.RemovePlayerFromLobby(clientConnection);

        };

        // Testing spawn
        //GameObject matchSpecificObject = Instantiate(_demoMarker);
        //matchSpecificObject.GetComponent<NetworkMatch>().matchId = matchID;
        //NetworkServer.Spawn(matchSpecificObject);

        //GameObject stage = Instantiate(_stageObject);
        //stage.GetComponent<NetworkMatch>().matchId = matchID;
        //NetworkServer.Spawn(stage);
    }

    [ServerCallback]
    void AbortMatch(Guid matchID)
    {
        HashSet<NetworkConnectionToClient> clientsInMatch = _matches[matchID];
        _matches.Remove(matchID);

        foreach (var client in clientsInMatch)
        {
            AddPlayerToQueue(client, _lobby[client].playerPreference);
        }
    }

    [ServerCallback]
    public static void AddPlayerToLobby(NetworkConnectionToClient connection)
    {
        if (!_lobby.ContainsKey(connection))
        {
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.playerState = PlayerState.QuickJoinLobby;
            _lobby.Add(connection, playerInfo);
        }
            

        else
            Debug.Log("Player already in the lobby");
    }

    [ServerCallback]
    public static void RemovePlayerFromLobby(NetworkConnectionToClient connection, bool disconnecting = false)
    {
        // is the player in this lobby
        if (!_lobby.ContainsKey(connection))
        {
            Debug.Log("Player isn't in quick join lobby");
            return;
        }
        
        // changing player state
        PlayerInfo playerInfo = _lobby[connection];
        PlayerState currentState = playerInfo.playerState;
        playerInfo.playerState = PlayerState.Leaving;
        _lobby[connection] = playerInfo;

        Debug.Log($"Removing From Quick Join: {connection}, Current State: {currentState}");
        _lobby.Remove(connection);

        // removing the player from queue
        // test flow when player disconnects in between
        if (currentState == PlayerState.Inqueue)
            if(playerInfo.playerPreference.mode != GameMode.None && playerInfo.playerPreference.gridTier != GridTier.None)
                _instance._queues[playerInfo.playerPreference.mode][playerInfo.playerPreference.gridTier] = new Queue<NetworkConnectionToClient>(
                    _instance._queues[playerInfo.playerPreference.mode][playerInfo.playerPreference.gridTier]
                    .Where(s => s != connection)
                );
        
        // removing player from match
        if (currentState == PlayerState.Playing)
            _instance.OnPlayerDisconnected?.Invoke(connection);

    }

    [ServerCallback]
    public void AddPlayerToQueue(NetworkConnectionToClient connection, OnlineMatchInfo onlineMatchInfo)
    {
        if (_lobby.ContainsKey(connection))
        {
            PlayerInfo playerInfo = _lobby[connection];
            playerInfo.playerPreference = new OnlineMatchInfo { mode = onlineMatchInfo.mode, gridTier = onlineMatchInfo.gridTier };
            playerInfo.playerState = PlayerState.Inqueue;
            _lobby[connection] = playerInfo;

            _queues[playerInfo.playerPreference.mode][playerInfo.playerPreference.gridTier].Enqueue(connection);

            Debug.Log($"Added Connection: {connection} Mode: {onlineMatchInfo.mode} Tier: {onlineMatchInfo.gridTier}");
        }
        else
            Debug.Log("Player already in queue");
    }

    public bool RemovePlayerFromWaiting(NetworkConnectionToClient connection)
    {
        if (_lobby.ContainsKey(connection))
        {
            _lobby.Remove(connection);
            return true;
        }

        return false;
    }

    #region Helping Function

    [ServerCallback]
    public int ReturnGridSizeInTier(GridTier gridTier, int playerCount)
    {
        var optionCount = Configurations.gridTierSizes[gridTier].Count;

        while (true)
        {
            var randomGenerated = _random.Next(optionCount);
            var gridSizeDecided = Configurations.gridTierSizes[gridTier][randomGenerated];

            if (Configurations.validStages[gridSizeDecided].ContainsKey(playerCount))
                return gridSizeDecided;
        }
    }    

    #endregion

    #endregion
}
