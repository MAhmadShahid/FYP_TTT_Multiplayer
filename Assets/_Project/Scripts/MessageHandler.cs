using Mirror;
using UnityEngine;
using TicTacToe;

public class MessageHandler : MonoBehaviour
{
    [SerializeField]
    CanvasController _canvasController;
    [SerializeField]
    LobbyManager _lobbyManager;

    #region Server
    [ServerCallback]
    public void RegisterAllServerHandlers()
    {
        NetworkServer.RegisterHandler<ServerMatchMessage>(OnServerMatchMessage);
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
    public void OnServerMatchmaking(NetworkConnectionToClient connection, ServerMatchMessage message)
    {
        Debug.Log($"Player Connection: {connection} \n Player Mode Pref: {message.matchInfo.mode}");
        Debug.Log($"Player Tier Pref: {message.matchInfo.gridTier}");

        _lobbyManager.AddPlayerToLobby(connection, message.matchInfo);
        connection.Send(new ClientMatchMessage { clientSideOperation = ClientMatchOperation.Matchmaking });
    }

    #endregion

    #region Client

    [ClientCallback]
    public void RegisterAllClientHandlers()
    {
        NetworkClient.RegisterHandler<ClientMatchMessage>(OnClientMatchMessageReceived);
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
                    _canvasController.InitializeCanvas();
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
    #endregion
}
