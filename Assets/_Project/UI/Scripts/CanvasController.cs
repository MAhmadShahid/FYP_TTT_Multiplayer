using Org.BouncyCastle.Asn1.Mozilla;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TicTacToe;
using System.Linq;

namespace TicTacToe
{
    public class CanvasController : MonoBehaviour
    {
        [SerializeField]
        MessageHandler _messageHandler;

        #region online lobby reference & vars

        // UI References
        [SerializeField]
        List<ScreenStructure<OnlineLobbyScreens>> _onlineScreenList = new List<ScreenStructure<OnlineLobbyScreens>>();

        [SerializeField]
        Toggle _lowTierToggle;
        [SerializeField]    
        Toggle _highTierToggle;
        [SerializeField]
        GameObject _playerBanner;

        // Game Settings
        GameMode _gameMode;
        GridTier _gridTier;

        #endregion

        public void InitializeCanvas()
        {
            // resetting all states
            _gameMode = GameMode.None;
            _gridTier = GridTier.None;

            _lowTierToggle.isOn = false;
            _highTierToggle.isOn = false;
            

            ShowScreen(OnlineLobbyScreens.GameMode);
        }

        public void ShowScreen(OnlineLobbyScreens screenName)
        {
            _onlineScreenList.ForEach(x => x.screenObject.SetActive(x.Screen == screenName));
        }

        
        

        #region Online Lobby Button Calls
        public void OnModeSelect(int mode)
        {
            if (mode != (int)GameMode.None)
            {
                Debug.Log($"Mode Chosen: {mode}");
                _gameMode = (GameMode)mode;
                ShowScreen(OnlineLobbyScreens.GridTier);
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
            ShowScreen(OnlineLobbyScreens.MatchSearching);
        }

        #endregion

        #region Server Start & Stop
        public void OnStartClient()
        {
            gameObject.SetActive(true);
            InitializeCanvas();
        }

        #endregion

        #region Test Functions
        public void ShowStartScreen(bool show, PlayerInfo[] playersInfo = null)
        {
            ScreenStructure<OnlineLobbyScreens> startScreenStructure = _onlineScreenList.Where(x => x.Screen == OnlineLobbyScreens.Start).FirstOrDefault();
            if (!show)
            {
                foreach (var transform in startScreenStructure.screenObject.GetComponentsInChildren<Transform>())
                    Destroy(transform.gameObject);

                ShowScreen(OnlineLobbyScreens.GameMode);
            }
            else
            {
                ShowScreen(OnlineLobbyScreens.Start);

                foreach (var info in playersInfo)
                {
                    GameObject obj = Instantiate(_playerBanner);
                    obj.transform.SetParent(startScreenStructure.screenObject.transform, false);
                }
            }
        }
        #endregion

    }

}

