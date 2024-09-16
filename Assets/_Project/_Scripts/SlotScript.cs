using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe
{
    public class SlotScript : MonoBehaviour
    {
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

        public void AddPlayer(string playerName, bool isRoomOwner)
        {
            Debug.Log("Client: (SlotScript) Adding player");
            _playerName.text = playerName;
            _roomOwner.SetActive(isRoomOwner);

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

