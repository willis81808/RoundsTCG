using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnboundLib.Networking;
using UnboundLib.GameModes;
using ModsPlus;
using UnboundLib;

public class TradeStartButton : MonoBehaviour
{
    internal static HashSet<Player> wantToTradePlayers = new HashSet<Player>();

    private void Start()
    {
        GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);
        GameModeManager.AddHook(GameModeHooks.HookPickEnd, OnPickEnd);
        gameObject.SetActive(false);
    }

    private IEnumerator OnPickStart(IGameModeHandler gm)
    {
        wantToTradePlayers.Clear();
        gameObject.SetActive(true);
        yield break;
    }

    private IEnumerator OnPickEnd(IGameModeHandler gm)
    {
        gameObject.SetActive(false);
        yield break;
    }

    public void SubmitTradeRequest()
    {
        var self = PlayerManager.instance.players.Where(p => p.data.view.IsMine && !p.IsMinion()).First();
        NetworkingManager.RPC(typeof(TradeStartButton), nameof(RPCA_WantsToTrade), self.playerID);
        gameObject.SetActive(false);
    }

    [UnboundRPC]
    private static void RPCA_WantsToTrade(int playerId)
    {
        var player = PlayerManager.instance.GetPlayerWithID(playerId);
        wantToTradePlayers.Add(player);
    }
}
