using Mirror;
using UnityEngine;
using TicTacToe;
using System;

public class MessageHandler : MonoBehaviour
{
    [SerializeField]
    CanvasController _canvasController;
    [SerializeField]
    LobbyManager _lobbyManager;
    [SerializeField]
    RoomManager _roomManager;

    #region Server
    [ServerCallback]
    public void RegisterAllServerHandlers()
    {
        NetworkServer.RegisterHandler<ServerMatchMessage>(OnServerMatchMessage);
        NetworkServer.RegisterHandler<PlayerHandlingMessage>(OnServerPlayerMessageHandler);
        NetworkServer.RegisterHandler<ServerRoomMessage>(OnServerRoomHandler);
    }


    #region Server Registered Functions
    [ServerCallback]
    public void OnServerPlayerMessageHandler(NetworkConnectionToClient connection, PlayerHandlingMessage message)
    {
        switch(message.operation)
        {
            case PlayerHandlingOperation.AddToLobby:
                PlayerManager.AddPlayerToLobby(connection, message.lobbyType);
                break;
            case PlayerHandlingOperation.RequestPlayerInfo:
                Debug.Log("Server: Sending player's information");
                connection.Send(new ClientPlayerMessage { operation = ClientPlayerOperation.Added, player = PlayerManager.GetPlayerStructureFromConnection(connection) });
                break;
            case PlayerHandlingOperation.RemoveFromLobby:
                PlayerManager.RemovePlayerFromLobby(connection);
                break;
        }
    }

    [ServerCallback]
    public void OnServerMatchMessage(NetworkConnectionToClient connection, ServerMatchMessage message)
    {
        if(connection == null) return;

        switch(message.serverSideOperation)
        {
            case ServerMatchOperation.Matchmaking:
                {
                    OnServerMatchmaking(connection, message);
                    break;
                }
            case ServerMatchOperation.CancelMatchmaking:
                {
                    // do cancellation logic
                    Debug.Log($"Player: {connection}, Requested cancellation thus, processing ...");
                    connection.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Reset });
                    break;
                }
        }
    }

    [ServerCallback]
    public void OnServerRoomHandler(NetworkConnectionToClient connection, ServerRoomMessage message)
    {
        Debug.Log($"Server recieved message: {message.operation}");
        switch (message.operation)
        {
            case ServerRoomOperation.Create:
                Debug.Log("Server: Client trying to create room");
                _roomManager.OnServerCreateRoom(connection);
                break;
            case ServerRoomOperation.Join:
                _roomManager.OnServerAddPlayerToRoom(message.roomID, connection);
                break;
            case ServerRoomOperation.Leave:
                _roomManager.OnServerRemovePlayerFromRoom(connection);  
                break;
        }
    }

    #endregion

    #region Server Message Handlers
    [ServerCallback]
    public void OnServerMatchmaking(NetworkConnectionToClient connection, ServerMatchMessage message)
    {
        Debug.Log($"Player Connection: {connection}");
        Debug.Log($"Player Mode Pref: {message.matchInfo.mode} \n Player Tier Pref: {message.matchInfo.gridTier}");

        _lobbyManager.AddPlayerToQueue(connection, message.matchInfo);
        connection.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Matchmaking });
    }

    #endregion

    #endregion

    #region Client

    [ClientCallback]
    public void RegisterAllClientHandlers()
    {
        NetworkClient.RegisterHandler<ClientMatchMessage>(OnClientMatchMessageReceived);
        NetworkClient.RegisterHandler<ClientRoomMessage>(OnClientRoomMessageReceived);
        NetworkClient.RegisterHandler<ClientPlayerMessage>(OnClientPlayerMessageRecieved);
    }

    [ClientCallback]
    public void OnClientMatchMessageReceived(ClientMatchMessage message)
    {
        switch (message.clientSideOperation)
        {
            case ClientMatchOperation.Matchmaking:
                {
                    Debug.Log("Client added to matchmaking");
                    _canvasController.ShowSearchingForMatchScreen();
                    break;
                }
            case ClientMatchOperation.Reset:
                {
                    Debug.Log("Client has been removed from matchmaking");
                    _canvasController.InitializeCanvasOnline();
                    break;
                }
            case ClientMatchOperation.Start:
                {
                    OnClientStartMatch(message.playersInfo);
                    break;
                }
        }
    }

    [ClientCallback]
    public void OnClientRoomMessageReceived(ClientRoomMessage message)
    {
        switch(message.roomOperation)
        {
            case ClientRoomOperation.RefreshList:
                _roomManager.RecieveRoomListing(message.roomsInfo);
                break;
            case ClientRoomOperation.Created:
                Debug.Log("Client: Room created!");
                _roomManager.InitializeRoomView(message.roomsInfo[0], true);
                break;
            case ClientRoomOperation.Joined:
                _roomManager.InitializeRoomView(message.roomsInfo[0], false);
                break;
            case ClientRoomOperation.Update:
                _roomManager.UpdateRoomView(message.roomsInfo[0], message.playerInfos);
                break;
            case ClientRoomOperation.Left:
                _roomManager.OnClientLeaveRoom();
                break;
        }
    }

    [ClientCallback]
    public void OnClientPlayerMessageRecieved(ClientPlayerMessage message)
    {
        switch (message.operation)
        {
            case ClientPlayerOperation.Added:
                Debug.Log("Client: Player added on server");
                PlayerManager.AddPlayerOnClient(message.player);
                _canvasController.OnPlayerAdded(message.player);
                break;
            
        }
    }
    
    [ClientCallback]
    public void SendOnlineMatchMessage(GameMode mode, GridTier tier, ServerMatchOperation operation)
    {
        NetworkClient.Send(
            new ServerMatchMessage 
            { 
                serverSideOperation = operation, 
                matchInfo = new OnlineMatchInfo 
                { 
                    mode = mode, 
                    gridTier = tier 
                } 
            }
        );
    }

    [ClientCallback]
    public void OnClientStartMatch(PlayerInfo[] playersInfo)
    {
        _canvasController.ShowStartScreen(true, playersInfo);

    }

    [ClientCallback]
    public void SendRoomMessageToServer(ServerRoomOperation op, Guid roomID)
    {
        Debug.Log($"Sending message to server: {op}");
        NetworkClient.Send(new ServerRoomMessage { operation = op, roomID = roomID });

    }

    [ClientCallback]
    public void SendPlayerHandleMessage(Lobby lobby, PlayerHandlingOperation op)
    {
        NetworkClient.Send(
            new PlayerHandlingMessage
            { 
                lobbyType = lobby,
                operation = op
            });
    }

    #endregion


}
