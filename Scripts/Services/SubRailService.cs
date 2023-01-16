using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.BookmarkController;

namespace PotionCraftBookmarkOrganizer.Scripts.Services
{
    /// <summary>
    /// Service responsible for
    /// </summary>
    public static class SubRailService
    {
        public static bool IsSubRail(BookmarkRail rail)
        {
            if (rail == null) return false;
            return rail.gameObject.name == StaticStorage.SubRailName;
        }

        public static bool IsActiveBookmark(Bookmark bookmark)
        {
            if (bookmark.IsActiveBookmark()) return true;
            //TODO also check if this is our copied bookmark on the subrail
            return false;
        }

        public static bool IsInvisiRail(BookmarkRail rail)
        {
            if (rail == null) return false;
            return rail.gameObject.name == StaticStorage.InvisiRailName;
        }

        public static void UpdateSubRailForSelectedIndex(int pageIndex)
        {
            //Before we mess with indexes update the static bookmark to match this recipe
            UpdateStaticBookmark(pageIndex);
            var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(pageIndex);
            var allBookmarks = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList();
            var saved = GetSubRailRecipesForIndex(groupIndex).Select(s => new { savedBookmark = s, bookmark = allBookmarks[s.recipeIndex], isActive = pageIndex == s.recipeIndex }).ToList();
            //Check to see if we actually need to switch bookmarks on page turn or if we are in the same recipe group
            if (saved.Any(sb => StaticStorage.SubRail.railBookmarks.Any(rb => sb.bookmark == rb)))
            {
                saved.ForEach(savedBookmark =>
                {
                    savedBookmark.bookmark.CurrentVisualState = savedBookmark.isActive ? Bookmark.VisualState.Active : Bookmark.VisualState.Inactive;
                });
                return;
            }

            var bookmarksToRemove = StaticStorage.SubRail.railBookmarks.ToList();
            bookmarksToRemove.ForEach(b =>
            {
                var nextAvailSpace = GetNextEmptySpaceOnRail(StaticStorage.InvisiRail);
                if (nextAvailSpace == null)
                {
                    throw new Exception("PotionCraftBookmarkOrganizer - Somehow the InvisiRail ran out of space. All hope is lost!");
                }
                ConnectBookmarkToRail(StaticStorage.InvisiRail, b, nextAvailSpace.Value);
            });
            saved.ForEach(savedBookmark =>
            {
                ConnectBookmarkToRail(StaticStorage.SubRail, savedBookmark.bookmark, savedBookmark.savedBookmark.SerializedBookmark.position);
                savedBookmark.bookmark.CurrentVisualState = savedBookmark.isActive ? Bookmark.VisualState.Active : Bookmark.VisualState.Inactive;
            });
        }

        public static List<BookmarkStorage> GetSubRailRecipesForIndex(int nextPageIndex)
        {
            var recipeIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(nextPageIndex);
            if (!StaticStorage.BookmarkGroups.TryGetValue(recipeIndex, out List<BookmarkStorage> saved)) return new List<BookmarkStorage>();
            return saved;
        }

        public static Vector2? GetNextEmptySpaceOnRail(BookmarkRail rail, bool getMax = false)
        {
            var emptySegments = rail.GetEmptySegments(BookmarkController.SpaceType.Min);
            var spawnHeight = typeof(BookmarkController).GetField("spawnHeight", BindingFlags.NonPublic | BindingFlags.Instance)
                                                        .GetValue(rail.bookmarkController) as MinMaxFloat;
            if (emptySegments.Any()) return new Vector2((rail.inverseSpawnOrder || getMax) ? emptySegments.Last().max : emptySegments.First().min, spawnHeight.min);
            return null;
        }

