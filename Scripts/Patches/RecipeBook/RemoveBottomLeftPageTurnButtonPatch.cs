using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using System;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class RemoveBottomLeftPageTurnButtonPatch
    { 
        [HarmonyPatch(typeof(Book), "Awake")]
        public class Book_Awake
        {
            static void Postfix(Book __instance)
            {
                Ex.RunSafe(() => RemoveBottomLeftPageTurnButton(__instance));
            }
        }

        private static void RemoveBottomLeftPageTurnButton(Book instance)
        {
            if (instance is not RecipeBook recipeBook) return;
            recipeBook.curlPageController.bottomLeftCorner.gameObject.SetActive(false);
        }
    }
}
