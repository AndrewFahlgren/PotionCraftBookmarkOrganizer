using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class UpdateLayerForActiveSubRailBookmarkPatch
    { 
        [HarmonyPatch(typeof(Bookmark), "UpdateMovingState")]
        public class Bookmark_UpdateMovingState
        {
            static bool Prefix(Bookmark __instance, Bookmark.MovingState value)
            {
                return Ex.RunSafe(() => UpdateLayerForActiveSubRailBookmark(__instance, value));
            }
        }

        //TODO either abondon this idea or create some sort of custom transition piece we can place in. Currently it looks pretty rough due to color mismatch and issues with the mask not lining up perfectly.
        private static bool UpdateLayerForActiveSubRailBookmark(Bookmark instance, Bookmark.MovingState value)
        {
            return true;
            //if (value == instance.CurrentMovingState) return true;
            //if (value != Bookmark.MovingState.Idle) return true;
            //if (!SubRailService.IsSubRail(instance.rail)) return true;
            //if (!SubRailService.IsActiveBookmark(instance)) return true;
            //var bookmarkController = instance.rail.bookmarkController;
            //instance.SetRaycastPriorityLevel(bookmarkController.GetLayer(StaticStorage.SubRailLayers.Count).raycastPriorityLevelRange.min - 500);
            //instance.transform.parent = StaticStorage.SubRailActiveBookmarkLayer.transform;
            //return true;
        }
    }
}
