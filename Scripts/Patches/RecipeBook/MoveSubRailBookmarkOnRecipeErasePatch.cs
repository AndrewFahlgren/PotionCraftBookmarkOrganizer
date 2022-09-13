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
            RecipeBookService.GetBookmarkStorageRecipeIndex(index, out bool indexIsParent);
            var isParentRecipe = RecipeBookService.IsBookmarkGroupParent(index);
            if (!indexIsParent && !isParentRecipe) return true;

            var controller = instance.bookmarkControllersGroupController.controllers.First().bookmarkController;
            var spawnPosition = SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Large)
                                ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Medium)
                                ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Small)
                                ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Min);
            if (spawnPosition == null)
            {
                Plugin.PluginLogger.LogInfo("There is no empty space for bookmark! Change settings!");
                return false;
            }


            //If this is a parent recipe we need to promote a new bookmark to be parent. Do this before connecting to avoid indexing nightmares.
            Bookmark newParentBookmark = null;
            if (isParentRecipe)
            {
                var oldGroup = StaticStorage.BookmarkGroups[index];
                var newParent = oldGroup.OrderByDescending(b => b.SerializedBookmark.position.x).First();
                oldGroup.Remove(newParent);
                StaticStorage.BookmarkGroups.Remove(index);
                StaticStorage.BookmarkGroups[newParent.recipeIndex] = oldGroup;
                newParentBookmark = instance.bookmarkControllersGroupController.GetBookmarkByIndex(newParent.recipeIndex);
            }


            var bookmark = instance.bookmarkControllersGroupController.GetBookmarkByIndex(index);
            SubRailService.ConnectBookmarkToRail(spawnPosition.Item1, bookmark, spawnPosition.Item2);

            var pagesCount = instance.GetPagesCount();
            int nextIndex = -1;
            if (isParentRecipe)
            {
                nextIndex = instance.bookmarkControllersGroupController.GetAllBookmarksList().IndexOf(newParentBookmark);
            }
            else
            {
                nextIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(index, out bool _);
                if (nextIndex == index)
                {
                    nextIndex = GetNextNonEmptyIndex(instance, index, pagesCount, nextIndex);
                }
            }

            //Flip the page to the proper recipe book page
            RecipeBookService.FlipPageToIndex(nextIndex);

            //Actually erase the recipe
            index = instance.bookmarkControllersGroupController.GetAllBookmarksList().IndexOf(bookmark);
            instance.savedRecipes[index] = null;
            if (instance.currentPotionRecipeIndex == index)
            {
                instance.currentPotionRecipeIndex = -1;
            }
            instance.UpdateBookmarkIcon(index);
            Managers.Potion.potionCraftPanel.UpdateSaveRecipeButton(true);
            Managers.Ingredient.alchemyMachine.finishLegendarySubstanceWindow.UpdateSaveProductRecipeButton();

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
