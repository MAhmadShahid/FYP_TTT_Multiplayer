using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    Color selectedColor = new Color(195, 121, 0);

    #region References
    [SerializeField] CanvasController _canvasController;
    [SerializeField] MessageHandler _messageHandler;

    // Room Listing UI References
    [SerializeField] Toggle _hideRoomToggle;
    [SerializeField] TMP_Dropdown _gameModeDrop, _gridSizeDrop;
    [SerializeField] GameObject _roomCardPrefab, _noRoomScreen, _roomContentContainer, _joinButton;


    [SerializeField] ToggleGroup _toggleGroup;
    List<Toggle> _toggles = new List<Toggle>();

    // Room View References
    [SerializeField] GameObject _roomSlotPrefab; 
    [SerializeField] Transform _slotGridContainer;
    [SerializeField] TextMeshProUGUI _roomTitle;

    [SerializeField] TextMeshProUGUI _gameMode, _gridSize, _participants;
    [SerializeField] Button _gameModeButton, _gridSizeButton, _participantsButton;
    [SerializeField] List<GameObject> arrows = new List<GameObject>();

    [SerializeField] GameModeSelectionScript _gameModeSelection;
    [SerializeField] AdvanceSettingsScript _advancedSettings;

    [SerializeField] Button _startGameButton;
    #endregion

    #region Variables
    Room _localRoom;

    Dictionary<Guid, Room> _openRooms = new Dictionary<Guid, Room>();
    Dictionary<NetworkConnectionToClient, Guid> _playersRoom = new Dictionary<NetworkConnectionToClient, Guid>();
    Dictionary<Guid, HashSet<NetworkConnectionToClient>> _roomConnections = new Dictionary<Guid, HashSet<NetworkConnectionToClient>>();

    HashSet<NetworkConnectionToClient> _subscribedRoomInfoClients = new HashSet<NetworkConnectionToClient>();


    // Room Listing UI Variables
    Toggle _currentActiveToggle = null;
    bool _hideFullRoomFilter = false;
    int _currentGameModeFilter = 0, _currentGridSizeFilter = 0;
    int[] gridSizeMapping = { 0, 3, 5, 7, 9 };

    // Room View UI Variables
    bool _isRoomOwner = false;

    GameMode _currentGameMode; int _currentGridSize; int _currentParticipantsCount;

    SlotScript[] _slots = new SlotScript[9];


    #endregion

    #region Mirror Callbacks

    [ClientCallback]
    public void OnClientDisconnect()
    {
        OnClientLeaveRoom();
    }
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
    public void OnServerAddPlayerToRoom(Guid roomID, NetworkConnectionToClient clientConnection)
    {
        if (!_openRooms.ContainsKey(roomID))
            return;

        Room roomToJoin = _openRooms[roomID];
        if (roomToJoin.currentPlayerCount >= roomToJoin.totalPlayersAllowed)
        {
            Debug.Log($"Player requested to join full room. Room = {roomID}; Connection = {clientConnection}");
            return;
        }

        roomToJoin.currentPlayerCount++;
        _openRooms[roomID] = roomToJoin;

        _roomConnections[roomID].Add(clientConnection);

        clientConnection.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.Joined, roomsInfo = new Room[] { roomToJoin } });

        SendUpdatedRoomInfo(roomID);    
        SendClientList();
    }

    [ServerCallback]
    public void OnServerRemovePlayerFromRoom(NetworkConnectionToClient clientConnection)
    {

        // if player leaving is the room owner
        if (_playersRoom.ContainsKey(clientConnection))
        {
            
            Room currentRoom = _openRooms[_playersRoom[clientConnection]];
            var participants = _roomConnections[currentRoom.roomId];
            Debug.Log($"Removing room owner: {currentRoom.roomOwner} from {currentRoom.roomName}");

            // clear up data structures
            _openRooms.Remove(_playersRoom[clientConnection]);
            _playersRoom.Remove(clientConnection);
            _roomConnections.Remove(currentRoom.roomId);

            foreach(var participant in participants)
            {
                participant.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.Left });
            }

            SendClientList();
        }

        // if player is a participant in another player's room
        foreach (var room in _roomConnections)
            if (room.Value.Contains(clientConnection))
            {
                Room currentRoom = _openRooms[room.Key];
                Debug.Log($"Removing Player; Room found: {currentRoom.roomName}");

                currentRoom.currentPlayerCount--;
                _openRooms[currentRoom.roomId] = currentRoom;
                room.Value.Remove(clientConnection);

                // tell leaving client that they have been removed
                clientConnection.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.Left });

                // update players in room view
                SendUpdatedRoomInfo(currentRoom.roomId);

                SendClientList();
            }
    }

    [ServerCallback]
    public void OnServerKickPlayer(NetworkConnectionToClient roomOwner, Guid[] playerID = null, bool kickAll = false)
    {
        if(!_playersRoom.ContainsKey(roomOwner))
        {
            Debug.LogWarning("Kick: someone other than room owner sent kicking request");
            return;
        }

        foreach (var player in playerID)
        {
            NetworkConnectionToClient connectionForPlayer = PlayerManager.GetClientConnectionFromPlayerID(player);
            
            if (_roomConnections[_playersRoom[roomOwner]].Contains(connectionForPlayer))
                OnServerRemovePlayerFromRoom(connectionForPlayer);
        }

    }

    [ServerCallback]
    public void SendClientList(NetworkConnectionToClient connection = null)
    {
        Room[] rooms = _openRooms.Select(key => key.Value).ToArray();

        if (connection != null)
            connection.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.RefreshList, roomsInfo = rooms });
        else
            foreach(var subsciber in _subscribedRoomInfoClients)
                subsciber.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.RefreshList, roomsInfo = rooms });
    }

    [ServerCallback]
    public void SendUpdatedRoomInfo(Guid roomID)
    {
        List<PlayerStruct> participants = new List<PlayerStruct>();

        foreach(NetworkConnectionToClient client in _roomConnections[roomID])
            participants.Add(PlayerManager.GetPlayerStructureFromConnection(client));

        Room[] roomsInfo = new Room[1];
        roomsInfo[0] = _openRooms[roomID];

        foreach(NetworkConnectionToClient client in _roomConnections[roomID])
            client.Send(new ClientRoomMessage { roomOperation = ClientRoomOperation.Update , roomsInfo = roomsInfo, playerInfos = participants.ToArray() });
    }

    [ServerCallback]
    public void RemovePlayerFromLobby(NetworkConnectionToClient clientConnection, bool disconnection = false)
    {
        _subscribedRoomInfoClients.Remove(clientConnection);
        Debug.Log("Removing: player subscription to room");

        OnServerRemovePlayerFromRoom(clientConnection);
    }

    [ServerCallback]
    public void OnServerChangeRoomSettings(NetworkConnectionToClient clientConnection, ClientRoomSettings settings)
    {
        if(!_playersRoom.ContainsKey(clientConnection))
        {
            Debug.Log("Requesting client doesn't own a room");
            return;
        }

        Guid clientRoomID = _playersRoom[clientConnection];
        Room clientRoom = _openRooms[clientRoomID];

        if (settings.roomMode != GameMode.None)
        {
            clientRoom.gameMode = settings.roomMode;
            Debug.Log($"Server: Changing settings. Mode = {settings.roomMode}");
        }
        else
        {
            if (settings.gridSize != 0 && Configurations.validStages.ContainsKey(settings.gridSize))
            {
                clientRoom.gridSize = settings.gridSize;
                clientRoom.totalPlayersAllowed = Configurations.validStages[clientRoom.gridSize].Keys.First();
                Debug.Log($"Server: Changing settings. Size = {settings.gridSize}");
            }

            if (settings.participants != 0 && Configurations.validStages[clientRoom.gridSize].ContainsKey(settings.participants))
            {
                clientRoom.totalPlayersAllowed = settings.participants;
                Debug.Log($"Server: Changing settings. Participants = {settings.participants}");
            }
        }
        
            

        _openRooms[clientRoomID] = clientRoom;

        SendUpdatedRoomInfo(clientRoomID);
        SendClientList();
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
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.Create, null, Guid.Empty);
    }


    [ClientCallback]
    public void InitializeRoomListing()
    {
        if(_roomContentContainer.transform.childCount > 0)
            for(int childIndex = 0; childIndex < _roomContentContainer.transform.childCount; childIndex++)
                Destroy(_roomContentContainer.transform.GetChild(childIndex).gameObject);

        _toggles.Clear();
        _noRoomScreen.SetActive(true);
    }

    [ClientCallback]
    public void RefreshRoomListingUI(Room[] rooms)
    {
        for(int childIndex = 0; childIndex < _roomContentContainer.transform.childCount; childIndex++)
        {
            Destroy(_roomContentContainer.transform.GetChild(childIndex).gameObject);
        }

        _toggles.Clear();
        ShowRooms(rooms); 
    }


    [ClientCallback]
    public void OnFilterSelected(bool reset)
    {
        if(reset)
        {
            _currentGameModeFilter = 0; _currentGridSizeFilter = 0; _hideFullRoomFilter = false;
            _gameModeDrop.value = 0; _gridSizeDrop.value = 0; _hideRoomToggle.isOn = false;
        }
        else
        {
            _currentGameModeFilter = _gameModeDrop.value;
            _currentGridSizeFilter = _gridSizeDrop.value;
            _hideFullRoomFilter = _hideRoomToggle.isOn;
        }

        FilterRoomListOnGlobalSettings();
    }
    #endregion

    #region Room View
    public void InitializeRoomView(Room room, bool forOwner, PlayerStruct[] participants = null)
    {
        Debug.Log("Client: Initializing Room View");
        Debug.Log($"Client Room Setttings: {room.roomName}, {room.roomId}, {room.gameMode}, {room.gridSize}");
        Debug.Log($"Is room owner? {forOwner}");

        _localRoom = room;
        _isRoomOwner = forOwner;
        SlotScript.OwnersRoom = forOwner;

        for(int slotIndex = 0; slotIndex < 9; slotIndex++)
        {
            _slots[slotIndex] = Instantiate(_roomSlotPrefab, _slotGridContainer).GetComponent<SlotScript>();
        }
        
        _roomTitle.text = $"{room.roomName}'s";
        SetupRoomButtons();
        UpdateRoomView(_localRoom, participants);
        _canvasController.ShowScreen(OnlineScreens.RoomView);

        Debug.Log("Room Initialized");
    }

    [ClientCallback]
    public void SetupRoomButtons()
    {
        ToggleSettingsButtonInteractability(_isRoomOwner);
    }



    [ClientCallback]
    public void OnClientGameModeChanged(GameMode selectedGameMode)
    {
        Debug.Log($"Selected game mode: {selectedGameMode}");
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.SettingChange, null, _localRoom.roomId, new ClientRoomSettings { roomMode = selectedGameMode});
        ButtonStates(true);
        _canvasController.ShowScreen(OnlineScreens.RoomView);       
    }

    [ClientCallback]
    public void OnClientGridOrParticipantChanged(int gridSize, int participants)
    {
        Debug.Log($"Grid Size: {gridSize}, Participants: {participants}");
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.SettingChange, null, _localRoom.roomId, new ClientRoomSettings { gridSize = gridSize, participants = participants });
        ButtonStates(true);
        _canvasController.ShowScreen(OnlineScreens.RoomView);
    }

    public void UpdateRoomView(Room room, PlayerStruct[] participants = null)
    {
        SlotScript.ResetStaticStats();

        _localRoom = room;

        if (_isRoomOwner)
        {
            PlayerStruct localPlayer = PlayerManager.GetLocalPlayerStructure();
            _slots[0].InitializeSlot(1, true);
            _slots[0].AddPlayer(localPlayer, _isRoomOwner);
        }

        int slotCounter = 1;

        if (participants != null)
        {
            for(int playerIndex = 0; playerIndex < participants.Length; playerIndex++)
            {
                if (participants[playerIndex].playerid == room.roomOwner)
                {
                    if(!_isRoomOwner)
                    {
                        _slots[0].InitializeSlot(1, true);
                        _slots[0].AddPlayer(participants[playerIndex], true);
                    }

                    continue;
                }

                _slots[slotCounter].InitializeSlot(slotCounter + 1, slotCounter < room.totalPlayersAllowed);
                _slots[slotCounter++].AddPlayer(participants[playerIndex], false, OnSinglePlayerKick);
            }
        }

        for (; slotCounter < 9; slotCounter++)
            _slots[slotCounter].InitializeSlot(slotCounter + 1, slotCounter < room.totalPlayersAllowed);


        UpdateSettingsUI(_localRoom);

        _startGameButton.enabled = SlotScript.invalidPlayers == 0;
    }

    [ClientCallback]
    public void OnClientKickPlayer(Guid[] playerToKick)
    { 
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.Kick, playerToKick);
    }
    
    public void UpdateSettingsUI(Room room)
    {
        _gameMode.text = room.gameMode.ToString();
        _gridSize.text = room.gridSize.ToString();
        _participants.text = $"{room.totalPlayersAllowed}";

        ButtonStates(false);
    }

    public void ButtonStates(bool waitingForServer)
    {
        if (!_isRoomOwner)
            return;

        if(waitingForServer)
        {
            ToggleSettingsButtonInteractability(false);
            _gameMode.text = "---"; _gridSize.text = "---"; _participants.text = "---";
        }
        else
            ToggleSettingsButtonInteractability(true);
    }

    // keep in mind for both room owner and other participants
    [ClientCallback]
    public void OnClientLeaveRoom()
    {
        // reset room view
        _localRoom = new Room();
        _isRoomOwner = false;

        foreach (var slot in _slots)
            Destroy(slot.gameObject);

        _canvasController.ShowScreen(OnlineScreens.RoomListing);
    }
    #endregion

    #region UI Button Clickes
    public void OnGameModeSettingClicked()
    {
        Debug.Log("Game mode button pressed!");
        _gameModeSelection.InitializeGameModeSelection(OnClientGameModeChanged, OnlineScreens.RoomView);
    }

    public void OnSinglePlayerKick(Guid playerID)
    {
        Guid[] playerIds = new Guid[] { playerID };
        OnClientKickPlayer(playerIds);
    }

    public void OnGridOrParticipantSettingClicked()
    {
        Debug.Log("Grid Size & Participants clicked");
        _advancedSettings.IntializeAdvanceSettings(
            OnClientGridOrParticipantChanged, 
            _localRoom.gameMode, 
            OnlineScreens.RoomView, 
            _localRoom.gridSize, 
            _localRoom.totalPlayersAllowed
        );
    }

    public void OnClientJoinButtonPressed()
    {
        Debug.Log($"Joining Room: {_localRoom.roomName}");
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.Join, null, _localRoom.roomId);
    }

    public void OnClientLeaveButtonPressed()
    {
        _messageHandler.SendRoomMessageToServer(ServerRoomOperation.Leave, null, _localRoom.roomId);
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

        if(_hideFullRoomFilter)
            rooms = rooms.Where(x => x.currentPlayerCount < x.totalPlayersAllowed);

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

                Toggle toggle = card.GetComponentInChildren<Toggle>();
                toggle.group = _toggleGroup;
                toggle.onValueChanged.AddListener((bool pressed) =>
                {
                    if (pressed)
                        _localRoom = room;

                    toggle.transform.GetComponent<Image>().color = pressed ? selectedColor : Color.white;
                    _joinButton.SetActive(pressed);
                });

                RoomCardScript script = card.GetComponent<RoomCardScript>();
                script.stats = room;
                script.SetupUI();
            }
        }
        else
            _noRoomScreen.SetActive(true);
    }

    public void ToggleSettingsButtonInteractability(bool interactable)
    {
        Debug.Log($"Setting button interactability: {interactable}");
        _gameModeButton.interactable = interactable; _gridSizeButton.interactable = interactable; _participantsButton.interactable = interactable;
        foreach (var arrow in arrows) arrow.SetActive(interactable);
    }

    public void Print(string text)
        { Debug.Log(text); }
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
