using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;
using UnboundLib.Networking;
using ModsPlus;
using Photon.Pun;

public class CardTradingMenu : MonoBehaviour
{
    internal static CardTradingMenu Instance { get; set; }

    [SerializeField]
    internal Transform offerMenuRoot, dealMenuRoot, resultsMenuRoot;

    [SerializeField]
    private TMPro.TextMeshProUGUI titleText;

    [SerializeField]
    private Button confirmButton, doneButton;

    [SerializeField]
    private Transform playerListContent, targetCardsContent, myCardsContent, dealsContent;

    [SerializeField]
    private CardPreview previewPrefab;

    [SerializeField]
    private TradeDeal tradeDealPrefab;

    [SerializeField]
    private TradeResult tradeResultPrefab;

    [SerializeField]
    private GameObject playerButtonPrefab;

    private List<CardPreview> selectedCards = new List<CardPreview>();

    internal List<Tuple<CardPreview, CardPreview>> pendingOffers = new List<Tuple<CardPreview, CardPreview>>();
    internal List<TradeDeal> pendingDeals = new List<TradeDeal>();

    internal HashSet<Player> doneTradingPlayers = new HashSet<Player>();
    internal HashSet<TradeDeal> acceptedDeals = new HashSet<TradeDeal>();
    internal HashSet<TradeDeal> myPendingDeals = new HashSet<TradeDeal>();
    
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        confirmButton.interactable = selectedCards.Count == 2;
    }

    private void SetupPlayerCards(Player me)
    {
        PopulateCardView(me, myCardsContent);
    }

    private void SetupTargetCards(Player target)
    {
        titleText.text = $"Trading with: {target.data.view.Controller.NickName}";
        PopulateCardView(target, targetCardsContent);
    }

    private void PopulateCardView(Player player, Transform container)
    {
        // disable any previously displayed card views
        foreach (var child in container.GetComponentsInChildren<CardPreview>())
        {
            Destroy(child.gameObject);
        }

        // spawn player cards
        for (int i = player.data.currentCards.Count - 1; i >= 0; i--)
        {
            var card = player.data.currentCards[i];
            var view = Instantiate(previewPrefab, container);
            view.Initialize(player, card, i);

            // disable any cards that are part of a currently pending offer
            if (pendingOffers.Where(pair => pair.Item1.cardToDisplay == card || pair.Item2.cardToDisplay == card).Count() > 0)
            {
                view.Disable();
            }
        }
    }

    private void SetupPlayerList()
    {
        for (int i = PlayerManager.instance.players.Count - 1; i >= 0; i--)
        {
            var player = PlayerManager.instance.players[i];
            if (player.data.view.IsMine || player.IsMinion()) continue;
            SetupPlayerListButton(player, () => SetupTargetCards(player));
        }
    }

    private void SetupPlayerListButton(Player player, Action clickedAction)
    {
        var buttonObj = Instantiate(playerButtonPrefab, playerListContent);
        buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = player.data.view.Controller.NickName;
        buttonObj.GetComponentInChildren<Button>().onClick.AddListener(() => clickedAction?.Invoke());
    }
    
    public void StopTrading()
    {
        dealMenuRoot.gameObject.SetActive(false);
        var self = PlayerManager.instance.players.Where(p => p.data.view.IsMine && !p.IsMinion()).First();

        NetworkingManager.RPC(typeof(CardTradingMenu), nameof(RPCA_FinalizeDeals), self.playerID);
    }

    public void FinalizeOffers()
    {
        offerMenuRoot.gameObject.SetActive(false);
        var self = PlayerManager.instance.players.Where(p => p.data.view.IsMine && !p.IsMinion()).First();

        var serializedOffers = new List<object>();
        foreach (var offer in pendingOffers)
        {
            serializedOffers.Add(offer.Item1.cardOwner.playerID);
            serializedOffers.Add(offer.Item1.cardIndex);
            serializedOffers.Add(offer.Item2.cardOwner.playerID);
            serializedOffers.Add(offer.Item2.cardIndex);
            serializedOffers.Add(Guid.NewGuid().ToString());
        }

        NetworkingManager.RPC(typeof(CardTradingMenu), nameof(RPCA_FinalizeOffers), self.playerID, serializedOffers.ToArray());
    }

    public void SubmitTradeOffer()
    {
        if (selectedCards.Count != 2) return;
        pendingOffers.Add(Tuple.Create(selectedCards[0], selectedCards[1]));

        for (int i = selectedCards.Count - 1; i >= 0; i--)
        {
            var card = selectedCards[i];
            card.Deselect();
            card.Disable();
        }
    }

    public void OnItemSelected(CardPreview card)
    {
        if (selectedCards.Where(c => c.cardOwner == card.cardOwner).FirstOrDefault() is CardPreview oldCard)
        {
            oldCard.Deselect();
        }
        selectedCards.Add(card);
    }

    public void OnItemDeselected(CardPreview card)
    {
        if (selectedCards.Where(c => c.cardOwner == card.cardOwner).FirstOrDefault() is CardPreview oldCard)
        {
            oldCard.Deselect(false);
            selectedCards.Remove(oldCard);
        }
        card.Deselect(false);
    }

    public void Show()
    {
        titleText.text = "Select a player to trade with";
        offerMenuRoot.gameObject.SetActive(true);
        dealMenuRoot.gameObject.SetActive(false);
        resultsMenuRoot.gameObject.SetActive(false);
        gameObject.SetActive(true);
        foreach (var player in PlayerManager.instance.players)
        {
            if (player.data.view.IsMine && !player.IsMinion())
            {
                SetupPlayerCards(player);
            }
        }
        SetupPlayerList();
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
    }

    public void Clear()
    {
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }
        foreach (var child in targetCardsContent.GetComponentsInChildren<CardPreview>())
        {
            Destroy(child.gameObject);
        }
        foreach (var child in myCardsContent.GetComponentsInChildren<CardPreview>())
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in dealsContent)
        {
            Destroy(child.gameObject);
        }
        selectedCards.Clear();
        pendingOffers.Clear();
        pendingDeals.Clear();
        acceptedDeals.Clear();
        myPendingDeals.Clear();
        doneTradingPlayers.Clear();
    }

    private TradeDeal CreateDeal(string dealId, Player self, CardInfo selfCard, int selfCardIndex, Player other, CardInfo otherCard, int otherCardIndex)
    {
        var newDeal = Instantiate(tradeDealPrefab, dealsContent);
        pendingDeals.Add(newDeal);

        this.ExecuteAfterSeconds(2f, () => newDeal.Initialize(dealId, self, selfCard, selfCardIndex, other, otherCard, otherCardIndex));

        return newDeal;
    }

    [UnboundRPC]
    public static void RPCA_FinalizeOffers(int playerId, object[] offers)
    {
        var player = PlayerManager.instance.GetPlayerWithID(playerId);
        Instance.doneTradingPlayers.Add(player);

        var self = PlayerManager.instance.players.Where(p => p.data.view.IsMine && !p.IsMinion()).First();
        for (int i = 0; i < offers.Length; i += 5)
        {
            var p1 = PlayerManager.instance.GetPlayerWithID((int)offers[i]);
            var c1i = (int)offers[i + 1];
            var c1 = p1.data.currentCards[c1i];
            var p2 = PlayerManager.instance.GetPlayerWithID((int)offers[i + 2]);
            var c2i = (int)offers[i + 3];
            var c2 = p2.data.currentCards[c2i];
            var dealId = (string)offers[i + 4];

            if (p1 == self && p2 == player)
            {
                var newDeal = Instance.CreateDeal(dealId, p1, c1, c1i, p2, c2, c2i);
                Instance.myPendingDeals.Add(newDeal);
            }
            else if (p2 == self && p1 == player)
            {
                var newDeal = Instance.CreateDeal(dealId, p2, c2, c2i, p1, c1, c1i);
                Instance.myPendingDeals.Add(newDeal);
            }
            else
            {
                var otherDeal = Instance.CreateDeal(dealId, p1, c1, c1i, p2, c2, c2i);
                otherDeal.gameObject.SetActive(false);
            }
        }

        if (!PhotonNetwork.IsMasterClient) return;

        if (Instance.doneTradingPlayers.Count >= PlayerManager.instance.players.Where(p => !p.IsMinion()).Count())
        {
            NetworkingManager.RPC(typeof(CardTradingMenu), nameof(RPCA_BeginInspectingDeals));
        }
    }

    [UnboundRPC]
    public static void RPCA_FinalizeDeals(int playerId)
    {
        var player = PlayerManager.instance.GetPlayerWithID(playerId);
        Instance.doneTradingPlayers.Add(player);

        if (!PhotonNetwork.IsMasterClient) return;

        if (Instance.doneTradingPlayers.Count >= PlayerManager.instance.players.Where(p => !p.IsMinion()).Count())
        {
            Instance.StartCoroutine(PerformTradesCoroutine());
        }
    }
    
    [UnboundRPC]
    private static void RPCA_ShowResults(int p1i, int c1i, int p2i, int c2i)
    {
        Instance.resultsMenuRoot.gameObject.SetActive(true);
        Instance.dealMenuRoot.gameObject.SetActive(false);

        var p1 = PlayerManager.instance.GetPlayerWithID(p1i);
        var c1 = p1.data.currentCards[c1i];
        var p2 = PlayerManager.instance.GetPlayerWithID(p2i);
        var c2 = p2.data.currentCards[c2i];

        var tradeResult = Instantiate(Instance.tradeResultPrefab, Instance.resultsMenuRoot);
        tradeResult.Initialize(p1, c2, p2, c1);
        Destroy(tradeResult.gameObject, 3f);
    }

    private static IEnumerator PerformTradesCoroutine()
    {
        foreach (var trade in Instance.acceptedDeals)
        {
            yield return trade.DoTrade();

            NetworkingManager.RPC(typeof(CardTradingMenu), nameof(RPCA_ShowResults),
                trade.giveCard.cardOwner.playerID,
                trade.giveCard.cardIndex,
                trade.takeCard.cardOwner.playerID,
                trade.takeCard.cardIndex);
            
            yield return new WaitForSeconds(3f);
        }
        NetworkingManager.RPC(typeof(CardTradingMenu), nameof(RPCA_ResumeGame));
    }

    [UnboundRPC]
    public static void RPC_TryAcceptTradeDeal(string tradeId)
    {
        if (!PhotonNetwork.IsMasterClient || Instance.acceptedDeals.Any(d => d.tradeId == tradeId)) return;

        Instance.acceptedDeals.Add(Instance.pendingDeals.Where(d => d.tradeId == tradeId).First());
        NetworkingManager.RPC(typeof(CardTradingMenu), nameof(RPCA_OnDealAccepted), tradeId);
    }

    [UnboundRPC]
    public static void RPCA_OnDealAccepted(string tradeId)
    {
        var deal = Instance.pendingDeals.First(d => d.tradeId == tradeId);

        for (int i = Instance.pendingDeals.Count - 1; i >= 0; i--)
        {
            var relatedDeal = Instance.pendingDeals[i];
            if (!deal.OverlapsWith(relatedDeal)) continue;

            relatedDeal.gameObject.SetActive(false);
            Instance.pendingDeals.RemoveAt(i);
        }

        deal.gameObject.SetActive(false);
        Instance.pendingDeals.Remove(deal);
    }

    [UnboundRPC]
    public static void RPCA_BeginInspectingDeals()
    {
        Instance.doneTradingPlayers.Clear();
        Instance.dealMenuRoot.gameObject.SetActive(true);
        if (Instance.myPendingDeals.Count == 0)
        {
            Instance.StopTrading();
        }
    }

    [UnboundRPC]
    public static void RPCA_ResumeGame()
    {
        new TradingFinishedEvent().FireEvent();
    }
}

public class TradingFinishedEvent : EventDispatcher.Event<TradingFinishedEvent> { }
