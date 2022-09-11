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
            static void Postfix( Book __instance, List<int> newIndexes)
            {
                Ex.RunSafe(() => UpdatePageIndexesOnRearrange(__instance, newIndexes));
            }
        }

        [HarmonyPatch(typeof(Book), "GetNextPageIndex")]
        public class Book_GetNextPageIndex
        {
            static bool Prefix(ref int __result, Book __instance)
            {
                return OverrideNextIndexWhenSwitchingPages(ref __result, __instance, true);
            }
        }

        [HarmonyPatch(typeof(Book), "GetPreviousPageIndex")]
        public class Book_GetPreviousPageIndex
        {
            static bool Prefix(ref int __result, Book __instance)
            {
                return OverrideNextIndexWhenSwitchingPages(ref __result, __instance, false);
            }
        }

        private static bool OverrideNextIndexWhenSwitchingPages(ref int result, Book instance, bool next)
        {
            if (instance is not RecipeBook) return true;
            var newResult = -1;
            Ex.RunSafe(() =>
            {
                var currentBookIndex = instance.currentPageIndex;
                var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(currentBookIndex, out bool indexIsParent);
                var isParentRecipe = RecipeBookService.IsBookmarkGroupParent(currentBookIndex);
                if (!indexIsParent && !isParentRecipe)
                {
                    //Go to the next page like normal
                    if (next)
                    {
                        var pagesCount = SubRailService.GetPagesCountWithoutSpecialRails();
                        newResult = (currentBookIndex + 1 + pagesCount) % pagesCount;
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

                        var pagesCount = SubRailService.GetPagesCountWithoutSpecialRails();
                        newResult = (groupIndex + 1 + pagesCount) % pagesCount;
                        return;
                    }
                    //Go back to the group parent
                    newResult = groupIndex;
                    return;
                }
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
            var pagesCount = SubRailService.GetPagesCountWithoutSpecialRails();
            var previousGroup = (groupIndex - 1 + pagesCount) % pagesCount;
            var previousBookmarkGroup = RecipeBookService.IsBookmarkGroupParent(previousGroup) ? StaticStorage.BookmarkGroups[previousGroup] : new List<BookmarkStorage>();
            var newResult = previousBookmarkGroup.OrderBy(b => b.recipeIndex).FirstOrDefault()?.recipeIndex ?? previousGroup;
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
            return nextBookmark;
        }

        private static void UpdatePageIndexesOnRearrange(Book instance, List<int> newIndexes)
        {
            UpdatePageIndexesOnRearrange(instance.curlPageController.frontLeftPage, newIndexes);
            UpdatePageIndexesOnRearrange(instance.curlPageController.frontRightPage, newIndexes);
            UpdatePageIndexesOnRearrange(instance.curlPageController.backLeftPage, newIndexes);
            UpdatePageIndexesOnRearrange(instance.curlPageController.backRightPage, newIndexes);
        }

        private static void UpdatePageIndexesOnRearrange(Page page, List<int> newIndexes)
        {
            page.pageIndex = newIndexes.IndexOf(page.pageIndex);
        }
    }
}
