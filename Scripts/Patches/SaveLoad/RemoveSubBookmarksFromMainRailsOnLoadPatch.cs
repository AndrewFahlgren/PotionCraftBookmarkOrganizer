using HarmonyLib;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.Bookmark;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.BookmarkController;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.BookmarkRail;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class RemoveSubBookmarksFromMainRailsOnLoadPatch
    { 
        [HarmonyPatch(typeof(BookmarkController), "LoadFrom")]
        public class BookmarkController_LoadFrom
        {
            static void Postfix(BookmarkController __instance)
            {
                Ex.RunSafe(() => RemoveSubBookmarksFromMainRailsOnLoad( __instance));
            }
        }

        private static void RemoveSubBookmarksFromMainRailsOnLoad(BookmarkController instance)
        {
            if (instance != Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController) return;
            //If there are no saved recipe positions then there is nothing to remove
            if (StaticStorage.SavedRecipePositions == null || !StaticStorage.SavedRecipePositions.Any()) return;
            RemoveSubBookmarksFromMainRails(instance);
            //Do this here because it is right before we load the recipes from the progress state into the recipe book
            ReorganizeSavedRecipes();
            StaticStorage.SavedRecipePositions = null;
        }

        private static void RemoveSubBookmarksFromMainRails(BookmarkController instance)
        {
            var railList = instance.rails.Except(new[] { StaticStorage.SubRail, StaticStorage.InvisiRail }).ToList();
            var allBookmarksCount = railList.Sum(r => r.railBookmarks.Count);

            var index = 0;
            var oldListIndex = 0;
            var railIndex = 0;
            var savedRecipeIndex = 0;
            while (index < allBookmarksCount)
            {
                var curRail = railList[0];
                while (railIndex >= curRail.railBookmarks.Count)
                {
                    railIndex = 0;
                    railList.RemoveAt(0);
                    curRail = railList[0];
                }
                var isSubRecipe = StaticStorage.SavedRecipePositions[savedRecipeIndex] != oldListIndex;
                if (!isSubRecipe)
                {
                    railIndex++;
                    index++;
                    savedRecipeIndex++;
                    oldListIndex++;
                    continue;
                }
                var bookmark = curRail.railBookmarks[railIndex];
                curRail.railBookmarks.RemoveAt(railIndex);
                UnityEngine.Object.Destroy(bookmark.gameObject);
                oldListIndex++;
                allBookmarksCount--;
            }
        }

        private static void ReorganizeSavedRecipes()
        {
            var progressState = Managers.SaveLoad.SelectedProgressState;
            var potionList = new List<SerializedPotionRecipe>();
            for (var i = 0; i < progressState.savedRecipes.Count; i++)
            {
                potionList.Add(progressState.savedRecipes[StaticStorage.SavedRecipePositions[i]]);
            }
            progressState.savedRecipes = potionList;
        }
    }
}
