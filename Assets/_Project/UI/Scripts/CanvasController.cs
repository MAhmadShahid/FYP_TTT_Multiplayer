using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

namespace TicTacToe
{
    public class CanvasController : MonoBehaviour
    {
        public bool uiTesting;

        [SerializeField] MyNetworkManager myNetworkManager;
        [SerializeField] MessageHandler _messageHandler;
        [SerializeField] RoomManager _roomManager;

        #region online lobby reference & vars


        // Game Settings
        GameMode _gameMode;
        GridTier _gridTier;

        // Variables
        [SerializeField]
        List<ScreenStructure<OnlineScreens>> _onlineScreenList = new List<ScreenStructure<OnlineScreens>>();
        ScreenStructure<OnlineScreens> _currentActiveScreen;

        [Header("General UI References")]
        [SerializeField] GameObject _topBar;
        [SerializeField] GameObject _bottomBar;
        [SerializeField] TextMeshProUGUI _userName;
        [SerializeField] GameModeSelectionScript _modeSelection;
        [SerializeField] TierSelectionScript _tierSelection;
        [SerializeField] AdvanceSettingsScript _advancedSettings;

        [Header("Testing Prefabs")]
        [SerializeField] GameObject _playerSlot;
        [SerializeField] Button _cancelSearchingButton;

        #endregion




        #region Helping Functions
        public void InitializeCanvasOffline()
        {
            gameObject.SetActive(true);
            ShowScreen(OnlineScreens.Main);
        }

        public void InitializeCanvasOnline()
        {
            // resetting all states
            _gameMode = GameMode.None;
            _gridTier = GridTier.None;

            ShowScreen(OnlineScreens.OnlineLobby);
        }

        public void InitializeCanvasOfflineNew()
        {
            // resetting all states
            _gameMode = GameMode.None;
            _gridTier = GridTier.None;

            ShowScreen(OnlineScreens.OfflineLobby);
        }

        public void ShowScreen(OnlineScreens screenName)
        {
                _onlineScreenList.ForEach(x =>
                {
                    if(x.Screen == screenName)
                    {
                        x.screenObject.SetActive(true);
                        _currentActiveScreen = x;

                        _topBar.SetActive(x.showTopBar);
                        _bottomBar.SetActive(x.showBottomBar);
                    }
                    else
                        x.screenObject.SetActive(x.Screen == screenName);
                    
                });

        }

        public void ShowScreenIntegerNumbering(int mode_integer)
        {
            ShowScreen((OnlineScreens)mode_integer);    
        }

        IEnumerator MyCoroutine(Action action, float delay)
        {
            // Execute your lambda
            yield return new WaitForSeconds(delay);
            action();
            
        }

        public void InitializeGameMode()
        {
            _gameMode = GameMode.None;
            _gridTier = GridTier.None;
            
            ShowScreen(OnlineScreens.GameMode);
        }

        public void OnPlayerAdded(PlayerStruct player)
        {
            _userName.text = player.name;
        }

        #endregion
        public void OnStartMatchClient()
        {
            
        }

        #region Online Lobby Button Calls

        public void OnOnlineLobbySelected()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.QuickLobby, PlayerHandlingOperation.AddToLobby);

            _modeSelection.InitializeGameModeSelection(OnModeSelected, OnQuitLobby);
        }

        public void OnRoomOptionSelected()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.RoomLobby, PlayerHandlingOperation.AddToLobby);
            _roomManager.InitializeRoomListing();
            ShowScreenIntegerNumbering(7);
        }

        public void OnModeSelected(GameMode mode)
        {
            if (mode != GameMode.None)
            {
                Debug.Log($"Mode Chosen: {mode}");
                _gameMode = mode;

                _tierSelection.InitializeTierSelection(OnStartGame, OnlineScreens.GameMode, _gameMode);
                ShowScreen(OnlineScreens.GridTier);
            }
        }

        public void OnStartGame(GridTier tier)
        {
            _gridTier = tier;

            if (_gameMode != GameMode.None && _gridTier != GridTier.None)
            {
                Debug.Log($"Starting match with settings =  Mode: {_gameMode}  Tier: {_gridTier} ");
                _messageHandler.SendOnlineMatchMessage(_gameMode, _gridTier, ServerMatchOperation.Matchmaking);
            }
            else
                Debug.Log("All options not selected!");
        }


        public void ShowSearchingForMatchScreen()
        {
            ShowScreen(OnlineScreens.MatchSearching);
            _cancelSearchingButton.gameObject.SetActive(true);
        }

        public void OnQuitLobby()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.None, PlayerHandlingOperation.RemoveFromLobby);
        }

        public void OnClientCancelSearchingForMatch()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.QuickLobby, PlayerHandlingOperation.RemoveFromLobby);
        }

        #endregion

        #region Offline Lobby Screen

        public void OnStartOfflineGame(int gridSize, int participants)
        {
            Debug.Log($"Starting offline game, {gridSize} | {participants}");
        }

        public void OnClassicModeSelected()
        {
            _gameMode = GameMode.Classic;
            _advancedSettings.IntializeAdvanceSettings(
                OnStartOfflineGame,
                _gameMode,
                OnlineScreens.OfflineLobby
            );
        }

        public void OnBlitzModeSelected()
        {
            _gameMode = GameMode.Blitz;

            _advancedSettings.IntializeAdvanceSettings(
                OnStartOfflineGame,
                _gameMode,
                OnlineScreens.OfflineLobby
            ); 
        }


        #endregion

        public void PlayerRemovedFromLobby()
        {
            InitializeCanvasOnline();
        }

        #region Mirror:NetworkManager Callbacks

        public void InitiateConnectionFromClient()
        {
            ShowScreen(OnlineScreens.Connecting);
        }

        public void OnClientConnect()
        {
            if(myNetworkManager.connectionMode == ConnectionMode.Online)
                InitializeCanvasOnline();
            else
                InitializeCanvasOfflineNew();
        }

        public void OnClientDisconnect()
        {
            InitializeCanvasOffline();
        }

        #endregion

        #region Test Functions
        public void ShowStartScreen(bool show, PlayerStruct[] playersInfo = null)
        {
            ScreenStructure<OnlineScreens> startScreenStructure = _onlineScreenList.Where(x => x.Screen == OnlineScreens.Start).FirstOrDefault();
            
            if (!show)
            {
                foreach (var transform in startScreenStructure.screenObject.GetComponentsInChildren<Transform>())
                    Destroy(transform.gameObject);

                ShowScreen(OnlineScreens.GameMode);
            }
            else
            {
                ShowScreen(OnlineScreens.Start);
                var playerBannerContainer = startScreenStructure.screenObject.GetComponentInChildren<GridLayoutGroup>().transform;

                foreach (var info in playersInfo)
                {
                    GameObject obj = Instantiate(_playerSlot, playerBannerContainer);
                    SlotScript slot = obj.GetComponent<SlotScript>();
                    slot.InitializeSlot(0, true, true);
                    slot.AddPlayer(info, false);
                }
            }

            StartCoroutine(MyCoroutine(() => { _currentActiveScreen.screenObject.SetActive(false); }, 5));
        }
        #endregion

        #region Unity Callbacks
        private void Start()
        {
            if(!uiTesting)
                InitializeCanvasOffline();
        }
        #endregion

    }

}

