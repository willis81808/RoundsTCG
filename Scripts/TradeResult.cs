using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradeResult : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI p1Header, p2Header;

    [SerializeField]
    private CardPreview p1Card, p2Card;

    public void Initialize(Player p1, CardInfo c1, Player p2, CardInfo c2)
    {
        p1Card.Initialize(p1, c1, -1);
        p1Header.text = p1.data.view.Controller.NickName;
        p1Header.color = p1.GetTeamColors().winText;

        p2Card.Initialize(p2, c2, -1);
        p2Header.text = p2.data.view.Controller.NickName;
        p2Header.color = p2.GetTeamColors().winText;
    }
}
