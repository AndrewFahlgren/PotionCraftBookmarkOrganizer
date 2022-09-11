using PotionCraft.Core.Extensions;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Cursor;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftBookmarkOrganizer.Scripts.Services
{
    /// <summary>
    /// Service responsible for 
    /// </summary>
    public static class RecipeBookService
    {
        public static void SetupListeners()
        {
            if (!StaticStorage.AddedListeners)
            {
                StaticStorage.AddedListeners = true;
                Managers.Potion.recipeBook.bookmarkControllersGroupController.onBookmarksRearranged.AddListener(BookmarksRearranged);
                Managers.Potion.gameObject.AddComponent<BookmarkOrganizerManager>();
                var asset = PotionCraft.Settings.Settings<CursorManagerSettings>.Asset;
                StaticStorage.HotkeyUp = CommandInvokeRepeater.GetNewCommandInvokeRepeater(asset.invokeRepeaterSettings, new List<Command>()
                {
                  Commands.roomUp
                });
                StaticStorage.HotkeyDown = CommandInvokeRepeater.GetNewCommandInvokeRepeater(asset.invokeRepeaterSettings, new List<Command>()
                {
                  Commands.roomDown
                });
            }
        }

        public static void FlipPageToNextGroup(bool flipForward)
        {
            if (!Managers.Potion.recipeBook.isActiveAndEnabled) return;
            var nextGroupIndex = GetNextNonSubRecipeIndex(flipForward);
            if (nextGroupIndex == Managers.Potion.recipeBook.currentPageIndex) return;
            FlipPageToIndex(nextGroupIndex);
        }

        private static int GetNextNonSubRecipeIndex(bool moveForward)
        {
            var recipeBook = Managers.Potion.recipeBook;
            var currentPageIndex = recipeBook.currentPageIndex;
            var pagesCount = recipeBook.GetPagesCount();
            var nextIndex = currentPageIndex;
            //Start the index at one so we don't consider the current recipe when checking for the next non sub recipe
            for (var i = 1; i <= pagesCount; ++i)
            {
                var indexOffset = i * (moveForward ? 1 : -1);
                var actualIndex = (currentPageIndex + indexOffset + pagesCount) % pagesCount;
                Plugin.PluginLogger.LogInfo($"GetNextNonSubRecipeIndex - actualIndex: {actualIndex} - currentPageIndex: {currentPageIndex}");
                GetBookmarkStorageRecipeIndex(actualIndex, out bool indexIsparent);
                if (!indexIsparent)
                {
                    nextIndex = actualIndex;
                    break;
                }
            }
            Plugin.PluginLogger.LogInfo($"GetNextNonSubRecipeIndex - nextIndex: {nextIndex} - currentPageIndex: {currentPageIndex}");
            return nextIndex;
        }

        private static bool currentlyRearranging;
        private static ConcurrentQueue<List<int>> rearrangeQueue = new ConcurrentQueue<List<int>>();
        private static async void BookmarksRearranged(BookmarkController bookmarkController, List<int> intList)
        {
            rearrangeQueue.Enqueue(intList);
            if (currentlyRearranging)
            {
                Plugin.PluginLogger.LogInfo("BookmarksRearranged - ForcedToQueue");
                while (currentlyRearranging)
                {
                    await Task.Delay(100);
                }
            }
            BookmarksRearranged();
        }

        //TODO for some reason this is rearranging wrong. Lets put in debug statemtns here. Is this a result of the save changes or something that has been wrong for a long time?
        private static async void BookmarksRearranged()
        {
            Plugin.PluginLogger.LogInfo("BookmarksRearranged");
            currentlyRearranging = true;
            try
            {
                while (rearrangeQueue.Count > 0)
                {
                    var didDequeue = rearrangeQueue.TryDequeue(out List<int> intList);
                    while (!didDequeue)
                    {
                        await Task.Delay(10);
                        didDequeue = rearrangeQueue.TryDequeue(out List<int> temp);
                        if (didDequeue) intList = temp;
                    }
                    var oldBookmarks = StaticStorage.BookmarkGroups;
                    var newBookmarks = new Dictionary<int, List<BookmarkStorage>>();
                    var oldStoredBookmarks = oldBookmarks.SelectMany(b => b.Value).ToDictionary(b => b.recipeIndex);
                    for (var newIndex = 0; newIndex < intList.Count; newIndex++)
                    {
                        var oldIndex = intList[newIndex];
                        //We could possibly be waiting on this rearrange before storing sub recipes for this page. If that is the case we need to keep this index in sync with the rearrange.
                        if (recipeIndexForCurrentGroupUpdate == oldIndex) recipeIndexForCurrentGroupUpdate = newIndex;
                        //This will recreate the old bookmark dictionary making sure to update any indexes along the way
                        if (oldBookmarks.ContainsKey(oldIndex)) newBookmarks[newIndex] = oldBookmarks[oldIndex];
                        if (oldIndex == newIndex) continue;
                        if (!oldStoredBookmarks.TryGetValue(oldIndex, out BookmarkStorage affectedBookmark)) continue;
                        affectedBookmark.recipeIndex = newIndex;
                    }
                    StaticStorage.BookmarkGroups = newBookmarks;
                }
            }
            catch (Exception ex)
            {
                Ex.LogException(ex);
            }
            currentlyRearranging = false;
        }

        private static int recipeIndexForCurrentGroupUpdate = -1;
        public static async void UpdateBookmarkGroupsForCurrentRecipe()
        {
            recipeIndexForCurrentGroupUpdate = GetBookmarkStorageRecipeIndexForSelectedRecipe();
            var subRailBookmarks = StaticStorage.SubRail.railBookmarks.Select(b => new { bookmark = b, serialized = b.GetSerialized() }).ToList();
            if (currentlyRearranging || rearrangeQueue.Count > 0)
            {
                Plugin.PluginLogger.LogInfo("UpdateBookmarkGroupsForCurrentRecipe - ForcedToQueue");
                while (currentlyRearranging || rearrangeQueue.Count > 0)
                {
                    await Task.Delay(100);
                }
            }
            currentlyRearranging = true;

            var allBookmarks = Managers.Potion.recipeBook.bookmarkControllersGroupController.GetAllBookmarksList();
            var bookmarks = subRailBookmarks.Select(bookmark =>
            {
                return new BookmarkStorage
                {
                    recipeIndex = allBookmarks.IndexOf(bookmark.bookmark),
                    SerializedBookmark = bookmark.serialized
                };
            });
            StaticStorage.BookmarkGroups[recipeIndexForCurrentGroupUpdate] = bookmarks.ToList();
            currentlyRearranging = false;
        }

        public static int GetBookmarkStorageRecipeIndexForSelectedRecipe()
        {
            return GetBookmarkStorageRecipeIndex(Managers.Potion.recipeBook.currentPageIndex, out bool _);
        }

        public static int GetBookmarkStorageRecipeIndexForSelectedRecipe(out bool indexIsParent)
        {
            return GetBookmarkStorageRecipeIndex(Managers.Potion.recipeBook.currentPageIndex, out indexIsParent);
        }

        public static int GetBookmarkStorageRecipeIndex(int recipeIndex)
        {
            return GetBookmarkStorageRecipeIndex(recipeIndex, out bool _);
        }

        public static int GetBookmarkStorageRecipeIndex(int recipeIndex, out bool indexIsParent)
        {
            indexIsParent = false;
            var storedParentIndex = StaticStorage.BookmarkGroups.Keys.Where(k => StaticStorage.BookmarkGroups[k].Any(b => b.recipeIndex == recipeIndex)).ToList();
            if (storedParentIndex.Any())
            {
                recipeIndex = storedParentIndex.First();
                indexIsParent = true;
            }
            return recipeIndex;
        }

        public static bool IsBookmarkGroupParent(int index)
        {
            if (!StaticStorage.BookmarkGroups.TryGetValue(index, out List<BookmarkStorage> subBookmarks)) return false;
            return subBookmarks.Count > 0;
        }

        public static void FlipPageToIndex(int nextIndex)
        {
            if (Managers.Cursor.grabbedInteractiveItem is InactiveBookmarkButton) return;
            var recipeBook = Managers.Potion.recipeBook;
            var pagesCount = recipeBook.GetPagesCount();
            recipeBook.curlPageController.HotkeyClicked(nextIndex > recipeBook.currentPageIndex
                                                            ? recipeBook.currentPageIndex.Distance(nextIndex) <= nextIndex.Distance(recipeBook.currentPageIndex + pagesCount)
                                                            : recipeBook.currentPageIndex.Distance(nextIndex) >= recipeBook.currentPageIndex.Distance(nextIndex + pagesCount)
                                                        , nextPageIndex: nextIndex);
        }
    }
}
