using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Assets
{
    internal static AssetBundle Bundle = AssetUtils.LoadAssetBundleFromResources("roundstcg", typeof(RoundsTCG).Assembly);
    
    internal static GameObject ChooseCardShop = Bundle.LoadAsset<GameObject>("Trading Card Menu");
}