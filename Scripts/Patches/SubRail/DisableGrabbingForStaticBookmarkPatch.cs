using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Setup indexes for subbookmark and adjust all other indexes as needed
    /// If the first bookmark has changed update main bookmark with that icon
    /// </summary>
    public class DisableGrabbingForStaticBookmarkPatch
    { 
        [HarmonyPatch(typeof(Bookmark), "UpdateMovingState")]
        public class Bookmark_UpdateMovingState
        {
            static bool Prefix(Bookmark __instance, Bookmark.MovingState value)
            {
                return Ex.RunSafe(() => DisableGrabbingForStaticBookmark(__instance, value));
            }
        }

        private static bool DisableGrabbingForStaticBookmark(Bookmark instance, Bookmark.MovingState value)
        {
            if (instance != StaticStorage.StaticBookmark) return true;
            if (value == Bookmark.MovingState.Moving) return false;
            return true;
        }
    }
}
