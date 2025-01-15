using JetBrains.Annotations;
using Mirror;
using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicTacToe
{
    public static class Configurations
    {
        public static readonly Dictionary<int, Dictionary<int, int>> validStages = new Dictionary<int, Dictionary<int, int>>
        {
            // [GridSize] = { [PlayerCount] = WinCondition } 
            [3] = new Dictionary<int, int> { [2] = 3 },
            [5] = new Dictionary<int, int>
            {
                [2] = 5,
                [3] = 4,
                [4] = 3
            },
            [7] = new Dictionary<int, int>
            {
                [2] = 6,
                [3] = 5,
                [4] = 4,
                [5] = 3
            },
            [9] = new Dictionary<int, int>
            {
                [2] = 7,
                [3] = 6,
                [4] = 5,
                [5] = 4,
                [6] = 3
            },
        };

        public static readonly Dictionary<GridTier, List<int>> gridTierSizes = new Dictionary<GridTier, List<int>>
        {
            [GridTier.Low] = new List<int> { 3, 5 },
            [GridTier.High] = new List<int> { 7, 9 }
        };

    }


    #region General

    [System.Serializable]
    public enum ConnectionMode
    {
        Local,
        Online
    }



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
        Inqueue,
        RoomLobby,
        InRoom,
        Playing,
        Leaving
    }

    [Serializable]
    public class Player
    {
        public Guid playerid;
        public NetworkConnectionToClient connection;
        public string name;
        public PlayerState state;

        public Player(Guid playerid, NetworkConnectionToClient connection, string name, PlayerState state)
        {
            this.playerid = playerid;
            this.connection = connection;
            this.name = name;
            this.state = state;
        }

        public Player(PlayerStruct player, NetworkConnectionToClient connection)
        {
            this.playerid =    player.playerid;
            this.connection =  connection;
            this.name =        player.name;
            this.state =       player.state;
        }
    }

    [Serializable]
    public struct PlayerStruct
    {
        public Guid playerid;
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
        RoomView,
        Connecting,
        AdvanceSettings,
        OfflineLobby 
    }

    #endregion

    #region Messages

    #region Player Handling

    public enum PlayerHandlingOperation
    {
        None,
        AddToLobby,
        RemoveFromLobby,
        RequestPlayerInfo
    }

    public enum ClientPlayerOperation
    {
        None,
        Added,
        Removed
    }

    public struct PlayerHandlingMessage : NetworkMessage
    {
        public PlayerHandlingOperation operation;
        public Lobby lobbyType;
    }

    public struct ClientPlayerMessage : NetworkMessage
    {
        public ClientPlayerOperation operation;
        public PlayerStruct player;
        public Lobby lobby;
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
        public PlayerStruct[] playerStructInfo;
    }

    #endregion

    #region Room Messages
    public struct ServerRoomMessage : NetworkMessage
    {
        public ServerRoomOperation operation;
        public Guid roomID;
        public ClientRoomSettings roomSettings;
        public Guid[] playerIDs;
    }

    public struct ClientRoomMessage : NetworkMessage
    {
        public ClientRoomOperation roomOperation;
        public Room[] roomsInfo;
        public PlayerStruct[] playerInfos; 
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


    [Serializable]
    public enum ClientRoomOperation
    {
        None,
        Created,
        RefreshList,
        Update,
        Kick,
        Joined,
        Left
    }

    [Serializable]
    public enum ServerRoomOperation
    {
        None,
        Create,
        SettingChange,
        Join,
        Leave,
        Kick,
        Start
    }

    #endregion
    #endregion

    #region Others
    [Serializable]
    public struct OnlineMatchInfo
    {
        public GameMode mode;
        public GridTier gridTier;
        public bool online;
    }

    [Serializable]
    public struct MatchInfo
    {
        public Guid matchID;
        public string name;
        public GameMode mode;
        public int gridSize;
        public int playerCount;
        public Lobby lobby;
    }

    [Serializable]
    public struct ClientRoomSettings
    {
        public GameMode roomMode;
        public int gridSize;
        public int participants;
        public int blitzSeconds;
    }

    [Serializable]
    public enum GameStatus
    {
        Won,
        Draw,
        Forfeit
    }

    #endregion

}