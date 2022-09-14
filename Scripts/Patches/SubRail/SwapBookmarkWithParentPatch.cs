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
    public class SwapBookmarkWithParentPatch
    {
        private const float SwapAboveY = 1.2f;
        private const float DontSwapBelowX = -0.6f;
        private const float SwapPreviewAlpha = 0.5f;

        private static bool ShouldSwapOnRelease;
        private static Vector2 SavedStaticBookmarkPosition;
        private static Vector2 SavedBookmarkPosition;

        [HarmonyPatch(typeof(Bookmark), "UpdateMovingState")]
        public class Bookmark_UpdateMovingState
        {
            static void Postfix(Bookmark __instance, Bookmark.MovingState value)
            {
                Ex.RunSafe(() => BookmarkMovingStateChanged(__instance, value));
            }
        }

        [HarmonyPatch(typeof(Bookmark), "UpdateMoving")]
        public class Bookmark_UpdateMoving
        {
            static void Postfix(Bookmark __instance)
            {
                Ex.RunSafe(() => UpdatePreviewForNewBookmarkLocation(__instance));
            }
        }

        private static void BookmarkMovingStateChanged(Bookmark instance, Bookmark.MovingState value)
        {
            switch (value)
            {
                case Bookmark.MovingState.Idle:
                    if (!ShouldSwapOnRelease) break;
                    //Get rid of our preview
                    ShouldSwapOnRelease = false;
                    UpdateSwapPreview(instance);
                    //Reuse the old bookmark for the old group leader
                    instance.SetPosition(SavedBookmarkPosition);
                    //Promote the sub bookmark to parent and update the static bookmark
                    RecipeBookService.PromoteIndexToParent(instance.rail.bookmarkController.GetAllBookmarksList().IndexOf(instance));
                    break;
                case Bookmark.MovingState.Moving:
                    SavedBookmarkPosition = instance.GetNormalizedPosition();
                    break;
            }
        }

        private static void UpdatePreviewForNewBookmarkLocation(Bookmark instance)
        {
            if (instance.CurrentMovingState == Bookmark.MovingState.Idle) return;
            if (instance.rail != StaticStorage.SubRail)
            {
                if (ShouldSwapOnRelease)
                {
                    ShouldSwapOnRelease = false;
                    UpdateSwapPreview(instance);
                }
            }
            //Empty bookmarks should never be able to swap with the static bookmark
            if (instance.activeBookmarkButton.normalSpriteIcon == null) return;
            var index = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList().IndexOf(instance);
            var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(index, out bool indexIsParent);
            //Do not allow swaps from outside the group
            if (groupIndex != RecipeBookService.GetBookmarkStorageRecipeIndexForSelectedRecipe()) return;
            //Only allow swaps for sub bookmarks
            if (!indexIsParent) return;
            if (instance.CurrentMovingState == Bookmark.MovingState.Idle) return;
            var mouseWorldPosition = Managers.Input.controlsProvider.CurrentMouseWorldPosition;
            var swapAboveYWorld = StaticStorage.SubRail.transform.position.y + SwapAboveY;
            var dontSwapBelowXWorld = StaticStorage.SubRail.transform.position.x - DontSwapBelowX;
            var oldShouldSwap = ShouldSwapOnRelease;
            ShouldSwapOnRelease = mouseWorldPosition.y > swapAboveYWorld && mouseWorldPosition.x > dontSwapBelowXWorld;
            if (oldShouldSwap != ShouldSwapOnRelease) UpdateSwapPreview(instance);
            UpdatePositionForSwapPreview(instance);
        }

        private static void UpdatePositionForSwapPreview(Bookmark instance)
        {
            if (!ShouldSwapOnRelease) return;
            instance.SetPosition(SavedStaticBookmarkPosition);
        }

        private static void UpdateSwapPreview(Bookmark instance)
        {
            if (ShouldSwapOnRelease)
            {
                SavedStaticBookmarkPosition = StaticStorage.StaticBookmark.GetNormalizedPosition();
                StaticStorage.StaticBookmark.SetPosition(SavedBookmarkPosition);
                SetGameObjectAlpha(StaticStorage.StaticBookmark.gameObject, SwapPreviewAlpha);
                SetGameObjectAlpha(instance.gameObject, SwapPreviewAlpha);
            }
            else
            {
                StaticStorage.StaticBookmark.SetPosition(SavedStaticBookmarkPosition);
                SetGameObjectAlpha(StaticStorage.StaticBookmark.gameObject, 1f);
                SetGameObjectAlpha(instance.gameObject, 1f);
            }
        }

        private static void SetGameObjectAlpha(GameObject gameObject, float alpha)
        {
            var color = new Color(1, 1, 1, alpha);
            gameObject.GetComponentsInChildren<SpriteRenderer>().ToList().ForEach(r => r.color = color);
        }
    }
}
