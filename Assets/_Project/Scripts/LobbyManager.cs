using Mirror;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections;
using System.Collections.Generic;
using TicTacToe;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    internal Dictionary<NetworkConnectionToClient, PlayerInfo> _lobby = new Dictionary<NetworkConnectionToClient, PlayerInfo>();
    internal Dictionary<GameMode, Dictionary<GridTier, Queue<NetworkConnectionToClient>>> _queues = new Dictionary<GameMode, Dictionary<GridTier, Queue<NetworkConnectionToClient>>>();
    internal Dictionary<int, List<NetworkConnectionToClient>> _matches = new Dictionary<int, List<NetworkConnectionToClient>>();

    System.Random _random = new System.Random();

    #region matchmaking parameters
    float _matchmakingRate = 10.0f;

    // classic mode constraints
    int _minPlayerCount = 2;
    int _maxPlayerCount = 7;

    

    #endregion
    // keep track of when player started waiting

    private void Awake()
    {
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
            if (queue.Count == _minPlayerCount)
                ExtractPlayerForMatch(mode, 2, queue);
            else if (queue.Count > _minPlayerCount)
            {
                int randomTryOnSameBatch = 2;
                while (true)
                {
                    int randomInteger = _random.Next(2, _maxPlayerCount);
                    if (queue.Count - randomInteger >= 0)
                    {
                        ExtractPlayerForMatch(mode, randomInteger, queue);
                    }
                    else
                    {
                        if (queue.Count < _minPlayerCount)
                            break;

                        // try generating random number some more
                        else if (randomTryOnSameBatch != 0)
                        {
                            randomTryOnSameBatch--;
                            continue;
                        }

                        // just match the players left
                        ExtractPlayerForMatch(mode, queue.Count, queue);
                        break;
                    }
                }
            }
            else
                Debug.Log("Not Enough Player ...");
            
        }

        yield return null;
    }

    [ServerCallback]

    void ExtractPlayerForMatch(GameMode mode, int numberofPlayers, Queue<NetworkConnectionToClient> queue)
    {
        int matchID = _random.Next(0, 100);
        _matches[matchID] = new List<NetworkConnectionToClient>();
        

        for (int playerIndex = 0; playerIndex < numberofPlayers; playerIndex++)
        {
            NetworkConnectionToClient playerConnection = queue.Dequeue();
            PlayerInfo player = _lobby[playerConnection];
            player.playerState = LobbyPlayerState.UndergoingMatchmaking;
            _matches[matchID].Add(playerConnection);

            Debug.Log($"Adding: {playerConnection} to match: {matchID}");
        }

        StartMatch(matchID);
    }

    [ServerCallback]
    void StartMatch(int matchID)
    {
        Debug.Log("Starting Match");
        List<PlayerInfo> playerList = new List<PlayerInfo>();

        foreach (var conn in _matches[matchID])
            playerList.Add(_lobby[conn]);

        foreach(NetworkConnectionToClient playerConnection in _matches[matchID])
        {
            playerConnection.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Start, playersInfo = playerList.ToArray() });
        }
    }

    [ServerCallback]
    public void AddPlayerToLobby(NetworkConnectionToClient connection, OnlineMatchInfo playerPrefs)
    {
        if (!_lobby.ContainsKey(connection))
        {
            _lobby.Add(connection, new PlayerInfo { playerPreference = playerPrefs, playerState = LobbyPlayerState.Lobby });
            AddPlayerToWaiting(connection);
        }
        else
        {
            Debug.Log("Player already in the lobby");
            AddPlayerToWaiting(connection);
        }
            
    }

    [ServerCallback]
    public void AddPlayerToWaiting(NetworkConnectionToClient connection)
    {
        if (_lobby.ContainsKey(connection))
        {
            PlayerInfo playerInfo = _lobby[connection];
            playerInfo.playerState = LobbyPlayerState.Waiting;
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
}
