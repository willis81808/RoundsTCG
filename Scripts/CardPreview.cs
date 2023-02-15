using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

public class CardPreview : MonoBehaviour
{
    [SerializeField]
    internal Transform cardRoot;

    [SerializeField]
    private GameObject selectedIndicator, toggleOffButton, toggleOnButton;

    [HideInInspector]
    public CardInfo cardToDisplay;

    [HideInInspector]
    public Player cardOwner;
    [HideInInspector]
    public int cardIndex;

    private CardTradingMenu menu;

    private void Awake()
    {
        menu = GetComponentInParent<CardTradingMenu>();
    }

    private void OnDestroy()
    {
        if (menu == null) return;
        menu.OnItemDeselected(this);
    }
    
    public void Initialize(Player owner, CardInfo card, int cardIndex)
    {
        cardToDisplay = card;
        cardOwner = owner;
        this.cardIndex = cardIndex;

        SetupCard(cardToDisplay, cardRoot.gameObject);

        this.ExecuteAfterSeconds(1f, () =>
        {
            foreach (var anim in gameObject.GetComponentsInChildren<Animator>())
            {
                anim.enabled = false;
            }
            foreach (var posnoise in gameObject.GetComponentsInChildren<PositionNoise>())
            {
                posnoise.enabled = false;
            }
            var group = cardRoot.gameObject.GetOrAddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        });
    }

    public void Disable()
    {
        var group = gameObject.GetOrAddComponent<CanvasGroup>();
        group.alpha = 0.3f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void Select(bool emitEvent = true)
    {
        selectedIndicator.SetActive(true);
        toggleOffButton.SetActive(true);
        toggleOnButton.SetActive(false);

        if (emitEvent) menu.OnItemSelected(this);
    }

    public void Deselect(bool emitEvent = true)
    {
        selectedIndicator.SetActive(false);
        toggleOnButton.SetActive(true);
        toggleOffButton.SetActive(false);

        if (emitEvent) menu.OnItemDeselected(this);
    }

    /*
    public int GetCardIndex()
    {
        for (int i = 0; i < cardOwner.data.currentCards.Count; i++)
        {
            if (cardOwner.data.currentCards[i] == cardToDisplay) return i;
        }
        return -1;
    }
    */
    
    public GameObject SetupCard(CardInfo card, GameObject parent)
    {
        GameObject container = null;
        GameObject holder = null;

        try
        {
            container = GameObject.Instantiate(ItemShops.ItemShops.instance.assets.LoadAsset<GameObject>("Card Container"));
        }
        catch (Exception)
        {

            UnityEngine.Debug.Log("Issue with creating the card container");
        }

        try
        {
            holder = container.transform.Find("Card Holder").gameObject;
        }
        catch (Exception)
        {

            UnityEngine.Debug.Log("Issue with getting the Card Holder");
            holder = container.transform.GetChild(0).gameObject;
        }
        holder.transform.localPosition = new Vector3(0f, -100f, 0f);
        holder.transform.localScale = new Vector3(0.125f, 0.125f, 1f);
        holder.transform.Rotate(0f, 180f, 0f);

        GameObject cardObj = null;

        try
        {
            cardObj = GetCardVisuals(card, holder);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Issue with getting card visuals");
            UnityEngine.Debug.LogError(e);
        }

        container.transform.SetParent(parent.transform);
        container.transform.localPosition = Vector3.zero;
        container.transform.localScale = Vector3.one * 1.15f;

        return container;
    }
    private GameObject GetCardVisuals(CardInfo card, GameObject parent)
    {
        GameObject cardObj = GameObject.Instantiate<GameObject>(card.gameObject, parent.gameObject.transform);
        cardObj.SetActive(true);
        cardObj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
        RectTransform rect = cardObj.GetOrAddComponent<RectTransform>();
        rect.localScale = 100f * Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        GameObject back = FindObjectInChildren(cardObj, "Back");
        try
        {
            GameObject.Destroy(back);
        }
        catch { }
        FindObjectInChildren(cardObj, "BlockFront")?.SetActive(false);

        var canvasGroups = cardObj.GetComponentsInChildren<CanvasGroup>();
        foreach (var canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = 1;
        }

        this.ExecuteAfterSeconds(0.2f, () =>
        {
            var rarities = cardObj.GetComponentsInChildren<CardRarityColor>();

            foreach (var rarity in rarities)
            {
                try
                {
                    rarity.Toggle(true);
                }
                catch
                {

                }
            }
        });

        return cardObj;
    }
    private static GameObject FindObjectInChildren(GameObject gameObject, string gameObjectName)
    {
        Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
        return (from item in children where item.name == gameObjectName select item.gameObject).FirstOrDefault();
    }
}
