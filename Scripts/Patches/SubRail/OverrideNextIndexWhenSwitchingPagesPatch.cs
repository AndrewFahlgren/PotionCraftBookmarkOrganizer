using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
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
    public class OverrideNextIndexWhenSwitchingPagesPatch
    {
        [HarmonyPatch(typeof(Book), "OnBookmarksRearranged")]
        public class Book_OnBookmarksRearranged
        {
            static bool Prefix(Book __instance)
            {
                var index = __instance.currentPageIndex;
                Plugin.PluginLogger.LogInfo($"Book_OnBookmarksRearranged - old index: {index} - {Managers.Potion.recipeBook.savedRecipes[index].GetLocalizedTitle()}");
                return true;
            }

            static void Postfix( Book __instance, List<int> newIndexes)
            {
                var index = __instance.currentPageIndex;
                Plugin.PluginLogger.LogInfo($"Book_OnBookmarksRearranged - new index: {index} - {Managers.Potion.recipeBook.savedRecipes[index].GetLocalizedTitle()}");

                __instance.curlPageController.frontLeftPage.pageIndex = newIndexes.IndexOf(__instance.curlPageController.frontLeftPage.pageIndex);
                __instance.curlPageController.frontRightPage.pageIndex = newIndexes.IndexOf(__instance.curlPageController.frontRightPage.pageIndex);
                __instance.curlPageController.backLeftPage.pageIndex = newIndexes.IndexOf(__instance.curlPageController.backLeftPage.pageIndex);
                __instance.curlPageController.backRightPage.pageIndex = newIndexes.IndexOf(__instance.curlPageController.backRightPage.pageIndex);
            }
        }

        [HarmonyPatch(typeof(Book), "GetNextPageIndex")]
        public class Book_GetNextPageIndex
        {
            static bool Prefix(ref int __result, Book __instance)
            {
                var t1 = OverrideNextIndexWhenSwitchingPages(ref __result, __instance, true);
                Plugin.PluginLogger.LogInfo("---");
                Plugin.PluginLogger.LogInfo("---");
                return t1;
            }
        }

        [HarmonyPatch(typeof(Book), "GetPreviousPageIndex")]
        public class Book_GetPreviousPageIndex
        {
            static bool Prefix(ref int __result, Book __instance)
            {
                var t1 = OverrideNextIndexWhenSwitchingPages(ref __result, __instance, false);
                Plugin.PluginLogger.LogInfo("---");
                Plugin.PluginLogger.LogInfo("---");
                return t1;
            }
        }

        private static bool OverrideNextIndexWhenSwitchingPages(ref int result, Book instance, bool next)
        {
            if (instance is not RecipeBook) return true;
            var newResult = -1;
            Ex.RunSafe(() =>
            {
                var currentBookIndex = instance.currentPageIndex;
                Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 0 - currentBookIndex: {currentBookIndex} - {Managers.Potion.recipeBook.savedRecipes[currentBookIndex].GetLocalizedTitle()}");
                var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(currentBookIndex, out bool indexIsParent); //TODo THIS IS WRONG BECAUSE THE CURRENT INDEX IS WRONG. tHE CURRENT INDEX NEEDS TO BE FIXED AFTER MOVING FROM SUBRAIL AND BACK. hOW DO WE ENSURE THIS HAPPENS??
                Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 0 - groupIndex: {groupIndex} - {Managers.Potion.recipeBook.savedRecipes[groupIndex].GetLocalizedTitle()}");
                var isParentRecipe = RecipeBookService.IsBookmarkGroupParent(currentBookIndex);
                if (!indexIsParent && !isParentRecipe)
                {
                    Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 1 - next:{next}");
                    //Go to the next page like normal
                    if (next)
                    {
                        Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 1 - currentBookIndex: {currentBookIndex} - {Managers.Potion.recipeBook.savedRecipes[currentBookIndex].GetLocalizedTitle()}");
                        var pagesCount = SubRailService.GetPagesCountWithoutSpecialRails();
                        newResult = (currentBookIndex + 1 + pagesCount) % pagesCount;
                        Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 1 - newResult: {newResult} - {Managers.Potion.recipeBook.savedRecipes[newResult].GetLocalizedTitle()}");
                        return;
                    }
                    //Get the last bookmark from the previous group
                    newResult = GetPreviousIndexForPreviousGroup(groupIndex);
                    return;
                }
                var currentBookmarkGroup = StaticStorage.BookmarkGroups[groupIndex];
                BookmarkStorage nextBookmark = null;
                if (indexIsParent)
                {
                    Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 2 - next:{next}");
                    //Get the next recipe in the group we should go to
                    nextBookmark = GetNextBookmarkFromGroup(currentBookmarkGroup, currentBookIndex, next, false);
                    if (nextBookmark != null)
                    {
                        newResult = nextBookmark.recipeIndex;
                        return;
                    }
                    //Go to the next page like normal using the group parent index
                    if (next)
                    {
                        Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 2 - groupIndex: {groupIndex} - {Managers.Potion.recipeBook.savedRecipes[groupIndex].GetLocalizedTitle()}");

                        var pagesCount = SubRailService.GetPagesCountWithoutSpecialRails();
                        newResult = (groupIndex + 1 + pagesCount) % pagesCount;
                        Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 2 - newResult: {newResult} - {Managers.Potion.recipeBook.savedRecipes[newResult].GetLocalizedTitle()}");
                        return;
                    }
                    //Go back to the group parent
                    newResult = groupIndex;
                    Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 2 - newResult: {newResult} - {Managers.Potion.recipeBook.savedRecipes[newResult].GetLocalizedTitle()}");
                    return;
                }
                Plugin.PluginLogger.LogInfo($"OverrideNextIndexWhenSwitchingPages - 3 - next:{next}");
                if (next)
                {
                    //Go to the next bookmark in this group
                    newResult = GetNextBookmarkFromGroup(currentBookmarkGroup, currentBookIndex, next, true).recipeIndex;
                    return;
                }
                //Get the last bookmark from the previous group
                newResult = GetPreviousIndexForPreviousGroup(groupIndex);
                return;
            });
            if (newResult >= 0)
            {
                result = newResult;
                return false;
            }
            return true;
        }

        private static int GetPreviousIndexForPreviousGroup(int groupIndex)
        {
            Plugin.PluginLogger.LogInfo($"GetPreviousIndexForPreviousGroup - 1 - groupIndex: {groupIndex} - {Managers.Potion.recipeBook.savedRecipes[groupIndex].GetLocalizedTitle()}");
            var pagesCount = SubRailService.GetPagesCountWithoutSpecialRails();
            var previousGroup = (groupIndex - 1 + pagesCount) % pagesCount;
            Plugin.PluginLogger.LogInfo($"GetPreviousIndexForPreviousGroup - 2 - previousGroup: {previousGroup} - {Managers.Potion.recipeBook.savedRecipes[previousGroup].GetLocalizedTitle()}");
            var previousBookmarkGroup = RecipeBookService.IsBookmarkGroupParent(previousGroup) ? StaticStorage.BookmarkGroups[previousGroup] : new List<BookmarkStorage>();
            Plugin.PluginLogger.LogInfo($"GetPreviousIndexForPreviousGroup - 3 - previousBookmarkGroup.Count: {previousBookmarkGroup.Count}");
            var newResult = previousBookmarkGroup.OrderBy(b => b.recipeIndex).FirstOrDefault()?.recipeIndex ?? previousGroup;
            Plugin.PluginLogger.LogInfo($"GetPreviousIndexForPreviousGroup - 4 - newResult: {newResult} - {Managers.Potion.recipeBook.savedRecipes[newResult].GetLocalizedTitle()}");
            return newResult;
        }

        private static BookmarkStorage GetNextBookmarkFromGroup(List<BookmarkStorage> currentBookmarkGroup, int currentBookIndex, bool next, bool isParent)
        {
            if (!isParent)
            {
                currentBookmarkGroup = currentBookmarkGroup.Where(b => next ? b.recipeIndex < currentBookIndex : b.recipeIndex > currentBookIndex).ToList();
            }
            BookmarkStorage nextBookmark;
            if (next)
            {
                nextBookmark = currentBookmarkGroup.OrderByDescending(b => b.recipeIndex).FirstOrDefault();
            }
            else
            {
                nextBookmark = currentBookmarkGroup.OrderBy(b => b.recipeIndex).FirstOrDefault();
            }
            var currentTitle = Managers.Potion.recipeBook.savedRecipes[currentBookIndex].GetLocalizedTitle();
            var nextTitle = nextBookmark == null ? "null" : Managers.Potion.recipeBook.savedRecipes[nextBookmark.recipeIndex].GetLocalizedTitle();
            Plugin.PluginLogger.LogInfo($"GetNextBookmarkFromGroup - current: {currentTitle} - next: {nextTitle}");
            return nextBookmark;
        }
    }
}
