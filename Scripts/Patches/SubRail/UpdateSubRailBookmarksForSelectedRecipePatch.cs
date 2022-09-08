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
        [HarmonyPatch(typeof(CurlPageController), "OnPageFlippingEnd")]
        public class CurlPageController_OnPageFlippingEnd
        {
            static bool Prefix()
            {
                return Ex.RunSafe(() => UpdateBookmarkGroupsForCurrentRecipe());
            }

            static void Postfix(CurlPageController __instance)
            {
                Ex.RunSafe(() => UpdateSubRailBookmarksForSelectedRecipe(__instance));
            }
        }


        private static bool UpdateBookmarkGroupsForCurrentRecipe()
        {
            RecipeBookService.UpdateBookmarkGroupsForCurrentRecipe();
            return true;
        }


        private static void UpdateSubRailBookmarksForSelectedRecipe(CurlPageController instance)
        {
            if (instance.book != Managers.Potion.recipeBook) return;
            MoveBookmarksToAndFromInvisiRail();
            SubRailService.UpdateStaticBookmark();
        }

        private static void MoveBookmarksToAndFromInvisiRail()
        {
            var saved = SubRailService.GetSubRailRecipesForSelectedIndex();
            var allBookmarks = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList();
            //Check to see if we actually need to switch bookmarks on page turn or if we are in the same recipe group
            if (saved.Any(sb => StaticStorage.SubRail.railBookmarks.Any(rb => sb.recipeIndex == allBookmarks.IndexOf(rb)))) return;

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
                //Each time we connect a bookmark to a rail we are scrambling the indexes. It is important to grab the up to date indexes from the dictionary at each step
                var curSavedBookmark = StaticStorage.BookmarkGroups[Managers.Potion.recipeBook.currentPageIndex].First(b => b.SerializedBookmark.position == savedBookmark.SerializedBookmark.position);
                var bookmark = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetBookmarkByIndex(curSavedBookmark.recipeIndex);
                SubRailService.ConnectBookmarkToRail(StaticStorage.SubRail, bookmark, curSavedBookmark.SerializedBookmark.position);
            });
            StaticStorage.AddingSubRailBookMarksForPageTurn = false;
        }

        private static Vector2? GetNextInvisiRailSpace()
        {
            return SubRailService.GetNextEmptySpaceOnRail(StaticStorage.InvisiRail);
        }
    }
}
