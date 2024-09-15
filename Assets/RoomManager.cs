using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TicTacToe;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }



    #region References
    [SerializeField] CanvasController _canvasController;
    [SerializeField] MessageHandler _messageHandler;

    // Room UI References
    [SerializeField] GameObject _roomCardPrefab;
    [SerializeField] GameObject _noRoomScreen;
    [SerializeField] GameObject _roomContentContainer;
    #endregion

    #region Variables
    Dictionary<Guid, Room> _openRooms = new Dictionary<Guid, Room>();
    Dictionary<NetworkConnectionToClient, Guid> _playersRoom = new Dictionary<NetworkConnectionToClient, Guid>();
    Dictionary<Guid, HashSet<NetworkConnectionToClient>> _roomConnections = new Dictionary<Guid, HashSet<NetworkConnectionToClient>>();

    HashSet<NetworkConnectionToClient> _subscribedRoomInfoClients = new HashSet<NetworkConnectionToClient>();
    #endregion

    #region ServerCallbacks

    [ServerCallback]
    public static void AddPlayerToLobby(NetworkConnectionToClient clientConnection)
    {
        Instance._subscribedRoomInfoClients.Add(clientConnection);
        Instance.SendClientList(clientConnection);
    }

    [ServerCallback]
    public void OnServerCreateRoom(NetworkConnectionToClient requestingClient)
    {
        Debug.Log("Passing through checks: server side");
        if(requestingClient == null || _playersRoom.ContainsKey(requestingClient)) return;

        Debug.Log("Successfully passed checks: creating room");
        Player requestingPlayer = PlayerManager.GetPlayerFromConnection(requestingClient);

        Guid roomID = Guid.NewGuid();
        Room newRoom = new Room { 
            roomId = roomID, 
            gameMode = GameMode.Classic, 
            gridSize = 3, 
            roomName = requestingPlayer.name,
            roomOwner = requestingPlayer.playerid,
            currentPlayerCount = 1, 
            totalPlayersAllowed = 2,
        };

        // add rooms to their respective slots
        _openRooms.Add(roomID, newRoom);
        _playersRoom.Add(requestingClient, roomID);
        _roomConnections.Add(roomID, new HashSet<NetworkConnectionToClient>());
        _roomConnections[roomID].Add(requestingClient);

        Room[] rooms = new Room[1];
        rooms[0] = newRoom;

        requestingClient.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.Created, roomsInfo = rooms });
        SendClientList();
    }

    [ServerCallback]
    public void SendClientList(NetworkConnectionToClient connection = null)
    {
        Room[] rooms = _openRooms.Select(key => key.Value).ToArray();
        if (rooms.Length == 0)
            return;

        if (connection != null)
            connection.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.RefreshList, roomsInfo = rooms });
        else
            foreach(var subsciber in _subscribedRoomInfoClients)
                subsciber.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.RefreshList, roomsInfo = rooms });
    }
    #endregion

    #region ClientCallbacks

    [ClientCallback]
    public void RecieveRoomListing(Room[] rooms)
    {
        _openRooms.Clear();

        foreach (var room in rooms)
            _openRooms.Add(room.roomId, room);

        RefreshRoomListings(rooms);
    }

    [ClientCallback]
    public void OnClientCreateRoom()
    {
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.Create);
    }

    #endregion

    #region UI Functions

    [ClientCallback]
    public void InitializeRoomListing()
    {
        if(_roomContentContainer.transform.childCount > 0)
            foreach (var card in _roomContentContainer.GetComponentsInChildren<GameObject>())
                Destroy(card);

        _noRoomScreen.SetActive(true);
    }

    [ClientCallback]
    public void RefreshRoomListings(Room[] rooms)
    {
        if(_roomContentContainer.transform.childCount != 0)
            foreach (var card in _roomContentContainer.GetComponentsInChildren<Transform>())
                Destroy(card.gameObject);

        if (rooms != null && rooms.Length > 0)
        {
            _noRoomScreen.SetActive(false);

            foreach (var room in rooms)
            {
                GameObject card = Instantiate(_roomCardPrefab);
                card.transform.SetParent(_roomContentContainer.transform, false);

                RoomCardScript script = card.GetComponent<RoomCardScript>();
                script.stats = room;
                script.SetupUI();
            }
        }
        else
            _noRoomScreen.SetActive(true);
    }
    #endregion

    #region Unity Callbacks
    void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(Instance);
        else
            Instance = this;
    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion


}
