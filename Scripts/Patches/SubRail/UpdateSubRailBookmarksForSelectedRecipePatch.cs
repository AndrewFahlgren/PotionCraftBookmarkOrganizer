using HarmonyLib;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class UpdateSubRailBookmarksForSelectedRecipePatch
    { 
        [HarmonyPatch(typeof(CurlPageController), "HotkeyClicked")]
        public class CurlPageController_HotkeyClicked
        {
            static bool Prefix(CurlPageController __instance, bool toTheLeft, int nextPageIndex)
            {
                return Ex.RunSafe(() => UpdateSubRailBookmarksForSelectedRecipe(__instance, toTheLeft, nextPageIndex));
            }
        }

        private static float lastPageTurnTime = 0;
        private static bool UpdateSubRailBookmarksForSelectedRecipe(CurlPageController instance, bool toTheLeft, int nextPageIndex)
        {
            if (instance.book != Managers.Potion.recipeBook) return true;
            //When moving between pages quickly the recipe book index may not be up to date. It only updates once the page flipping animation has finished.
            //If these indexes are out of sync it is because we quickly flipped past the last page. In this case we do not need to update sub bookmarks since nothing had a chance to change.
            //If you just hold down the hotkey eventually things move so fast we start getting idle states thrown in. Here we have an additional failsafe to make sure we don't update if we just flipped a page by checking time.
            if (instance.CurrentState == CurlPageController.State.Idle && (Time.fixedTime - lastPageTurnTime) > 0.5f)
            {
                var recipeAtIndex = Managers.Potion.recipeBook.savedRecipes[instance.book.currentPageIndex];
                Plugin.PluginLogger.LogInfo($"UpdateSubRailBookmarksForSelectedRecipe - Updating for: {recipeAtIndex?.GetLocalizedTitle()} - subCount: {StaticStorage.SubRail.railBookmarks.Count}");
                RecipeBookService.UpdateBookmarkGroupsForCurrentRecipe();
            }
            lastPageTurnTime = Time.fixedTime;
            MoveBookmarksToAndFromInvisiRail(instance, toTheLeft, nextPageIndex);
            return true;
        }

        private static void MoveBookmarksToAndFromInvisiRail(CurlPageController instance, bool toTheLeft, int nextPageIndex)
        {
            if (nextPageIndex < 0)
            {
                var indexModifier = 0;
                //If we are currently flipping we need to make adjustments to the nextpageindex to conteract the cancelled
                if (instance.CurrentState == CurlPageController.State.Flipping || instance.CurrentState == CurlPageController.State.FlippingBack)
                {
                    var flippingDirectionChanged = CurlPageController.IsRightCorner(instance.corner) ^ toTheLeft;
                    var shouldModifyIndex = instance.CurrentState == CurlPageController.State.Flipping ? !flippingDirectionChanged : flippingDirectionChanged;
                    if (shouldModifyIndex)
                    {
                        indexModifier = CurlPageController.IsRightCorner(instance.corner) ? 1 : -1;
                    }
                }
                nextPageIndex = (toTheLeft ? instance.book.GetNextPageIndex() : instance.book.GetPreviousPageIndex()) + indexModifier;
                var pageCount = instance.book.GetPagesCount();
                nextPageIndex = (nextPageIndex + pageCount) % pageCount;
            }
            //Before we mess with indexes update the static bookmark to match this recipe
            SubRailService.UpdateStaticBookmark(nextPageIndex);
            var index = RecipeBookService.GetBookmarkStorageRecipeIndex(nextPageIndex);
            var allBookmarks = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList();
            var saved = SubRailService.GetSubRailRecipesForIndex(index).Select(s => new { savedBookmark = s, bookmark = allBookmarks[s.recipeIndex]}).ToList();
            //Check to see if we actually need to switch bookmarks on page turn or if we are in the same recipe group
            if (saved.Any(sb => StaticStorage.SubRail.railBookmarks.Any(rb => sb.bookmark == rb)))
            {
                Plugin.PluginLogger.LogInfo("MoveBookmarksToAndFromInvisiRail - Skipped populating bookmarks due to same recipe group");
                return;
            }

            var bookmarksToRemove = StaticStorage.SubRail.railBookmarks.ToList();
            StaticStorage.RemovingSubRailBookMarksForPageTurn = true;
            bookmarksToRemove.ForEach(b =>
            {
                var nextAvailSpace = GetNextInvisiRailSpace();
                if (nextAvailSpace == null)
                {
                    throw new Exception("PotionCraftBookmarkOrganizer - Somehow the InvisiRail ran out of space. All hope is lost!");
                }
                SubRailService.ConnectBookmarkToRail(StaticStorage.InvisiRail, b, nextAvailSpace.Value);
            });
            StaticStorage.RemovingSubRailBookMarksForPageTurn = false;

            StaticStorage.AddingSubRailBookMarksForPageTurn = true;

            saved.ForEach(savedBookmark =>
            {
                SubRailService.ConnectBookmarkToRail(StaticStorage.SubRail, savedBookmark.bookmark, savedBookmark.savedBookmark.SerializedBookmark.position);
            });
            StaticStorage.AddingSubRailBookMarksForPageTurn = false;
        }

        private static Vector2? GetNextInvisiRailSpace()
        {
            return SubRailService.GetNextEmptySpaceOnRail(StaticStorage.InvisiRail);
        }
    }
}
