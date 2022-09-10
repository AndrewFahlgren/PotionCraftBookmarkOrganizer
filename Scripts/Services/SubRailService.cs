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
            var activeBookmarkIconContourColor = typeof(RecipeBook).GetField("activeBookmarkIconContourColor", BindingFlags.NonPublic | BindingFlags.Instance)
                                                                   .GetValue(recipeBook) as ColorObject;
            var inactiveBookmarkIconContourColor = typeof(RecipeBook).GetField("inactiveBookmarkIconContourColor", BindingFlags.NonPublic | BindingFlags.Instance)
                                                                     .GetValue(recipeBook) as ColorObject;
            var bookmarkIcon1 = savedRecipe.coloredIcon.GetSprite(activeBookmarkIconContourColor, true);
            var bookmarkIcon2 = savedRecipe.coloredIcon.GetSprite(inactiveBookmarkIconContourColor, true);
            StaticStorage.StaticBookmark.SetBookmarkContent(bookmarkIcon1, bookmarkIcon2, null);
            StaticStorage.StaticBookmark.CurrentVisualState = isparentIndex ? Bookmark.VisualState.Inactive : Bookmark.VisualState.Active;
            //TODO what does mirroring actuall do? It is flipping bookmark icons upside down with this code. Do we need this at all?
            //var isOriginalMirrored = recipeBook.bookmarkControllersGroupController.GetBookmarkByIndex(index).isMirrored;
            //if (StaticStorage.StaticBookmark.isMirrored != isOriginalMirrored) StaticStorage.StaticBookmark.SetMirrored(true);
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
    }
}
