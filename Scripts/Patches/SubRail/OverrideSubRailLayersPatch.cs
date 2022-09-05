using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class OverrideSubRailLayersPatch
    { 
        [HarmonyPatch(typeof(BookmarkRail), "SpawnLayers")]
        public class BookmarkRail_SpawnLayers
        {
            static bool Prefix(BookmarkRail __instance)
            {
                return Ex.RunSafe(() => OverrideSubRailLayers(__instance));
            }
        }

        private static bool OverrideSubRailLayers(BookmarkRail instance)
        {
            if (instance.gameObject.name != StaticStorage.SubRailName) return true;

            var layers = StaticStorage.SubRailLayers.ToArray();
            typeof(BookmarkRail).GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, layers);

            return false;
        }
    }
}
