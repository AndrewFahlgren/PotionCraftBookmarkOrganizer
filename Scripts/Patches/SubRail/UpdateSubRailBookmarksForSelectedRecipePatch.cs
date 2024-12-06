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
        /// <summary>
        /// This is only used when adding recipes or deleting recipes
        /// </summary>
        [HarmonyPatch(typeof(Book), "UpdateCurrentPageIndex")]
        public class Book_UpdateCurrentPageIndex
        {
            static void Postfix(Book __instance, int index)
            {
                Ex.RunSafe(() => UpdateCurrentPageIndex(__instance, index));
            }
        }

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
                Ex.RunSafe(() => UpdateCurrentPageIndex(__instance, pageIndex));
            }
        }

        private static void UpdateCurrentPageIndex(Book instance, int index)
        {
            if (instance is not RecipeBook) return;
            SubRailService.UpdateSubRailForSelectedIndex(index);
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
            //Hide the static rails and show the real sub rail
            SubRailService.ShowSubRailAfterFlip();
            var recipeBook = RecipeBook.Instance;
            //Fix the subrail for a cancelled page turn with the current index
            SubRailService.UpdateSubRailForSelectedIndex(recipeBook.currentPageIndex);
        }

        private static void MoveBookmarksToAndFromInvisiRail(CurlPageController instance, int nextPageIndex, bool showStaticRails = true)
        {
            if (instance.book != RecipeBook.Instance) return;

            if (showStaticRails)
            {
                //First copy over current rail to the current page's static rail
                var page = instance.frontLeftPage.pageContent as RecipeBookLeftPageContent;
                CopyBookmarksToStaticRailAndShow(page);
            }
            SubRailService.UpdateSubRailForSelectedIndex(nextPageIndex);

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
    }
}
