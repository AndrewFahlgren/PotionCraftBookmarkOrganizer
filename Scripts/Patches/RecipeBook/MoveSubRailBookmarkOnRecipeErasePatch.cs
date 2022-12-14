using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class MoveSubRailBookmarkOnRecipeErasePatch
    { 
        [HarmonyPatch(typeof(RecipeBook), "EraseRecipe")]
        public class RecipeBook_EraseRecipe
        {
            static bool Prefix(RecipeBook __instance, Potion potion)
            {
                return Ex.RunSafe(() => MoveSubRailBookmarkOnRecipeErase(__instance, potion));
            }

            static void Postfix()
            {
                Ex.RunSafe(() => SubRailService.UpdateStaticBookmark());
            }
        }

        private static bool MoveSubRailBookmarkOnRecipeErase(RecipeBook instance, Potion potion)
        {
            var index = instance.savedRecipes.IndexOf(potion);
            var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(index, out bool indexIsParent);
            var isParentRecipe = RecipeBookService.IsBookmarkGroupParent(index);
            if (!indexIsParent && !isParentRecipe) return true;

            var controller = instance.bookmarkControllersGroupController.controllers.First().bookmarkController;


            //If this is a parent recipe we need to promote a new bookmark to be parent. Do this before connecting to avoid indexing nightmares.
            if (isParentRecipe)
            {
                var oldGroup = StaticStorage.BookmarkGroups[index];
                var newParent = oldGroup.OrderByDescending(b => b.SerializedBookmark.position.x).FirstOrDefault();
                RecipeBookService.PromoteIndexToParent(newParent.recipeIndex);
                index = newParent.recipeIndex;
            }

            instance.savedRecipes[index] = null;
            //This shouldn't happen but lets make sure
            if (instance.currentPotionRecipeIndex == index)
            {
                instance.currentPotionRecipeIndex = -1;
            }
            instance.UpdateBookmarkIcon(index);
            Managers.Potion.potionCraftPanel.UpdateSaveRecipeButton(true);
            Managers.Ingredient.alchemyMachine.finishLegendarySubstanceWindow.UpdateSaveProductRecipeButton();

            //Before messing with indexes erase the recipe from our bookmark groups dictionary
            StaticStorage.BookmarkGroups[groupIndex] = StaticStorage.BookmarkGroups[groupIndex].Where(b => b.recipeIndex != index).ToList();
            var groupBookmark = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetBookmarkByIndex(groupIndex);
            RecipeBookService.ShowHideGroupBookmarkIcon(groupBookmark, StaticStorage.BookmarkGroups[groupIndex].Any());

            //Before messing with indexes select naviage to the correct page (this shouldn't move any bookmarks because we are staying in the same group)
            Managers.Potion.recipeBook.UpdateCurrentPageIndex(groupIndex);
            SubRailService.UpdateNonSubBookmarksActiveState();

            //Move the now empty bookmark out of the sub group
            var spawnPosition = SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Large)
                                ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Medium)
                                ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Small)
                                ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Min);
            if (spawnPosition == null)
            {
                Plugin.PluginLogger.LogError("There is no empty space for bookmark! Change settings!");
                return false;
            }
            var bookmark = instance.bookmarkControllersGroupController.GetBookmarkByIndex(index);
            SubRailService.ConnectBookmarkToRail(spawnPosition.Item1, bookmark, spawnPosition.Item2);


            return false;
        }

        private static int GetNextNonEmptyIndex(RecipeBook instance, int index, int pagesCount, int nextIndex)
        {
            var currentPageIndex = instance.currentPageIndex;
            var flag = false;
            for (int index1 = 0; index1 < pagesCount; ++index1)
            {
                int index2 = (currentPageIndex + index1 * -1 + pagesCount) % pagesCount;
                var isEmptyPage = typeof(RecipeBook).GetMethod("IsEmptyPage", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!(bool)isEmptyPage.Invoke(instance, new object[] { index2 }))
                {
                    nextIndex = index2;
                    flag = true;
                    break;
                }
            }
            if (!flag) nextIndex = (currentPageIndex >= pagesCount - 1) ? index - 1 : index + 1;
            return nextIndex;
        }
    }
}
