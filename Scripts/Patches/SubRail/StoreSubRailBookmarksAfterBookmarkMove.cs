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
    public class StoreSubRailBookmarksAfterBookmarkMove
    { 
        [HarmonyPatch(typeof(Bookmark), "UpdateMovingState")]
        public class Bookmark_UpdateMovingState
        {
            static void Postfix(Bookmark.MovingState value)
            {
                Ex.RunSafe(() => UpdateLayerForActiveSubRailBookmark(value));
            }
        }

        private static void UpdateLayerForActiveSubRailBookmark(Bookmark.MovingState value)
        {
            if (value != Bookmark.MovingState.Idle) return;
            RecipeBookService.UpdateBookmarkGroupsForCurrentRecipe();
            SubRailService.UpdateSubRailForSelectedIndex(Managers.Potion.recipeBook.currentPageIndex);
            return;
        }
    }
}
