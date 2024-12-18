using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe
{
    public class SlotScript : MonoBehaviour
    {
        public static bool OwnersRoom;
        public static int invalidPlayers = 0;

        PlayerStruct _player;
        int _slotNumber;
        bool _playerAssigned = false;
        bool _isRoomOwner = false;
        bool _isSlotValid = true;
        bool _listingOnly = false;

        // player banner references
        [SerializeField] GameObject _playerBanner;
        [SerializeField] Image _playerAvatar;
        [SerializeField] TextMeshProUGUI _playerName;
        [SerializeField] GameObject _roomOwner;
        [SerializeField] GameObject _playerNotAvailablePanel;

        // other slots references
        [SerializeField] TextMeshProUGUI _slotNumberText;
        [SerializeField] GameObject _addBotButton;
        [SerializeField] GameObject _notAvailablePanel;

        [SerializeField] Button _kickPlayerButton;

        public static void ResetStaticStats()
        {
            invalidPlayers = 0;
        }

        public void InitializeSlot(int slotNumber, bool isSlotValid, bool isListingOnly)
        {
            _slotNumber = slotNumber;
            _isSlotValid = isSlotValid;
            _listingOnly = isListingOnly;

            _playerBanner.SetActive(false);
            _addBotButton.SetActive(false);

            _slotNumberText.text = $"Slot {slotNumber}";
            _notAvailablePanel.SetActive(!isSlotValid);
        }

        public void AddPlayer(PlayerStruct player, bool isRoomOwner, Action<Guid> onKickPlayer = null)
        {
            if(!_isSlotValid)
                invalidPlayers++;

            _player = player;
            _playerAssigned = true;
            _isRoomOwner = isRoomOwner;

            if(OwnersRoom && (onKickPlayer != null))
            {
                _kickPlayerButton.onClick.RemoveAllListeners();
                _kickPlayerButton.onClick.AddListener(() => { onKickPlayer(_player.playerid); });
            }

            UpdatePlayerUI();
        }

        public void UpdatePlayerUI()
        {
            Debug.Log("Client: (SlotScript) Adding player");
            _playerName.text = _player.name;
            _roomOwner.SetActive(_isRoomOwner);

            _playerBanner.SetActive(true);
            _playerNotAvailablePanel.SetActive(!_isSlotValid);
            _notAvailablePanel.SetActive(false);

            _kickPlayerButton.gameObject.SetActive(OwnersRoom && !_isRoomOwner && !_listingOnly);
            Debug.Log("Client: (SlotScript) Added player");
        }

        public void MakePlayerInvalid()
        {
            _playerNotAvailablePanel.SetActive(false);
        }


        
    }
}

