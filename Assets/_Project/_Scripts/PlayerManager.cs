using Mirror;
using System.Collections.Generic;
using UnityEngine;
using TicTacToe;
using System.Linq;
using System;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;

    Player _localPlayer;

    Dictionary<NetworkConnectionToClient, Guid> _playerConnectionMapping = new Dictionary<NetworkConnectionToClient, Guid>();
    Dictionary<Guid, Player> _onlinePlayers = new Dictionary<Guid, Player>();

    #region Name Params
    HashSet<string> _generatedNames = new HashSet<string>();
    List<string> _names = new List<string>
        {
            "Osman",
            "Elon",
            "Trump",
            "JoeBiden",
            "Ertugral",
            "Shapper",
            "Excalibur",
            "RonaldoSUI",
            "ProNigger",
            "Batman",
            "Chapri",
            "Pedo",
            "MarkZingerBurger",
            "SteelTitan",
            "WindRanger",
            "SkyGuardian",
            "FlameBender",
            "CrystalMage",
            "StormBreaker",
            "LightningFury",
            "DragonSlayer",
            "SilentNinja",
            "MysticSage",
            "PhantomAssassin",
            "ArcaneWarden"
        };
    #endregion

    private void Awake()
    {
        if(_instance != null && _instance != this)
            Destroy(_instance);
        else
            _instance = this;

    }

    //public static void AddPlayerToOnlinePool(NetworkConnectionToClient connectionToClient, PlayerInfo playerInfo)
    //{
    //    playerInfo.playerPreference.mode = GameMode.None;
    //    playerInfo.playerPreference.gridTier = GridTier.None;
    //    playerInfo.playerState = PlayerState.Online;

    //    _onlinePlayers.Add(connectionToClient, playerInfo); 
    //}

    #region Server Callbacks

    [ServerCallback]
    public static void OnPlayerConnect(NetworkConnectionToClient clientConnection)
    {
        Guid playerID = Guid.NewGuid();
        Player player = new Player (playerID, clientConnection, PlayerManager.GeneratePlayerName(), PlayerState.Online);
        PlayerManager._instance._onlinePlayers.Add(playerID, player);
        PlayerManager._instance._playerConnectionMapping.Add(clientConnection, playerID);

        Debug.Log("Player Connected To Server: ");
        Debug.Log($"Name: {player.name}; Connection: {clientConnection}");
    }

    [ServerCallback]
    public static void OnPlayerDisconnect(NetworkConnectionToClient clientConnection)
    {
        Guid playerID = _instance._playerConnectionMapping[clientConnection];

        if(playerID == null)
        {
            Debug.Log("Disconnecting: User doesn't exist in online player pool");
            return;
        }    

        Player player = PlayerManager._instance._onlinePlayers[playerID];

        if(player == null)
        {
            Debug.Log("Disconnecting: User Id exists, but player object doesn't");
            return;
        }

        if(player.state != PlayerState.Online)
            RemovePlayerFromLobby(clientConnection);

        Debug.Log($"Disconnecting Player: {player.name}; Connection: {clientConnection}");
        
        // wrap up player from this script
        _instance._generatedNames.Remove(player.name);
        _instance._playerConnectionMapping.Remove(clientConnection);
        _instance._onlinePlayers.Remove(playerID);

        
    }

    [ServerCallback]
    // For player state management.
    public static void AddPlayerToLobby(NetworkConnectionToClient clientConnection, Lobby lobby)
    {
        if (!_instance._playerConnectionMapping.ContainsKey(clientConnection))
            return;

        PlayerManager manager = PlayerManager._instance;
        Player player = manager._onlinePlayers[manager._playerConnectionMapping[clientConnection]];

        if (player.state != PlayerState.Online)
        {   
            Debug.LogWarning("Request to add player: Player not in online lobby");
            return;
        }

        switch (lobby)
        {
            case Lobby.QuickLobby:
                player.state = PlayerState.QuickJoinLobby;
                LobbyManager.AddPlayerToLobby(clientConnection);
                break;
            case Lobby.RoomLobby:
                player.state = PlayerState.RoomLobby;
                RoomManager.AddPlayerToLobby(clientConnection);
                break;
        }

        Debug.Log($"Added player to lobby: {player.state}");

    }

    [ServerCallback]
    // For player state management.
    public static void RemovePlayerFromLobby(NetworkConnectionToClient clientConnection)
    {
        if (!_instance._playerConnectionMapping.ContainsKey(clientConnection))
        {
            Debug.LogWarning("Player doesn't exist in online pool");
            return;
        }
            
        PlayerManager manager = PlayerManager._instance;
        Player player = manager._onlinePlayers[manager._playerConnectionMapping[clientConnection]];

        if (player.state == PlayerState.Online)
        {
            Debug.LogWarning("Removing: Player still in online pool");
            return;
        }

        Lobby lobby = player.state == PlayerState.QuickJoinLobby ? Lobby.QuickLobby : Lobby.RoomLobby;
        player.state = PlayerState.Online;

        Debug.Log($"Removing: Removing player from {lobby}");

        switch (lobby)
        {
            case Lobby.QuickLobby:
                LobbyManager.RemovePlayerFromLobby(clientConnection, true);
                break;
            case Lobby.RoomLobby:
                RoomManager.Instance.RemovePlayerFromLobby(clientConnection);
                break;
        }



        Debug.Log($"Added player to lobby: {player.state}");

    }

    [ServerCallback]
    public static Player GetPlayerFromConnection(NetworkConnectionToClient clientConnection) => _instance._onlinePlayers[_instance._playerConnectionMapping[clientConnection]];
    [ServerCallback]
    public static PlayerStruct GetPlayerStructureFromConnection(NetworkConnectionToClient clientConnection)
    { 
        Player playerObject = _instance._onlinePlayers[_instance._playerConnectionMapping[clientConnection]];
        return new PlayerStruct
        {
            playerid = playerObject.playerid,
            name = playerObject.name,
            state = playerObject.state
        };
    }

 

    #endregion

    #region Client Callbacks
    [ClientCallback]
    public static void AddPlayerOnClient(PlayerStruct player)
    {
        Debug.Log("Client: Adding player on client");
        _instance._localPlayer = new Player(player, null);
    }
    

    [ClientCallback]
    public static PlayerStruct GetLocalPlayerStructure()
    {
        if (_instance._localPlayer == null)
        {
            Debug.Log("Client: Player instance null on client");
            return new PlayerStruct();
        }
            

        return new PlayerStruct {
            playerid = _instance._localPlayer.playerid,
            name = _instance._localPlayer.name,
            state = _instance._localPlayer.state
        };

    }
    #endregion

    #region Helping Function
    private static string GeneratePlayerName()
    {
        string name =  _instance._names.FirstOrDefault(name => !_instance._generatedNames.Contains(name));
        _instance._generatedNames.Add(name);
        return name;
    }
    #endregion


    //public static void AddPlayerToLobby(NetworkConnectionToClient connection, Lobby lobby)
    //{
    //    PlayerInfo playerInfo = _onlinePlayers[connection];

    //    if (playerInfo.playerState != PlayerState.Online)
    //        return;

    //    if(lobby == Lobby.QuickLobby)
    //    {
    //        playerInfo.playerState = PlayerState.QuickJoinLobby;
    //        _onlinePlayers[connection] = playerInfo;
    //        LobbyManager.AddPlayerToLobby(connection, playerInfo.)
    //    }

    //}
}
