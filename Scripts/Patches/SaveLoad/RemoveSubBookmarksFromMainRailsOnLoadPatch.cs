﻿using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.SaveLoadSystem;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System.Collections.Generic;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class RemoveSubBookmarksFromMainRailsOnLoadPatch
    { 
        [HarmonyPatch(typeof(BookmarkController), "LoadFrom")]
        public class BookmarkController_LoadFrom
        {
            static void Postfix(BookmarkController __instance)
            {
                Ex.RunSafe(() => RemoveSubBookmarksFromMainRailsOnLoad( __instance), null, true);
            }
        }

        private static void RemoveSubBookmarksFromMainRailsOnLoad(BookmarkController instance)
        {
            if (instance != Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController) return;
            //If there are no saved recipe positions then there is nothing to remove
            if (StaticStorage.SavedRecipePositions == null || !StaticStorage.SavedRecipePositions.Any())
            {
                DoHardResetIfNeeded(instance);
                return;
            }
            RemoveSubBookmarksFromMainRails(instance);
            //Do this here because it is right before we load the recipes from the progress state into the recipe book
            ReorganizeSavedRecipes();
            DoIncorrectCountFailsafe();
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
                var curSavedIndex = StaticStorage.SavedRecipePositions[savedRecipeIndex];
                var isOutOfPlace = curSavedIndex != oldListIndex;
                RecipeBookService.GetBookmarkStorageRecipeIndex(index, out bool indexIsParent);
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

        //The actual cause of this issue may now be fixed however there may still be save files which are in a messed up state
        private static void DoIncorrectCountFailsafe()
        {
            var bookmarkCount = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList().Count;
            var recipeCount = StaticStorage.SavedRecipePositions?.Count ?? Managers.Potion.recipeBook.savedRecipes.Count;
            if (bookmarkCount == recipeCount) return;
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

        private static void DoHardResetIfNeeded(BookmarkController instance)
        {
            //There is a bug where saved recipe positions do not save meaning we also need to do a failsafe here
            var bookmarkCount = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList().Count;
            var recipeCount = Managers.SaveLoad.SelectedProgressState.savedRecipes.Count;
            if (bookmarkCount == recipeCount) return;
            Plugin.PluginLogger.LogError("ERROR: There is an incorrect ammount of bookmarks saved. Running failsafe to fix file! - 1");
            var savedGroupCount = StaticStorage.BookmarkGroups.SelectMany(b => b.Value).Count();
            var mainRails = instance.rails.Except(new[] { StaticStorage.SubRail, StaticStorage.InvisiRail }).ToList();
            while (bookmarkCount > recipeCount)
            {
                BookmarkRail railToRemove = null;
                if (StaticStorage.InvisiRail.railBookmarks.Count > 0)
                {
                    railToRemove = StaticStorage.InvisiRail;
                }
                else if (StaticStorage.SubRail.railBookmarks.Count > 0)
                {
                    railToRemove = StaticStorage.SubRail;
                }
                else
                {
                    railToRemove = mainRails.LastOrDefault(r => r.railBookmarks.Count > 0);
                }
                if (railToRemove == null)
                {
                    Plugin.PluginLogger.LogError("ERROR: Incorrect count failsafe failed to find enough bookmarks on any rails to fix count. To recover save file post it in the Potion Craft discord modding channel!");
                    return;
                }
                var bookmark = railToRemove.railBookmarks[railToRemove.railBookmarks.Count - 1];
                railToRemove.railBookmarks.RemoveAt(railToRemove.railBookmarks.Count - 1);
                UnityEngine.Object.Destroy(bookmark.gameObject);
                bookmarkCount--;
            }
            //Clear out bookmark groups so we can start fresh
            StaticStorage.BookmarkGroups.Clear();
            StaticStorage.SavedRecipePositions = null;
        }
    }
}
