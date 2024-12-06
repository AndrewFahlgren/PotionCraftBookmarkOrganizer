using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
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
            EnsureSubBookmarksAreShowingFailsafe();
        }

        private static void EnsureSubBookmarksAreShowingFailsafe()
        {
            var pageIndex = RecipeBook.Instance.currentPageIndex;
            var groupIndex = RecipeBookService.GetBookmarkStorageRecipeIndex(pageIndex);
            var allBookmarks = RecipeBook.Instance.bookmarkControllersGroupController.GetAllBookmarksList();
            var saved = SubRailService.GetSubRailRecipesForIndex(groupIndex).Select(s => new { savedBookmark = s, bookmark = allBookmarks[s.recipeIndex], isActive = pageIndex == s.recipeIndex }).ToList();
            var missingBookmarks = saved.Where(sb => StaticStorage.SubRail.railBookmarks.All(rb => sb.bookmark != rb)).ToList();

            //Check for missing bookmarks
            if (!missingBookmarks.Any()) return;

            Plugin.PluginLogger.LogError($"ERROR:  Not all bookmarks are showing on subrail on load! Adding missing bookmarks to subrail.");

            missingBookmarks.ForEach(savedBookmark =>
            {
                Plugin.PluginLogger.LogMessage("Adding bookmark");
                SubRailService.ConnectBookmarkToRail(StaticStorage.SubRail, savedBookmark.bookmark, savedBookmark.savedBookmark.SerializedBookmark.position);
                savedBookmark.bookmark.CurrentVisualState = savedBookmark.isActive ? Bookmark.VisualState.Active : Bookmark.VisualState.Inactive;
            });
        }
    }
}
