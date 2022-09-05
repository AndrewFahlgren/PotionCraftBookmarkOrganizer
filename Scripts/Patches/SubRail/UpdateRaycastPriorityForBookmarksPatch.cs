using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using System.Reflection;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class UpdateRaycastPriorityForBookmarksPatch
    { 
        [HarmonyPatch(typeof(BookmarkRail), "UpdateLayers")]
        public class BookmarkRail_UpdateLayers
        {
            //static bool Prefix(BookmarkRail __instance)
            //{
            //    return Ex.RunSafe(() => UpdateSubRailRequestingLayer(__instance, true));
            //}
            static void Postfix(BookmarkRail __instance)
            {
                Ex.RunSafe(() => UpdateRaycastPriorityForBookmarks(__instance));
            }
        }

        private static void UpdateRaycastPriorityForBookmarks(BookmarkRail instance)
        {
            var isSubRail = instance.gameObject.name == StaticStorage.SubRailName;
            if (!isSubRail) return;
            instance.railBookmarks.ForEach(b => b.SetRaycastPriorityLevel(b.inactiveBookmarkButton.raycastPriorityLevel - 500));
        }

        //private static bool UpdateSubRailRequestingLayer(BookmarkRail instance, bool requesting)
        //{
        //    //var isSubRail = instance.gameObject.name == StaticStorage.SubRailName;
        //    //if (!isSubRail) return true;
        //    //var originalController = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
        //    //instance.bookmarkController = requesting
        //    //                                ? new SubRailBookmarkController { showActiveBookmarkInActiveLayer = originalController.showActiveBookmarkInActiveLayer }
        //    //                                : originalController;
        //    return true;
        //}
    }
}
