using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class WaitingForPlayersText : MonoBehaviour
{
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    
    void Update()
    {
        var pendingPlayers = PlayerManager.instance.players
            .Except(CardTradingMenu.Instance.doneTradingPlayers)
            .Where(p => !p.IsMinion())
            .ToList();

        var sb = new StringBuilder();
        for (int i = pendingPlayers.Count - 1; i >= 0; i--)
        {
            sb.AppendLine(pendingPlayers[i].data.view.Controller.NickName);
        }
        text.text = sb.ToString();
    }
}
