using HarmonyLib;
using PotionCraft.Core.Extensions;
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
    public class DisableMovingBookmarkToOwnSubRailPatch
    { 
        [HarmonyPatch(typeof(BookmarkRail), "Connect")]
        public class BookmarkRail_Connect
        {
            static bool Prefix(BookmarkRail __instance, Bookmark bookmark)
            {
                return Ex.RunSafe(() => DisableMovingBookmarkToOwnSubRail(__instance, bookmark));
            }
        }

        private static bool DisableMovingBookmarkToOwnSubRail(BookmarkRail instance, Bookmark bookmark)
        {
            if (!StaticStorage.IsLoaded) return true;
            if (instance != StaticStorage.SubRail) return true;
            if (bookmark.CurrentMovingState == Bookmark.MovingState.Idle) return true;
            var bookmarksListBeforeMoving = typeof(Bookmark).GetField("bookmarksListBeforeMoving", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(bookmark) as List<Bookmark>;
            var bookmarkIndex = bookmarksListBeforeMoving.IndexOf(bookmark);

            var isParent = RecipeBookService.IsBookmarkGroupParent(bookmarkIndex);
            //We need to prevent bookmarks being dragged onto their own subrails and we need to prevent other recipe groups from being dragged onto a subrail at all
            if (bookmarkIndex != Managers.Potion.recipeBook.currentPageIndex && !isParent) return true;

            var mouseWorldPosition = Managers.Input.controlsProvider.CurrentMouseWorldPosition;
            var newRail = instance.bookmarkController.rails.Except(new[] { instance })
                                                           .OrderBy(bRail => (mouseWorldPosition - (Vector2)bRail.transform.position).GetDistanceToLineSegment(bRail.bottomLine.Item1, bRail.bottomLine.Item2))
                                                           .First();
            var emptySegmentsForMoving = typeof(Bookmark).GetField("emptySegmentsForMoving", BindingFlags.NonPublic | BindingFlags.Instance)
                                                         .GetValue(bookmark) as Dictionary<BookmarkRail, List<MinMaxFloat>>;
            if (newRail == bookmark.rail || emptySegmentsForMoving[newRail].Count == 0) return false;
            newRail.Connect(bookmark, newRail.GetMovingToNormalizedPosition(emptySegmentsForMoving[newRail], bookmark));

            return false;
        }
    }
}
