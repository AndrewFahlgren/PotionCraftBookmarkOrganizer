using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Setup indexes for subbookmark and adjust all other indexes as needed
    /// If the first bookmark has changed update main bookmark with that icon
    /// </summary>
    public class ReturnCustomLayersToSubRailPatch
    { 
        [HarmonyPatch(typeof(BookmarkController), "GetLayer")]
        public class BookmarkController_GetLayer
        {
            static bool Prefix(ref BookmarkLayer __result, BookmarkController __instance, int index)
            {
                return ReturnCustomLayersToSubRail(ref __result, __instance, index);
            }
        }

        private static bool ReturnCustomLayersToSubRail(ref BookmarkLayer __result, BookmarkController instance,int index)
        {
            return true;
            //if (instance is not SubRailBookmarkController) return true;
            //if (index < 0) index = StaticStorage.SubRailLayers.Count - 1;
            //if (index < 0) return true;
            //if (StaticStorage.SubRailLayers.Count <= index) return true;
            //__result = new BookmarkLayer
            //{
            //    sortingLayer = PotionCraft.SpriteSortingLayers.RecipeBook,
            //    sortingOrder = StaticStorage.SubRailLayers[index].GetComponent<SortingGroup>().sortingOrder
            //};
            //return false;
        }
    }
}
