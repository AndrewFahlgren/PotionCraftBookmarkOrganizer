using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Setup indexes for subbookmark and adjust all other indexes as needed
    /// If the first bookmark has changed update main bookmark with that icon
    /// </summary>
    public class OpenParentRecipeOnInactiveStaticBookmarkClickPatch
    {
        [HarmonyPatch(typeof(InactiveBookmarkButton), "OnReleasePrimary")]
        public class InactiveBookmarkButton_OnGrabPrimary
        {
            static bool Prefix(InactiveBookmarkButton __instance)
            {
                return Ex.RunSafe(() => DisableGrabbingForInactiveStaticBookmark(__instance));
            }
        }

        private static bool DisableGrabbingForInactiveStaticBookmark(InactiveBookmarkButton instance)
        {
            var bookmark = typeof(InactiveBookmarkButton).GetField("bookmark", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as Bookmark;
            if (bookmark != StaticStorage.StaticBookmark) return true;
            //Set the parent recipe of this group to be active
            var recipeIndex = RecipeBookService.GetBookmarkStorageRecipeIndexForSelectedRecipe();
            var parentBookmark = bookmark.rail.bookmarkController.GetBookmarkByIndex(recipeIndex);
            bookmark.rail.bookmarkController.bookmarkControllersGroupController.SetActiveBookmark(parentBookmark);

            //update the visual state of the static bookmark
            bookmark.CurrentVisualState = Bookmark.VisualState.Active;
            bookmark.activeBookmarkButton.slot.Selected = true;
            if (bookmark.activeBookmarkButton.thisCollider.bounds.Contains(Managers.Input.controlsProvider.CurrentMouseWorldPosition))
                bookmark.activeBookmarkButton.SetHovered(true);

            typeof(InactiveBookmarkButton).GetField("grabConditionChecked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, true);

            return false;
        }
    }
}
