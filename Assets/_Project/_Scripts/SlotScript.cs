using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe
{
    public class SlotScript : MonoBehaviour
    {
        PlayerStruct _player;
        bool _playerAssigned = false;
        bool _isRoomOwner = false;

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

        public void InitializeSlot(int slotNumber, bool isSlotValid)
        {
            _playerBanner.SetActive(false);
            _addBotButton.SetActive(false);

            _slotNumberText.text = $"Slot {slotNumber}";
            _notAvailablePanel.SetActive(!isSlotValid);
        }

        public void AddPlayer(PlayerStruct player, bool isRoomOwner)
        {
            _player = player;
            _playerAssigned = true;
            _isRoomOwner = isRoomOwner;

            UpdatePlayerUI();
        }

        public void UpdatePlayerUI()
        {
            Debug.Log("Client: (SlotScript) Adding player");
            _playerName.text = _player.name;
            _roomOwner.SetActive(_isRoomOwner);

            _playerBanner.SetActive(true);
            _playerNotAvailablePanel.SetActive(false);
            Debug.Log("Client: (SlotScript) Added player");
        }

        public void MakePlayerInvalid()
        {
            _playerNotAvailablePanel.SetActive(false);
        }
        
    }
}

