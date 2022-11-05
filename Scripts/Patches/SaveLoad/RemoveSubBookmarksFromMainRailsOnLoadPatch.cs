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
            DoIncorrectCountFailsafe();
            StaticStorage.SavedRecipePositions = null;
        }

        //The actual cause of this issue may now be fixed however there may still be save files which are in a messed up state
        private static void DoIncorrectCountFailsafe()
        {
            var bookmarkCount = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList().Count;
            var recipeCount = StaticStorage.SavedRecipePositions.Count;
            if (bookmarkCount == recipeCount) return;
            Plugin.PluginLogger.LogError("ERROR: There is an incorrect ammount of bookmarks saved. Running failsafe to fix file!");
            while (bookmarkCount > recipeCount)
            {
                var invisiRailCount = StaticStorage.InvisiRail.railBookmarks.Count;
                if (invisiRailCount == 0)
                {
                    Plugin.PluginLogger.LogError("ERROR: Incorrect count failsafe failed to find enough bookmarks on the invisirail to fix count. To recover save file post it in the Potion Craft discord modding channel!");
                    return;
                }
                var bookmark = StaticStorage.InvisiRail.railBookmarks[invisiRailCount - 1];
                StaticStorage.InvisiRail.railBookmarks.RemoveAt(invisiRailCount - 1);
                UnityEngine.Object.Destroy(bookmark.gameObject);
                bookmarkCount--;
            }
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
                var curSavedIndex = StaticStorage.SavedRecipePositions[savedRecipeIndex];
                var isOutOfPlace = curSavedIndex != oldListIndex;
                RecipeBookService.GetBookmarkStorageRecipeIndex(curSavedIndex, out bool indexIsParent);
                if (!isOutOfPlace && !indexIsParent)
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
