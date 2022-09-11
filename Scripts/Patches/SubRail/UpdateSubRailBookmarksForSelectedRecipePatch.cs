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
        [HarmonyPatch(typeof(CurlPageController), "OnCornerReleased")]
        public class CurlPageController_OnCornerReleased
        {
            static bool Prefix(CurlPageController __instance, CurlPageCornerController releasedCorner)
            {
                return Ex.RunSafe(() => CleanupAfterHoveredPageButton(__instance, releasedCorner));
            }
        }

        [HarmonyPatch(typeof(CurlPageController), "SetCornerHovered")]
        public class CurlPageController_SetCornerHovered
        {
            static void Postfix(bool hovered)
            {
                Ex.RunSafe(() => CleanupAfterHoveredPageButton(hovered));
            }
        }

        [HarmonyPatch(typeof(Book), "UpdateBackPage")]
        public class Book_UpdateBackPage
        {
            static void Postfix(Book __instance, int backPageIndex)
            {
                Ex.RunSafe(() => MoveBookmarksToAndFromInvisiRail(__instance.curlPageController, backPageIndex));
            }
        }

        /// <summary>
        /// This patch is only useful if Recipe Waypoints is installed. 
        /// Normally this method is not used for the recipe book but since it is a really simple way to go to a certain page it was used for Recipe Waypoints.
        /// </summary>
        [HarmonyPatch(typeof(Book), "OpenPageAt")]
        public class Book_OpenPageAt
        {
            static void Postfix(Book __instance, int pageIndex)
            {
                Ex.RunSafe(() => MoveBookmarksToAndFromInvisiRail(__instance.curlPageController, pageIndex, false));
            }
        }

        private static bool CleanupAfterHoveredPageButton(CurlPageController instance, CurlPageCornerController releasedCorner)
        {
            //If the page has been clicked and dragged we but the page was not pulled far enough for a page flip we need to clean up here
            if (instance.HasPageGotToBeFlippedOnRelease(releasedCorner.corner)) return true;
            if (releasedCorner.clickGrabHandler.ClickCondition()) return true;
            CleanupAfterHoveredPageButton();
            return true;
        }

        private static bool CleanupAfterHoveredPageButton(bool hovered)
        {
            //If the page was only hovered this method will be called with a hovered set to false
            if (hovered) return true;
            CleanupAfterHoveredPageButton();
            return true;
        }

        private static void CleanupAfterHoveredPageButton()
        {
            Plugin.PluginLogger.LogInfo("CleanupAfterHoveredPageButton");
            //Hide the static rails and show the real sub rail
            SubRailService.ShowSubRailAfterFlip();
            var recipeBook = Managers.Potion.recipeBook;
            //Fix the subrail for a cancelled page turn with the current index
            MoveBookmarksToAndFromInvisiRail(recipeBook.curlPageController, recipeBook.currentPageIndex, false);
        }

        private static void MoveBookmarksToAndFromInvisiRail(CurlPageController instance, int nextPageIndex, bool showStaticRails = true)
        {
            if (instance.book != Managers.Potion.recipeBook) return;

            if (showStaticRails)
            {
                //First copy over current rail to the current page's static rail
                var page = instance.frontLeftPage.pageContent as RecipeBookLeftPageContent;
                CopyBookmarksToStaticRailAndShow(page);
            }
            //Before we mess with indexes update the static bookmark to match this recipe
            SubRailService.UpdateStaticBookmark(nextPageIndex);
            var index = RecipeBookService.GetBookmarkStorageRecipeIndex(nextPageIndex);
            var allBookmarks = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList();
            var saved = SubRailService.GetSubRailRecipesForIndex(index).Select(s => new { savedBookmark = s, bookmark = allBookmarks[s.recipeIndex], isActive = nextPageIndex == s.recipeIndex}).ToList();
            //Check to see if we actually need to switch bookmarks on page turn or if we are in the same recipe group
            if (saved.Any(sb => StaticStorage.SubRail.railBookmarks.Any(rb => sb.bookmark == rb)))
            {
                saved.ForEach(savedBookmark =>
                {
                    savedBookmark.bookmark.CurrentVisualState = savedBookmark.isActive ? Bookmark.VisualState.Active : Bookmark.VisualState.Inactive;
                });
                //Copy bookmarks to static subrail
                if (showStaticRails) UpdateBackPageForFlipAndShow(instance);
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
                savedBookmark.bookmark.CurrentVisualState = savedBookmark.isActive ? Bookmark.VisualState.Active : Bookmark.VisualState.Inactive;
            });
            StaticStorage.AddingSubRailBookMarksForPageTurn = false;

            //Copy bookmarks to static subrail
            if (showStaticRails) UpdateBackPageForFlipAndShow(instance);
        }

        private static void UpdateBackPageForFlipAndShow(CurlPageController instance)
        {
            CopyBookmarksToStaticRailAndShow(instance.backLeftPage.pageContent as RecipeBookLeftPageContent);
            StaticStorage.SubRail.gameObject.SetActive(false);
            StaticStorage.SubRailPages.gameObject.SetActive(false);
        }

        private static void CopyBookmarksToStaticRailAndShow(RecipeBookLeftPageContent page)
        {
            var containers = StaticStorage.StaticRails[page];

            //Remove old copied bookmarks
            foreach (Transform child in containers.Item1.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            //Add current bookmarks
            UnityEngine.Object.Instantiate(StaticStorage.SubRail.bookmarksContainer, containers.Item1.transform);

            containers.Item1.SetActive(true);
            containers.Item2.SetActive(true);
        }

        private static Vector2? GetNextInvisiRailSpace()
        {
            return SubRailService.GetNextEmptySpaceOnRail(StaticStorage.InvisiRail);
        }
    }
}
