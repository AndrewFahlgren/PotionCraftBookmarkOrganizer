using PotionCraft.Core.Extensions;
using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Cursor;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                RecipeBook.Instance.bookmarkControllersGroupController.onBookmarksRearranged.AddListener(BookmarksRearranged);
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
            if (!RecipeBook.Instance.isActiveAndEnabled) return;
            var nextGroupIndex = GetNextNonSubRecipeIndex(flipForward);
            if (nextGroupIndex == RecipeBook.Instance.currentPageIndex) return;
            FlipPageToIndex(nextGroupIndex);
        }

        private static int GetNextNonSubRecipeIndex(bool moveForward)
        {
            var recipeBook = RecipeBook.Instance;
            var currentPageIndex = recipeBook.currentPageIndex;
            var pagesCount = recipeBook.GetPagesCount();
            var nextIndex = currentPageIndex;
            //Start the index at one so we don't consider the current recipe when checking for the next non sub recipe
            for (var i = 1; i <= pagesCount; ++i)
            {
                var indexOffset = i * (moveForward ? 1 : -1);
                var actualIndex = (currentPageIndex + indexOffset + pagesCount) % pagesCount;
                GetBookmarkStorageRecipeIndex(actualIndex, out bool indexIsparent);
                if (!indexIsparent)
                {
                    nextIndex = actualIndex;
                    break;
                }
            }
            return nextIndex;
        }

        private static bool ignoreBookmarkRearrangeForSubBookmarkPromotion;
        public static void PromoteIndexToParent(int subBookmarkIndex)
        {
            var groupIndex = GetBookmarkStorageRecipeIndex(subBookmarkIndex, out bool indexIsParent);
            if (!indexIsParent) return;
            var bookmarkList = RecipeBook.Instance.bookmarkControllersGroupController.GetAllBookmarksList();
            var subBookmark = bookmarkList[subBookmarkIndex];
            var groupBookmark = bookmarkList[groupIndex];
            var groupBookmarkPosition = groupBookmark.GetNormalizedPosition();
            var subBookmarkPosition = subBookmark.GetNormalizedPosition();

            var bookmarkController = RecipeBook.Instance.bookmarkControllersGroupController.controllers[0].bookmarkController;
            var i = 0;
            var railList = bookmarkController.rails.ToList().Select(rail => (rail, bookmarks: rail.railBookmarks.ToList())).ToList();
            railList.ForEach(rail =>
            {
                rail.bookmarks.ForEach(bookmark =>
                {
                    if (i == groupIndex)
                        rail.rail.Connect(subBookmark, groupBookmarkPosition);
                    else if (i == subBookmarkIndex)
                        rail.rail.Connect(groupBookmark, subBookmarkPosition);
                    i++;
                });
            });

            ignoreBookmarkRearrangeForSubBookmarkPromotion = true;
            bookmarkController.CallOnBookmarksRearrangeIfNecessary(bookmarkList);
            ignoreBookmarkRearrangeForSubBookmarkPromotion = false;

            RecipeBook.Instance.UpdateBookmarkIcon(groupIndex);
            RecipeBook.Instance.UpdateBookmarkIcon(subBookmarkIndex);
            SubRailService.UpdateStaticBookmark();
            UpdateBookmarkGroupsForCurrentRecipe();
            ShowHideGroupBookmarkIcon(groupBookmark, false);
            SubRailService.UpdateSubBookmarksActiveState();
        }

        //This flag and queue appear to never matter since everything occurs synchronously. However the code has been tested with this from the beginning so leave it in for now as a failsafe.
        private static bool currentlyRearranging;
        private static ConcurrentQueue<List<int>> rearrangeQueue = new ConcurrentQueue<List<int>>();
        private static async void BookmarksRearranged(BookmarkController bookmarkController, List<int> intList)
        {
            if (ignoreBookmarkRearrangeForSubBookmarkPromotion) return;
            rearrangeQueue.Enqueue(intList);
            if (currentlyRearranging)
            {
                while (currentlyRearranging)
                {
                    await Task.Delay(100);
                }
            }
            BookmarksRearranged();
        }

        private static async void BookmarksRearranged()
        {
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
                    var oldStoredBookmarksList = oldBookmarks.SelectMany(b => b.Value).ToList();
                    var duplicateSubBookmarks = oldStoredBookmarksList.GroupBy(b => b.recipeIndex).Where(bg => bg.Count() > 1).ToList();
                    if (duplicateSubBookmarks.Any())
                    {
                        //This should never happen but this is a common error case when indexing bugs are afoot. If there is a bug this should help to fix the issue without orphaning recipes.
                        Plugin.PluginLogger.LogError("ERROR: somehow the same bookmark is in two different groups!");
                        duplicateSubBookmarks.ForEach(dbg => oldStoredBookmarksList.Remove(dbg.Last()));
                    }
                    var oldStoredBookmarks = oldStoredBookmarksList.ToDictionary(b => b.recipeIndex);
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
                DoOrphanedBookmarkFailsafe();
            }
            catch (Exception ex)
            {
                Ex.LogException(ex);
            }
            currentlyRearranging = false;
        }

        private static void DoOrphanedBookmarkFailsafe()
        {
            RecipeBook.Instance.bookmarkControllersGroupController.GetAllBookmarksList().ForEach(bookmark =>
            {
                if (bookmark.rail == null || !bookmark.rail.railBookmarks.Contains(bookmark))
                {
                    Plugin.PluginLogger.LogError("ERROR: An orphaned bookmark has been found! This is the result of another error!");
                    var controller = RecipeBook.Instance.bookmarkControllersGroupController.controllers.First().bookmarkController;
                    //Move the now empty bookmark out of the sub group
                    var spawnPosition = SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Large)
                                        ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Medium)
                                        ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Small)
                                        ?? SubRailService.GetSpawnPosition(controller, BookmarkController.SpaceType.Min);
                    if (spawnPosition == null)
                    {
                        Plugin.PluginLogger.LogError("DoOrphanedBookmarkFailsafe - There is no empty space for bookmark! Change settings!");
                        return;
                    }
                    SubRailService.ConnectBookmarkToRail(spawnPosition.Item1, bookmark, spawnPosition.Item2);
                }
            });
        }

        private static int recipeIndexForCurrentGroupUpdate = -1;
        public static async void UpdateBookmarkGroupsForCurrentRecipe()
        {
            recipeIndexForCurrentGroupUpdate = GetBookmarkStorageRecipeIndexForSelectedRecipe();
            var subRailBookmarks = StaticStorage.SubRail.railBookmarks.Select(b => new { bookmark = b, serialized = b.GetSerialized() }).ToList();
            if (currentlyRearranging || rearrangeQueue.Count > 0)
            {
                while (currentlyRearranging || rearrangeQueue.Count > 0)
                {
                    await Task.Delay(100);
                }
            }
            currentlyRearranging = true;

            var allBookmarks = RecipeBook.Instance.bookmarkControllersGroupController.GetAllBookmarksList();
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
            var groupBookmark = RecipeBook.Instance.bookmarkControllersGroupController.GetBookmarkByIndex(recipeIndexForCurrentGroupUpdate);
            ShowHideGroupBookmarkIcon(groupBookmark, bookmarks.Any());
        }

        public static int GetBookmarkStorageRecipeIndexForSelectedRecipe()
        {
            return GetBookmarkStorageRecipeIndex(RecipeBook.Instance.currentPageIndex, out bool _);
        }

        public static int GetBookmarkStorageRecipeIndexForSelectedRecipe(out bool indexIsParent)
        {
            return GetBookmarkStorageRecipeIndex(RecipeBook.Instance.currentPageIndex, out indexIsParent);
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
            if (Managers.Cursor.grabbedInteractiveItem is BookmarkButtonInactive) return;
            var recipeBook = RecipeBook.Instance;
            var pagesCount = recipeBook.GetPagesCount();
            recipeBook.curlPageController.HotkeyClicked(nextIndex > recipeBook.currentPageIndex
                                                            ? recipeBook.currentPageIndex.Distance(nextIndex) <= nextIndex.Distance(recipeBook.currentPageIndex + pagesCount)
                                                            : recipeBook.currentPageIndex.Distance(nextIndex) >= recipeBook.currentPageIndex.Distance(nextIndex + pagesCount),
                                                        nextPageIndex: nextIndex);
        }

        public static void ShowHideGroupBookmarkIcon(Bookmark bookmark, bool show)
        {
            var cornerGameObject = bookmark.transform.Find(StaticStorage.CornerIconGameObjectName)?.gameObject;
            if (cornerGameObject == null)
            {
                Plugin.PluginLogger.LogError("ERROR: Bookmark does not have a group corner icon setup!");
                return;
            }
            cornerGameObject.gameObject.SetActive(show);
        }
    }
}
