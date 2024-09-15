using Mirror;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections;
using System.Collections.Generic;
using TicTacToe;
using UnityEditorInternal;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private static LobbyManager _instance;
    internal static Dictionary<NetworkConnectionToClient, PlayerInfo> _lobby = new Dictionary<NetworkConnectionToClient, PlayerInfo>();

    internal Dictionary<GameMode, Dictionary<GridTier, Queue<NetworkConnectionToClient>>> _queues = new Dictionary<GameMode, Dictionary<GridTier, Queue<NetworkConnectionToClient>>>();
    internal Dictionary<Guid, HashSet<NetworkConnectionToClient>> _matches = new Dictionary<Guid, HashSet<NetworkConnectionToClient>>();

    System.Random _random = new System.Random();

    #region Testing References
    [SerializeField]
    GameObject _demoMarker;
    [SerializeField]
    GameObject _stageObject;
    #endregion


    #region quick join matchmaking parameters
    float _matchmakingRate = 10.0f;

    // classic mode constraints
    int _minPlayerCount = 2;
    int _maxPlayerCount = 7;

    

    #endregion
    // keep track of when player started waiting

    private void Awake()
    {
        if(_instance != null && _instance != this)
            Destroy(_instance);
        else
            _instance = this;

        Dictionary<GridTier, Queue<NetworkConnectionToClient>> tempDict1 = new Dictionary<GridTier, Queue<NetworkConnectionToClient>>();
        tempDict1[GridTier.Low] = new Queue<NetworkConnectionToClient>();
        tempDict1[GridTier.High] = new Queue<NetworkConnectionToClient>();

        Dictionary<GridTier, Queue<NetworkConnectionToClient>> tempDict2 = new Dictionary<GridTier, Queue<NetworkConnectionToClient>>();
        tempDict1[GridTier.Low] = new Queue<NetworkConnectionToClient>();
        tempDict1[GridTier.High] = new Queue<NetworkConnectionToClient>();

        _queues.Add(GameMode.Classic, tempDict1);
        _queues.Add(GameMode.Blitz, tempDict2);

    }

    [ServerCallback]
    public void OnStartServer()
    {
        Debug.Log("Fired on start server: Lobby Manager");
        InvokeRepeating("StartMatchmaking", 1.0f, _matchmakingRate);
    }

    #region Online Quick Join

    [ServerCallback]
    void StartMatchmaking()
    {
        Debug.Log("Invoked Matchmaking ...");
        // run these in parallel and wait for all of them to finish
        Coroutine classic = StartCoroutine(PerformNormalModeMatchmaking(GameMode.Classic));
        Coroutine blitz = StartCoroutine(PerformNormalModeMatchmaking(GameMode.Blitz));
    }

    IEnumerator PerformNormalModeMatchmaking(GameMode mode)
    {
        Debug.Log($"Matchmaking for: {mode}");

        foreach (var queue in _queues[mode].Values)
        {
            int randomTryOnSameBatch = 2; // make sure to reset this as well
            int randomInteger = 0;

            while (queue.Count >= _minPlayerCount)
            {
                // if only bare minimum avaialable, proceed to matchmaking
                if (queue.Count == _minPlayerCount)
                {
                    ExtractPlayerForMatch(mode, _minPlayerCount, queue);
                    break;
                }

                // decide how many players will be in this match instance
                randomInteger = _random.Next(_minPlayerCount, _maxPlayerCount);

                if (randomTryOnSameBatch <= 0)
                {
                    ExtractPlayerForMatch(mode, queue.Count, queue);
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
                        ExtractPlayerForMatch(mode, randomInteger, queue);
                        randomTryOnSameBatch = 2;
                    }                    
                }
                    
            }

                /* initial while loop*/
                //while (true)
                //{
                //    int randomInteger = _random.Next(2, _maxPlayerCount);
                //    if (queue.Count - randomInteger >= 0)
                //    {
                //        ExtractPlayerForMatch(mode, randomInteger, queue);
                //    }
                //    else
                //    {
                //        if (queue.Count < _minPlayerCount)
                //            break;

                //        // try generating random number some more
                //        else if (randomTryOnSameBatch != 0)
                //        {
                //            randomTryOnSameBatch--;
                //            continue;
                //        }

                //        // just match the players left
                //        ExtractPlayerForMatch(mode, queue.Count, queue);
                //        break;
                //    }
                //}
        }

        yield return null;
    }

    [ServerCallback]

    void ExtractPlayerForMatch(GameMode mode, int numberofPlayers, Queue<NetworkConnectionToClient> queue)
    {
        Guid matchID = Guid.NewGuid();
        _matches[matchID] = new HashSet<NetworkConnectionToClient>();
        

        for (int playerIndex = 0; playerIndex < numberofPlayers; playerIndex++)
        {
            NetworkConnectionToClient playerConnection = queue.Dequeue();
            PlayerInfo player = _lobby[playerConnection];
            player.playerState = PlayerState.UndergoingMatchmaking;
            _matches[matchID].Add(playerConnection);

            Debug.Log($"Adding: {playerConnection} to match: {matchID}");
        }

        StartMatch(matchID);
    }

    [ServerCallback]
    void StartMatch(Guid matchID)
    {
        Debug.Log("Starting Match");
        // information for each player in match
        List<PlayerInfo> playerList = new List<PlayerInfo>();

        foreach (var conn in _matches[matchID])
            playerList.Add(_lobby[conn]);

        foreach(NetworkConnectionToClient playerConnection in _matches[matchID])
        {
            GameObject currentPlayerPrefab = Instantiate(NetworkManager.singleton.playerPrefab);
            currentPlayerPrefab.GetComponent<NetworkMatch>().matchId = matchID;
            NetworkServer.AddPlayerForConnection(playerConnection, currentPlayerPrefab);
            
            playerConnection.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Start, playersInfo = playerList.ToArray() });
        }

        GameObject matchSpecificObject = Instantiate(_demoMarker);
        matchSpecificObject.GetComponent<NetworkMatch>().matchId = matchID;
        NetworkServer.Spawn(matchSpecificObject);

        GameObject stage = Instantiate(_stageObject);
        stage.GetComponent<NetworkMatch>().matchId = matchID;
        NetworkServer.Spawn(stage);
    }

    [ServerCallback]
    public static void AddPlayerToLobby(NetworkConnectionToClient connection)
    {
        if (!_lobby.ContainsKey(connection))
            _lobby.Add(connection, new PlayerInfo());

        else
            Debug.Log("Player already in the lobby");
    }

    [ServerCallback]
    public void AddPlayerToWaiting(NetworkConnectionToClient connection, OnlineMatchInfo onlineMatchInfo)
    {
        if (_lobby.ContainsKey(connection))
        {
            PlayerInfo playerInfo = _lobby[connection];
            playerInfo.playerPreference = new OnlineMatchInfo { mode = onlineMatchInfo.mode, gridTier = onlineMatchInfo.gridTier };
            playerInfo.playerState = PlayerState.UndergoingMatchmaking;
            _lobby[connection] = playerInfo;

            _queues[playerInfo.playerPreference.mode][playerInfo.playerPreference.gridTier].Enqueue(connection);
        }
        else
            Debug.Log("Player already waiting");

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

    #endregion
}
