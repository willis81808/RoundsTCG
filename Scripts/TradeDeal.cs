using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnityEngine;
using ModdingUtils;

public class TradeDeal : MonoBehaviour
{
    [SerializeField]
    internal CardPreview giveCard, takeCard;

    internal string tradeId;

    public void Initialize(string tradeId, Player giver, CardInfo giverCard, int giverCardIndex, Player taker, CardInfo takerCard, int takerCardIndex)
    {
        this.tradeId = tradeId;
        giveCard.Initialize(giver, giverCard, giverCardIndex);
        takeCard.Initialize(taker, takerCard, takerCardIndex);
    }
    
    public void Accept()
    {
        NetworkingManager.RPC(typeof(CardTradingMenu), nameof(CardTradingMenu.RPC_TryAcceptTradeDeal), tradeId);
    }

    public void Reject()
    {
        gameObject.SetActive(false);
    }

    public bool OverlapsWith(TradeDeal other)
    {
        return
            giveCard.cardToDisplay == other.giveCard.cardToDisplay ||
            giveCard.cardToDisplay == other.takeCard.cardToDisplay ||
            takeCard.cardToDisplay == other.giveCard.cardToDisplay ||
            takeCard.cardToDisplay == other.takeCard.cardToDisplay;
    }

    public IEnumerator DoTrade()
    {
        yield return SwapPlayerCard(giveCard.cardOwner, giveCard.cardIndex, takeCard.cardToDisplay);
        yield return SwapPlayerCard(takeCard.cardOwner, takeCard.cardIndex, giveCard.cardToDisplay);
    }

    private IEnumerator SwapPlayerCard(Player target, int oldCard, CardInfo newCard)
    {
        yield return ModdingUtils.Utils.Cards.instance.ReplaceCard(target, oldCard, newCard, "", 2f, 2f, true);
        yield return null;
    }
}
