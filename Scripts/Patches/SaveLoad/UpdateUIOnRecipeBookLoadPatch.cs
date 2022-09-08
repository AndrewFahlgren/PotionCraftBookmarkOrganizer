using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class UpdateUIOnRecipeBookLoadPatch
    { 
        [HarmonyPatch(typeof(RecipeBook), "OnLoad")]
        public class RecipeBook_OnLoad
        {
            //static bool Prefix()
            //{
            //    return Ex.RunSafe(() => true);
            //}
            static void Postfix()
            {
                Ex.RunSafe(() => UpdateUIOnRecipeBookLoad());
            }
        }

        private static void UpdateUIOnRecipeBookLoad()
        {
            StaticStorage.IsLoaded = true; //TODO if this ends up being the solution we need to manipulate this flag in a way where it is false during load/reload and only true once everyhting is loaded

            SubRailService.UpdateStaticBookmark();
        }
    }
}
