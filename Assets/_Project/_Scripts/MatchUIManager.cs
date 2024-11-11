using TicTacToe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class MatchUIManager : MonoBehaviour
{
    const string MATCH_MAKED = "MatchmakedScreen", MATCH_UI = "MatchUIScreen";

    [SerializeField] MatchController _matchController;
    
    [Header("GUIReferences")]
    [SerializeField] Canvas _matchCanvas;
    [SerializeField] GameObject _matchUIScreen, _matchmakedScreen, _topPanel;

    [Header("WinnerScreenReferences")]
    [SerializeField] GameObject _winnerScreenObject;
    [SerializeField] Image _winnerAvatar;
    [SerializeField] TextMeshProUGUI _winnerNameText;

    [Header("DrawScreenReferences")]
    [SerializeField] GameObject _drawScreenObject;

    [Header("Matchmaking Screen Variables")]
    [SerializeField] GameObject _playerBannerPrefab;
    [SerializeField] Transform _playerBannerContainer;

    [Header("Match UI Variables")]
    [SerializeField] TextMeshProUGUI _gameModeText;
    [SerializeField] TextMeshProUGUI _gridSizeText;
    [SerializeField] RectTransform _winConditionPrefab,  _linePrefab, _winContainer, _rowContainer;
    [SerializeField] GameObject _yourTurnObject;

    [SerializeField] GameObject _matchPlayerCardPrefab;
    [SerializeField] Transform _currentPlayerContainer, _otherPlayerContainer;
    [SerializeField] List<MatchPlayerCardScript> matchPlayerCardScripts = new List<MatchPlayerCardScript>();

    int _transitionTime = 3;

    public MatchInfo matchInfo;

    public IEnumerator OnStartClient()
    {
        gameObject.SetActive(true);
        ShowScreen(MATCH_MAKED);
        yield return new WaitForSeconds(_transitionTime);
        UtilityClass.LogMessages("Blah blah");
        ShowScreen(MATCH_UI);
    }

    [ClientCallback]
    public void ShowStartScreen(MatchInfo matchInfo)
    {

    }

    // Helping function
    public void ShowScreen(string screenName)
    {
        switch(screenName)
        {
            case MATCH_MAKED:
                _matchUIScreen.SetActive(false);
                _topPanel.SetActive(false);
                
                _matchmakedScreen.SetActive(true);
                AddPlayersToMatchmakedScreen();
                break;
            case MATCH_UI:
                _matchmakedScreen.SetActive(false);

                _topPanel.SetActive(true);
                _matchUIScreen.SetActive(true);
                ResetMatchmakingScreen();
                SetupMatchUI();
                break;
        }
    }

    public void AddPlayersToMatchmakedScreen()
    {
        foreach(var playerKeyValue in _matchController.matchPlayers)
        {
                GameObject obj = Instantiate(_playerBannerPrefab, _playerBannerContainer);
                SlotScript slot = obj.GetComponent<SlotScript>();
                slot.InitializeSlot(0, true, true);
                slot.AddPlayer(playerKeyValue.Value, false);
        }
    }

    public void ResetMatchmakingScreen()
    {
        foreach (var child in _playerBannerContainer.GetComponentsInChildren<Transform>())
            Destroy(child.gameObject);
    }

    public void SetupMatchUI()
    {
        // starting text setup
        _gameModeText.text = $"{matchInfo.mode}";
        _gridSizeText.text = $"{matchInfo.gridSize}x{matchInfo.gridSize}";

        // win condition ui setup
        int winConditionCount = Configurations.validStages[matchInfo.gridSize][matchInfo.playerCount];
        for (int count = 0; count < winConditionCount; count++)
            Instantiate(_winConditionPrefab, _rowContainer);

        RectTransform lineImage = Instantiate(_linePrefab, _winContainer);
        lineImage.sizeDelta = new Vector2((winConditionCount * 75) + 125, lineImage.sizeDelta.y);
        
        // current player card
        var currentPlayerCard = Instantiate(_matchPlayerCardPrefab, _currentPlayerContainer);
        RectTransform currentPlayerCardTransform = currentPlayerCard.GetComponent<RectTransform>();
        //currentPlayerCardTransform.sizeDelta = new Vector2(100, 100);
        currentPlayerCardTransform.localPosition = new Vector2(-346, 0);
        currentPlayerCardTransform.localScale = new Vector3(1.5f, 1.5f, 1);
        matchPlayerCardScripts.Add(currentPlayerCard.GetComponent<MatchPlayerCardScript>());
        
        for(int count = 0; count < matchInfo.playerCount - 1; count++)
        {
            var playerCard = Instantiate(_matchPlayerCardPrefab, _otherPlayerContainer);
            RectTransform cardTransform = playerCard.GetComponent<RectTransform>();
            matchPlayerCardScripts.Add(playerCard.GetComponent<MatchPlayerCardScript>());
        }

        UpdatePlayerTurnUI(0);
    }

    public void UpdatePlayerTurnUI(int currentPlayerIndex)
    {
        for (int count = 0; count < matchPlayerCardScripts.Count; count++)
        {
            var playerIdentity = _matchController.playerTurnQueue[currentPlayerIndex];
            var player = _matchController.matchPlayers[playerIdentity];
            var cardScript = matchPlayerCardScripts[count];

            cardScript.UpdateMatchCard(player.name, null, false);

            currentPlayerIndex = (currentPlayerIndex + 1) % _matchController.playerTurnQueue.Count;
        }

        _yourTurnObject.SetActive(_matchController.currentPlayer.isLocalPlayer);
    }

    public void ShowWinnerScreen(PlayerStruct winner)
    {
        _winnerScreenObject.SetActive(true);
        _winnerNameText.text = winner.name;
    }

    public void ShowDrawScreen()
    {
        _drawScreenObject.SetActive(true);
    }

    // [ClientCallback]
    // public IEnumerator ShowMatchmakedScreen()
}
