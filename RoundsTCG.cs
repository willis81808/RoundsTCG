using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using UnboundLib;
using System.Reflection;
using UnboundLib.GameModes;
using ModsPlus;

[BepInDependency("com.willis.rounds.unbound")]
[BepInDependency("pykess.rounds.plugins.moddingutils")]
[BepInDependency("com.willuwontu.rounds.itemshops")]
[BepInDependency("com.willis.rounds.modsplus")]
[BepInDependency("root.rarity.lib")]
[BepInPlugin(ModId, ModName, ModVersion)]
[BepInProcess("Rounds.exe")]
public class RoundsTCG : BaseUnityPlugin
{
    private const string ModId = "com.willis.rounds.tcg";
    private const string ModName = "TCG";
    private const string ModVersion = "0.0.4";
    private const string CompatabilityModName = "TCG";
    
    public static RoundsTCG Instance { get; private set; }

    private static CardTradingMenu tradingMenu;
    private static bool isTrading = false;
    
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //Unbound.RegisterHandshake(ModId, SyncSettings);
        //Unbound.RegisterMenu("Rounds TCG", null, SetupMenu, showInPauseMenu: false);

        GameModeManager.AddHook(GameModeHooks.HookPickStart, ResetMenu);
        GameModeManager.AddHook(GameModeHooks.HookPickEnd, DoCardTrading, GameModeHooks.Priority.Last);

        // create trade opt-in button
        Instantiate(Assets.OptInButton, Unbound.Instance.canvas.transform);

        // create menu
        tradingMenu = Instantiate(Assets.ChooseCardShop, Unbound.Instance.canvas.transform).GetComponent<CardTradingMenu>();
        tradingMenu.Hide();

        UnboundLib.Cards.CustomCard.BuildCard<TestCard>();

        TradingFinishedEvent.RegisterListener(OnFinishedTrading);
    }

    private static void OnFinishedTrading(TradingFinishedEvent e)
    {
        isTrading = false;
    }

    private static IEnumerator ResetMenu(IGameModeHandler gameModeHandler)
    {
        tradingMenu.Clear();
        yield break;
    }

    private static IEnumerator DoCardTrading(IGameModeHandler gameModeHandler)
    {
        if (TradeStartButton.wantToTradePlayers.Count < 2) yield break;

        isTrading = true;

        tradingMenu.Show();

        yield return new WaitUntil(() => !isTrading);

        tradingMenu.Hide();
    }
}
