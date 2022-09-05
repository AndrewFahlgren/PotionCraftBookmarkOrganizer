using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Perform any shenanigans that are needed to get the right recipe on screen and setup the subRail bookmarks to show this as the active bookmark
    /// </summary>
    public class UpdateSubRailRequestingLayerPatch
    { 
        [HarmonyPatch(typeof(BookmarkRail), "UpdateLayers")]
        public class BookmarkRail_UpdateLayers
        {
            static bool Prefix(BookmarkRail __instance)
            {
                return Ex.RunSafe(() => UpdateSubRailRequestingLayer(__instance, true));
            }
            static void Postfix(BookmarkRail __instance)
            {
                Ex.RunSafe(() => UpdateSubRailRequestingLayer(__instance, false));
            }
        }

        private static bool UpdateSubRailRequestingLayer(BookmarkRail instance, bool requesting)
        {
            //var isSubRail = instance.gameObject.name == StaticStorage.SubRailName;
            //if (!isSubRail) return true;
            //var originalController = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            //instance.bookmarkController = requesting
            //                                ? new SubRailBookmarkController { showActiveBookmarkInActiveLayer = originalController.showActiveBookmarkInActiveLayer }
            //                                : originalController;
            return true;
        }
    }
}
