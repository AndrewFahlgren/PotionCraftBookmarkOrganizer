using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Services;
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
            static bool Prefix(BookmarkRail __instance)
            {
                return Ex.RunSafe(() => AddStaticBookmarkToRailBookmarkList(__instance));
            }

            static void Postfix(BookmarkRail __instance)
            {
                Ex.RunSafe(() => UpdateRaycastPriorityForBookmarks(__instance));
            }
        }

        private static bool AddStaticBookmarkToRailBookmarkList(BookmarkRail instance)
        {
            if (!SubRailService.IsSubRail(instance)) return true;
            if (StaticStorage.StaticBookmark == null) return true;
            instance.railBookmarks.Insert(0, StaticStorage.StaticBookmark);
            return true;
        }

        private static void UpdateRaycastPriorityForBookmarks(BookmarkRail instance)
        {
            if (!SubRailService.IsSubRail(instance)) return;
            if (StaticStorage.StaticBookmark == null) return;
            instance.railBookmarks.ForEach(b => b.SetRaycastPriorityLevel(b.inactiveBookmarkButton.raycastPriorityLevel - 500));
            if (instance.railBookmarks.FirstOrDefault() == StaticStorage.StaticBookmark) instance.railBookmarks.RemoveAt(0);
            StaticStorage.StaticBookmark.transform.parent = StaticStorage.SubRailActiveBookmarkLayer.transform;
        }

    }
}
