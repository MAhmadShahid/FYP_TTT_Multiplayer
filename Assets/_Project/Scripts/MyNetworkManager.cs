using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TicTacToe;

public class MyNetworkManager : NetworkManager
{
    [SerializeField]
    MessageHandler _messageHandler;
    [SerializeField]
    CanvasController _canvasController;
    [SerializeField]
    LobbyManager _lobbyManager;

    #region Start & Stop Callbacks
    public override void OnStartServer()
    {
        base.OnStartServer();
        _lobbyManager.OnStartServer();
        _messageHandler.RegisterAllServerHandlers();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _messageHandler.RegisterAllClientHandlers();
        _canvasController.OnStartClient();
    }

    #endregion
}
