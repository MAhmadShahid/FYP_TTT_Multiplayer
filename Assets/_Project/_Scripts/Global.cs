using JetBrains.Annotations;
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

    #region Player Manager

    [Serializable]
    public enum Lobby
    {
        None,
        QuickLobby,
        RoomLobby
    }

    [Serializable]
    public enum PlayerState
    {
        Online,
        QuickJoinLobby,
        UndergoingMatchmaking,
        RoomLobby,
        InRoom,
        Playing
    }

    [Serializable]
    public class Player
    {
        public Guid playerid;
        public NetworkConnectionToClient connection;
        public string name;
        public PlayerState state;
    }

    [Serializable]
    public struct PlayerInfo
    {
        public OnlineMatchInfo playerPreference;
        public PlayerState playerState;
    }

    #endregion

    #region Screens
    [Serializable]
    public struct ScreenStructure<T>
    {
        public T Screen;
        public GameObject screenObject;
        public bool showTopBar;
        public bool showBottomBar;
    }

    [Serializable]
    public enum OnlineScreens
    {
        None,
        Main,
        OnlineLobby,
        GameMode,
        GridTier,
        MatchSearching,
        Start,
        RoomListing,
        RoomView          
    }

    #endregion

    #region Messages

    #region Player Handling

    public enum PlayerHandlingOperation
    {
        None,
        AddToLobby,
        RemoveFromLobby
    }
    public struct PlayerHandlingMessage: NetworkMessage
    {
        public PlayerHandlingOperation operation;
        public Lobby lobbyType;
    }
    #endregion

    #region Match Messages

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

    #endregion

    #region Room Messages
    public struct ServerRoomMessage : NetworkMessage
    {
        public ServerRoomOperation operation;
    }

    public struct ClientRoomMessage : NetworkMessage
    {
        public ClientRoomOperation roomOperation;
        public Room[] roomsInfo;
    }

    public struct Room
    {
        public Guid roomId;
        public string roomName;
        public GameMode gameMode;
        public int gridSize;
        public int totalPlayersAllowed;
        public int currentPlayerCount;
        public float blitzSeconds;
        public Guid roomOwner;
    }



    public enum ClientRoomOperation
    {
        None,
        Created,
        RefreshList,
        Update,
        Kick,
    }

    [Serializable]
    public enum ServerRoomOperation
    {
        None,
        Create,
        Update,
        Join,
        Leave
    }

    #endregion
    #endregion

    #region Others
    [System.Serializable]
    public struct OnlineMatchInfo
    {
        public GameMode mode;
        public GridTier gridTier;
    }

    [Serializable]
    public struct AddedMatchInfo
    {
        public string name;
        public GameMode mode;
        public GridTier gridTier;
        public int playerCount;
    }
    #endregion

}