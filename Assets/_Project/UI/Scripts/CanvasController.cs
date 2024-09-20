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
        
        [SerializeField] MessageHandler _messageHandler;
        [SerializeField] RoomManager _roomManager;

        #region online lobby reference & vars

        // UI References
        [SerializeField]
        GameObject _topBar;
        [SerializeField]
        GameObject _bottomBar;

        [SerializeField]
        List<ScreenStructure<OnlineScreens>> _onlineScreenList = new List<ScreenStructure<OnlineScreens>>();
        ScreenStructure<OnlineScreens> _currentActiveScreen;    

        [SerializeField]
        Toggle _lowTierToggle;
        [SerializeField]    
        Toggle _highTierToggle;
        [SerializeField]
        GameObject _playerBanner;
        [SerializeField] TextMeshProUGUI _userName;

        [SerializeField]
        TextMeshProUGUI _gameModeText;

        // Room UI References
        [SerializeField]
        GameObject _roomCardPrefab;
        [SerializeField]
        GameObject _noRoomScreen;
        [SerializeField]
        GameObject _roomContentContainer;

        // Game Settings
        GameMode _gameMode;
        GridTier _gridTier;

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

            _lowTierToggle.isOn = false;
            _highTierToggle.isOn = false;
            

            ShowScreen(OnlineScreens.OnlineLobby);
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

        #region Online Lobby Button Calls

        public void OnOnlineLobbySelected()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.QuickLobby, PlayerHandlingOperation.AddToLobby);

            InitializeGameMode();
        }

        public void OnRoomOptionSelected()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.RoomLobby, PlayerHandlingOperation.AddToLobby);
            _roomManager.InitializeRoomListing();
            ShowScreenIntegerNumbering(7);
        }

        public void OnModeSelect(int mode)
        {
            if (mode != (int)GameMode.None)
            {
                Debug.Log($"Mode Chosen: {mode}");
                _gameMode = (GameMode)mode;
                _gameModeText.text = _gameMode.ToString();
                ShowScreen(OnlineScreens.GridTier);
            }
        }

        public void OnLowTierToggled()
        {
            if(_lowTierToggle.isOn)
            {
                Debug.Log($"Tier Chosen: Low Grid Tier");
                _gridTier = GridTier.Low;
            }
        }

        public void OnHighTierToggled()
        {
            if (_highTierToggle.isOn)
            {
                Debug.Log($"Tier Chosen: High Grid Tier");
                _gridTier = GridTier.High;
            }
        }

        public void OnStartGame()
        {
            if (_gameMode != GameMode.None && _gridTier != GridTier.None)
            {
                Debug.Log($"Starting match with settings =  Mode: {_gameMode}  Tier: {_gridTier} ");
                _messageHandler.SendOnlineMatchMessage(_gameMode, _gridTier, ServerMatchOperation.Matchmaking);
            }
            else
                Debug.Log("All options not selected!");
        }

        public void OnCancelSearchingForMatch()
        {
            _messageHandler.SendOnlineMatchMessage(GameMode.None, GridTier.None, ServerMatchOperation.CancelMatchmaking);
        }

        public void ShowSearchingForMatchScreen()
        {
            ShowScreen(OnlineScreens.MatchSearching);
        }

        public void OnQuitLobby()
        {
            _messageHandler.SendPlayerHandleMessage(Lobby.None, PlayerHandlingOperation.RemoveFromLobby);
            ShowScreen(OnlineScreens.OnlineLobby);
        }


        #endregion

        #region Mirror:NetworkManager Callbacks

        public void InitiateConnectionFromClient()
        {
            ShowScreen(OnlineScreens.Connecting);
        }

        public void OnClientConnect()
        {
            InitializeCanvasOnline();
        }

        public void OnClientDisconnect()
        {
            InitializeCanvasOffline();
        }

        #endregion

        #region Test Functions
        public void ShowStartScreen(bool show, PlayerInfo[] playersInfo = null)
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
                    GameObject obj = Instantiate(_playerBanner);
                    obj.transform.SetParent(playerBannerContainer, false);
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

