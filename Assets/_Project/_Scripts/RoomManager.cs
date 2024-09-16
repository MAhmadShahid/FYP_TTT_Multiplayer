using Mirror;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TicTacToe;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }



    #region References
    [SerializeField] CanvasController _canvasController;
    [SerializeField] MessageHandler _messageHandler;

    // Room Listing UI References
    [SerializeField] TMP_Dropdown _gameModeDrop, _gridSizeDrop;
    [SerializeField] GameObject _roomCardPrefab;
    [SerializeField] GameObject _noRoomScreen;
    [SerializeField] GameObject _roomContentContainer;

    // Room View References
    [SerializeField] GameObject _roomSlotPrefab; 
    [SerializeField] Transform _slotGridContainer;
    [SerializeField] TextMeshProUGUI _roomTitle;
    #endregion

    #region Variables
    Room _localRoom;

    Dictionary<Guid, Room> _openRooms = new Dictionary<Guid, Room>();
    Dictionary<NetworkConnectionToClient, Guid> _playersRoom = new Dictionary<NetworkConnectionToClient, Guid>();
    Dictionary<Guid, HashSet<NetworkConnectionToClient>> _roomConnections = new Dictionary<Guid, HashSet<NetworkConnectionToClient>>();

    HashSet<NetworkConnectionToClient> _subscribedRoomInfoClients = new HashSet<NetworkConnectionToClient>();
    

    // Room Listing UI Variables
    int _currentGameModeFilter = 0, _currentGridSizeFilter = 0;
    int[] gridSizeMapping = { 0, 3, 5, 7, 9 };

    // Room View UI Variables
    bool _isRoomOwner = false;
    GameObject[] _slots = new GameObject[9];

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

        RefreshRoomListingUI(rooms);
    }
    #endregion

    #region UI Functions

    #region UI Listing View
    [ClientCallback]
    public void OnClientCreateRoom()
    {
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.Create);
    }


    [ClientCallback]
    public void InitializeRoomListing()
    {
        if(_roomContentContainer.transform.childCount > 0)
            foreach (var card in _roomContentContainer.GetComponentsInChildren<GameObject>())
                Destroy(card);

        _noRoomScreen.SetActive(true);
    }

    [ClientCallback]
    public void RefreshRoomListingUI(Room[] rooms)
    {
        for(int childIndex = 0; childIndex < _roomContentContainer.transform.childCount; childIndex++)
        {
            Destroy(_roomContentContainer.transform.GetChild(childIndex).gameObject);
        }

        ShowRooms(rooms); 
    }

    [ClientCallback]
    public void OnGameModeFilterSelected()
    {
        Debug.Log($"Selected game mode filter: {_gameModeDrop.value}");
        _currentGameModeFilter = _gameModeDrop.value;
        FilterRoomListOnGlobalSettings();
    }

    [ClientCallback]
    public void OnGridSizeFilterSelected()
    {
        Debug.Log($"selected grid size filter: {_gridSizeDrop.value}");
        _currentGridSizeFilter = _gridSizeDrop.value;
        FilterRoomListOnGlobalSettings();
    }
    #endregion

    #region Room View
    public void InitializeRoomView(Room room, bool forOwmer)
    {
        Debug.Log("Client: Initializing Room View");
        Debug.Log($"Client Room Setttings: {room.roomName}, {room.roomId}, {room.gameMode}, {room.gridSize}");
        Debug.Log($"Is room owner? {forOwmer}");

        _localRoom = room;
        _isRoomOwner = forOwmer;
        // initializing all the slots
        for(int slotIndex = 0; slotIndex < 9; slotIndex++)
        {
            _slots[slotIndex] = Instantiate(_roomSlotPrefab, _slotGridContainer);

            SlotScript slotScript = _slots[slotIndex].GetComponent<SlotScript>();
            slotScript.InitializeSlot(slotIndex + 1, slotIndex < room.totalPlayersAllowed);
        }

        if (_isRoomOwner)
        {
            PlayerStruct localPlayer = PlayerManager.GetLocalPlayerStructure();
            _slots[0].GetComponent<SlotScript>().AddPlayer(localPlayer.name, _isRoomOwner);
        }

        Debug.Log("Room should have been created");
        _canvasController.ShowScreen(OnlineScreens.RoomView);
    }
    #endregion

    #endregion

    #region Helping Functions
    public void FilterRoomListOnGlobalSettings()
    {
        Debug.Log($"Filtering On Settings:{_currentGameModeFilter}, {gridSizeMapping[_currentGridSizeFilter]}");

        IEnumerable<Room> rooms = _openRooms.Select(x => x.Value);

        if (_currentGameModeFilter != 0)
            rooms = rooms.Where(x => (int)x.gameMode == _currentGameModeFilter);

        if (_currentGridSizeFilter != 0)
            rooms = rooms.Where(x => x.gridSize == gridSizeMapping[_currentGridSizeFilter]);

        RefreshRoomListingUI(rooms.ToArray());
    }

    public void ShowRooms(Room[] rooms)
    {
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
