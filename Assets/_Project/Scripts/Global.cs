using Mirror;
using System;
using UnityEngine;

namespace TicTacToe
{
    #region General
    [System.Serializable]
    public enum GameMode
    {
        None,
        Classic,
        Blitz,
        Puzzle,
        Gamble
    }

    [System.Serializable]
    public enum GridTier
    {
        None,
        Low,
        High
    }

    #endregion

    #region Online Lobby

    [Serializable]
    public enum LobbyPlayerState
    {
        Lobby,
        Waiting,
        Inqueue,
        UndergoingMatchmaking,
        Inmatch
    }

    [Serializable]
    public struct PlayerInfo
    {
        public OnlineMatchInfo playerPreference;
        public LobbyPlayerState playerState;
    }

    #endregion

    public enum ServerMatchOperation
    {
        None,
        Matchmaking,
        CancelMatchmaking
    }

    [Serializable]
    public enum ClientMatchOperation
    {
        None,
        Matchmaking,
        Start,
        Reset
    }

    #region Screens
    [Serializable]
    public struct ScreenStructure<T>
    {
        public T Screen;
        public GameObject screenObject;
    }

    [Serializable]
    public enum OnlineLobbyScreens
    {
        None,
        GameMode,
        GridTier,
        MatchSearching,
        Start
    }

    #endregion

    [System.Serializable]
    public struct OnlineMatchInfo
    {
        public GameMode mode;
        public GridTier gridTier;
    }

    public struct ServerMatchMessage : NetworkMessage
    {
        public ServerMatchOperation serverSideOperation;
        public OnlineMatchInfo matchInfo;
    }

    public struct ClientMatchMessage : NetworkMessage
    {
        public ClientMatchOperation clientSideOperation;
        public PlayerInfo[] playersInfo;
    }
}