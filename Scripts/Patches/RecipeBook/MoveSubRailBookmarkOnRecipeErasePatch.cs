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
            static bool Prefix(RecipeBook __instance, ref IRecipeBookPageContent recipe)
            {
                return MoveSubRailBookmarkOnRecipeErase(__instance, ref recipe);
            }

            static void Postfix()
            {
                Ex.RunSafe(() => SubRailService.UpdateStaticBookmark());
            }
        }

        [HarmonyPatch(typeof(Book), "GoToTheFirstNotEmptyPage")]
        public class Book_GoToTheFirstNotEmptyPage
        {
            static bool Prefix(Book __instance)
            {
                return Ex.RunSafe(() => ShowPageAfterRecipeDelete(__instance));
            }
        }

        private static int? ShowPageAfterRecipeDeleteIndex;
        private static bool MoveSubRailBookmarkOnRecipeErase(RecipeBook instance, ref IRecipeBookPageContent potion) //TODO we need to tie into the get next non empty page method and redirect it to the proper bookmark
        {
            var localPotion = potion;
            Ex.RunSafe(() =>
            {
                var index = instance.savedRecipes.IndexOf(localPotion);
                var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(index, out bool indexIsParent);
                var isParentRecipe = RecipeBookService.IsBookmarkGroupParent(index);
                if (!indexIsParent && !isParentRecipe) return;

                ShowPageAfterRecipeDeleteIndex = groupIndex;

                if (!isParentRecipe) return;

                var controller = instance.bookmarkControllersGroupController.controllers.First().bookmarkController;

                var oldGroup = StaticStorage.BookmarkGroups[index];
                var newParent = oldGroup.OrderByDescending(b => instance.savedRecipes[b.recipeIndex] != null).ThenByDescending(b => b.SerializedBookmark.position.x).FirstOrDefault();
                index = newParent.recipeIndex;
                RecipeBookService.PromoteIndexToParent(newParent.recipeIndex);

                localPotion = RecipeBook.Instance.savedRecipes[newParent.recipeIndex] as Potion;
            });
            if (localPotion != potion) potion = localPotion;
            return true;
        }

        private static bool ShowPageAfterRecipeDelete(Book instance)
        {
            if (instance is not RecipeBook recipeBook) return true;
            if (ShowPageAfterRecipeDeleteIndex == null) return true;

            var nextIndex = ShowPageAfterRecipeDeleteIndex.Value;
            ShowPageAfterRecipeDeleteIndex = null;

            //If the group leader is empty then move to the next group
            if (recipeBook.savedRecipes[nextIndex] == null)
            {
                var pagesCount = recipeBook.savedRecipes.Count;
                for (var i = 0; i < pagesCount; ++i)
                {
                    var index2 = (nextIndex + i * -1 + pagesCount) % pagesCount;
                    RecipeBookService.GetBookmarkStorageRecipeIndex(index2, out var isSubBookmark);
                    if (recipeBook.savedRecipes[index2] != null && !isSubBookmark)
                    {
                        nextIndex = index2;
                        break;
                    }
                }
            }

            instance.UpdateCurrentPageIndex(nextIndex);

            return false;
        }
    }
}
