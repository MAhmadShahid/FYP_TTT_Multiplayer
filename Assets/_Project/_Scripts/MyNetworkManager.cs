
using UnityEngine;
using Mirror;
using TicTacToe;
using TMPro;
using System.Collections;

public class MyNetworkManager : NetworkManager
{
    [SerializeField] MessageHandler _messageHandler;
    [SerializeField] CanvasController _canvasController;
    [SerializeField] LobbyManager _lobbyManager;
    [SerializeField] NetworkManagerHUD _hud;
    [SerializeField] RoomManager _roomManager;
    
    
    [SerializeField] bool _distributionUnit = false;

    [SerializeField]
    TextMeshProUGUI _addressText;

    public ConnectionMode connectionMode;

    #region Start & Stop Callbacks

    public override void Awake()
    {
        Debug.Log($"Awake Before: Mode = {mode}");
#if UNITY_EDITOR
        networkAddress = "192.168.18.200";
        _hud.enabled = true;
#else
        if(_distributionUnit)
            networkAddress = "103.31.104.181";

        else
            networkAddress = "192.168.18.200";
        _hud.enabled = false;
#endif

        _addressText.text = $"Network Address: {networkAddress} Port: 7778";
        base.Awake();

        Debug.Log($"Awake After: Mode = {mode}");
    }

    public override void Start()
    {
        Debug.Log($"Start Before: Mode = {mode}");
        base.Start();
        Debug.Log($"Start After: Mode = {mode}");
    }



    public override void OnStartServer()
    {
        Debug.Log($"OnStartServer Before: Mode = {mode}");
        base.OnStartServer();
        Debug.Log($"OnStartServer After: Mode = {mode}");
        _lobbyManager.OnStartServer();
        _messageHandler.RegisterAllServerHandlers();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        PlayerManager.OnPlayerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        PlayerManager.OnPlayerDisconnect(conn);
        StartCoroutine(DelayedOnServerDisconnect(conn));
    }

    public IEnumerator DelayedOnServerDisconnect(NetworkConnectionToClient conn)
    {
        yield return new WaitForSeconds(.5f);
        base.OnServerDisconnect(conn);
    }

    public override void OnStartClient()
    {
        Debug.Log($"OnStartClient Before: Mode = {mode}");
        Debug.Log("On client start called");
        base.OnStartClient();
        Debug.Log($"OnStartClient After: Mode = {mode}");
        _messageHandler.RegisterAllClientHandlers();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        _messageHandler.SendPlayerHandleMessage(Lobby.None, PlayerHandlingOperation.RequestPlayerInfo);
        _canvasController.OnClientConnect();
    }

    public void InitiateOnlineConnectionFromClient()
    {
        if (mode == NetworkManagerMode.ServerOnly)
            return;

        networkAddress = "103.31.104.181";
        connectionMode = ConnectionMode.Online;

        StartClient();
        _canvasController.InitiateConnectionFromClient();
    }

    public void InitiateOfflineConnectionFromClient()
    {
        if (mode == NetworkManagerMode.ServerOnly)
            return;

        networkAddress = "localhost";
        connectionMode = ConnectionMode.Local;

        StartHost();
        _canvasController.InitiateConnectionFromClient();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        _roomManager.OnClientDisconnect();
        _canvasController.OnClientDisconnect();
        
        Debug.Log("Disconnected from server");
    }

    #endregion
}
