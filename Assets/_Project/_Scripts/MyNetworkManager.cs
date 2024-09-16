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
    [SerializeField]
    NetworkManagerHUD _hud;
    [SerializeField]
    bool _distributionUnit = false;

    #region Start & Stop Callbacks

    public override void Awake()
    {
#if UNITY_EDITOR
        networkAddress = "localhost";
        _hud.enabled = true;
#else
        if(_distributionUnit)
            networkAddress = "103.31.104.181";
        else
            networkAddress = "192.168.18.200";
        _hud.enabled = false;
#endif
        base.Awake();
    }

    public override void Start()
    {
        base.Start();
#if UNITY_EDITOR
#else
        if (mode != NetworkManagerMode.ServerOnly)
            StartClient();
#endif
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _lobbyManager.OnStartServer();
        _messageHandler.RegisterAllServerHandlers();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        PlayerManager.OnPlayerConnect(conn);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _messageHandler.RegisterAllClientHandlers();
        _messageHandler.SendPlayerHandleMessage(Lobby.None, PlayerHandlingOperation.RequestPlayerInfo);
        _canvasController.OnStartClient();
    }

#endregion
}
