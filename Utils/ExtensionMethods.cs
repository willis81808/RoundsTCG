using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.UI;

public static class ExtensionMethods
{
    public static Slider CreateSlider(this ConfigEntry<float> config, GameObject menu, string title, float min, float max, int fontSize = 30)
    {
        MenuHandler.CreateSlider(title, menu, fontSize, min, max, config.Value, value => config.Value = value, out var slider);
        return slider;
    }

    public static bool IsMinion(this Player p)
    {
        return ModdingUtils.AIMinion.Extensions.CharacterDataExtension.GetAdditionalData(p.data).isAIMinion;
    }
}