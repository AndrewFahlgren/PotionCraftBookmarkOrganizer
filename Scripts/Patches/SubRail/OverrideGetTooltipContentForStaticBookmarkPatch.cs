using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using TooltipSystem;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Setup indexes for subbookmark and adjust all other indexes as needed
    /// If the first bookmark has changed update main bookmark with that icon
    /// </summary>
    public class OverrideGetTooltipContentForStaticBookmarkPatch
    { 
        [HarmonyPatch(typeof(RecipeBookBookmarkTooltipContentContainer), "GetTooltipContent")]
        public class RecipeBookBookmarkTooltipContentContainer_GetTooltipContent
        {
            static bool Prefix(ref TooltipContent __result, Bookmark ___bookmark)
            {
                return OverrideGetTooltipContentForStaticBookmark(ref __result, ___bookmark);
            }
        }

        private static bool OverrideGetTooltipContentForStaticBookmark(ref TooltipContent __result, Bookmark bookmark)
        {
            if (bookmark != StaticStorage.StaticBookmark) return true;
            var parentRecipeIndex = RecipeBookService.GetBookmarkStorageRecipeIndexForSelectedRecipe();
            __result = RecipeBook.Instance.savedRecipes[parentRecipeIndex]?.GetTooltipContentForRecipeBookBrewing(1, false);
            return false;
        }
    }
}