        public static void UpdateStaticBookmark(int nextPageIndex = -1)
        {
            var recipeBook = Managers.Potion.recipeBook;
            if (nextPageIndex == -1) nextPageIndex = recipeBook.currentPageIndex;
            var index = RecipeBookService.GetBookmarkStorageRecipeIndex(nextPageIndex, out bool isparentIndex);
            var savedRecipe = recipeBook.savedRecipes[index];
            //Do not show the static bookmark for empty recipe pages
            if (savedRecipe == null)
            {
                StaticStorage.StaticBookmark.gameObject.SetActive(false);
                return;
            }
            StaticStorage.StaticBookmark.gameObject.SetActive(true);
            var sourceBookmark = recipeBook.bookmarkControllersGroupController.GetBookmarkByIndex(index);
            StaticStorage.StaticBookmark.SetBookmarkContent(sourceBookmark.activeBookmarkButton.normalSpriteIcon, sourceBookmark.inactiveBookmarkButton.normalSpriteIcon, null);
            StaticStorage.StaticBookmark.CurrentVisualState = isparentIndex ? Bookmark.VisualState.Inactive : Bookmark.VisualState.Active;
        }

        public static int GetPagesCountWithoutSpecialRails()
        {
            var controller = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            return controller.rails.Except(new[] { StaticStorage.SubRail, StaticStorage.InvisiRail }).Sum(r => r.railBookmarks.Count);
        }

        public static void ConnectBookmarkToRail(BookmarkRail rail, Bookmark bookmark, Vector2 position)
        {
            var bookmarksListBeforeMoving = StaticStorage.SubRail.bookmarkController.GetAllBookmarksList();
            rail.Connect(bookmark, position);
            rail.SortBookmarksInClockwiseOrder();
            rail.bookmarkController.CallOnBookmarksRearrangeIfNecessary(bookmarksListBeforeMoving);
        }

        public static void ShowSubRailAfterFlip()
        {
            StaticStorage.SubRail.gameObject.SetActive(true);
            StaticStorage.SubRailPages.gameObject.SetActive(true);
            StaticStorage.StaticRails.Values.ToList().ForEach(r =>
            {
                r.Item1.SetActive(false);
                r.Item2.SetActive(false);
            });
        }

        public static void UpdateNonSubBookmarksActiveState()
        {
            var selectedParentIndex = RecipeBookService.GetBookmarkStorageRecipeIndexForSelectedRecipe();
            var rails = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController.rails.Except(new[] { StaticStorage.SubRail, StaticStorage.InvisiRail });                                     
            var bookmarks = rails.SelectMany(r => r.railBookmarks).ToList();
            for (var i = 0; i < bookmarks.Count; i++)
            {
                bookmarks[i].CurrentVisualState = i == selectedParentIndex 
                                                    ? Bookmark.VisualState.Active 
                                                    : Bookmark.VisualState.Inactive;
            }
        }

        public static void UpdateSubBookmarksActiveState()
        {
            var bookmarks = StaticStorage.SubRail.railBookmarks;
            var firstBookmark = bookmarks.FirstOrDefault();
            if (firstBookmark == null) return;
            var startingSubRailIndex = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList().IndexOf(firstBookmark);
            for (var i = 0; i < bookmarks.Count; i++)
            {
                var trueIndex = i + startingSubRailIndex;
                bookmarks[i].CurrentVisualState = trueIndex == Managers.Potion.recipeBook.currentPageIndex
                                                        ? Bookmark.VisualState.Active
                                                        : Bookmark.VisualState.Inactive;
            }
        }

        public static Tuple<BookmarkRail, Vector2> GetSpawnPosition(BookmarkController bookmarkController, SpaceType spaceType)
        {
            var nonSpecialRails = bookmarkController.rails.Except(new[] { StaticStorage.SubRail, StaticStorage.InvisiRail }).ToList();
            foreach (var rail in nonSpecialRails)
            {
                var emptySegments = rail.GetEmptySegments(spaceType);
                if (emptySegments.Any())
                {
                    var x = rail.inverseSpawnOrder ? emptySegments.Last().max : emptySegments.First().min;
                    var spawnHeight = typeof(BookmarkController).GetField("spawnHeight", BindingFlags.NonPublic | BindingFlags.Instance)
                                                                .GetValue(bookmarkController) as MinMaxFloat;
                    var y = spawnHeight.GetRandom();
                    return new Tuple<BookmarkRail, Vector2>(rail, new Vector2(x, y));
                }
            }
            return null;
        }
    }
}
