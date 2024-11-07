using Org.BouncyCastle.Asn1.X509;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchPlayerCardScript : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _playerName;
    [SerializeField] Image _playerAvatar;

    public void UpdateMatchCard(string playerName, Image playerAvatar, bool activePlayer)
    {
        _playerName.text = playerName;
    }
}
