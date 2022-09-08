using HarmonyLib;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Setup indexes for subbookmark and adjust all other indexes as needed
    /// If the first bookmark has changed update main bookmark with that icon
    /// </summary>
    public class DisallowSpawningOfNewBookmarksOnSpecialRailsPatch
    { 
        [HarmonyPatch(typeof(BookmarkController), "AddNewBookmark")]
        public class Bookmark_AddNewBookmark
        {
            static bool Prefix()
            {
                return Ex.RunSafe(() => SetAddingBookmarkFlag(true));
            }
            static void Postfix()
            {
                Ex.RunSafe(() => SetAddingBookmarkFlag(false));
            }
        }

        [HarmonyPatch(typeof(BookmarkRail), "GetEmptySegments")]
        public class BookmarkRail_GetEmptySegments
        {
            static bool Prefix(ref List<MinMaxFloat> __result, BookmarkRail __instance)
            {
                return DisallowSpawningOfNewBookmarksOnSpecialRails(ref __result, __instance);
            }
        }

        private static bool addingNewBookmark;

        private static bool SetAddingBookmarkFlag(bool adding)
        {
            addingNewBookmark = adding;
            return true;
        }

        private static bool DisallowSpawningOfNewBookmarksOnSpecialRails(ref List<MinMaxFloat> result, BookmarkRail instance)
        {
            if (!addingNewBookmark) return true;
            if (instance != StaticStorage.SubRail && instance != StaticStorage.InvisiRail) return true;
            result = new List<MinMaxFloat>();
            return false;
        }
    }
}
