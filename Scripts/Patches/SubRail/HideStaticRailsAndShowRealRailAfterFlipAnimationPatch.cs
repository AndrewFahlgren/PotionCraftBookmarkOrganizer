using HarmonyLib;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class HideStaticRailsAndShowRealRailAfterFlipAnimationPatch
    { 
        [HarmonyPatch(typeof(CurlPageController), "OnPageFlippingEnd")]
        public class CurlPageController_OnPageFlippingEnd
        {
            static void Postfix(CurlPageController __instance)
            {
                Ex.RunSafe(() => HideStaticRailsAndShowRealRailAfterFlipAnimation(__instance));
            }
        }

        private static void HideStaticRailsAndShowRealRailAfterFlipAnimation(CurlPageController instance)
        {
            SubRailService.ShowSubRailAfterFlip();
        }
    }
}
