using HarmonyLib;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.Bookmark;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.BookmarkController;
using static PotionCraft.ObjectBased.UIElements.Bookmarks.BookmarkRail;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class StoreSubRecipesOntoAvailableRailsOnSavePatch
    { 
        [HarmonyPatch(typeof(BookmarkController), "GetSerialized")]
        public class BookmarkController_GetSerialized
        {
            static bool Prefix(ref SerializedBookmarkController __result, BookmarkController __instance)
            {
                return AssignSubBookmarksToRailsBeforeSerialization(ref __result, __instance);
            }
        }

        private static bool AssignSubBookmarksToRailsBeforeSerialization(ref SerializedBookmarkController result, BookmarkController instance)
        {
            if (instance != Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController) return true;
            var serialized = new SerializedBookmarkController();
            Ex.RunSafe(() =>
            {
                var subBookmarkPositions = new Dictionary<BookmarkRail, List<(SerializedBookmark, int)>>();
                var allSubBookmarks = StaticStorage.BookmarkGroups.SelectMany(bg => bg.Value).ToList();
                Tuple<BookmarkRail, Vector2> defaultSpawnPosition = null;
                allSubBookmarks.ForEach(bookmark =>
                {
                    var spawnPosition = GetSpawnPosition(instance, SpaceType.Large, subBookmarkPositions)
                                        ?? GetSpawnPosition(instance, SpaceType.Medium, subBookmarkPositions)
                                        ?? GetSpawnPosition(instance, SpaceType.Small, subBookmarkPositions)
                                        ?? GetSpawnPosition(instance, SpaceType.Min, subBookmarkPositions);

                    if (defaultSpawnPosition == null)
                    {
                        defaultSpawnPosition = spawnPosition;
                    }

                    if (spawnPosition == null)
                    {
                        if (defaultSpawnPosition == null) defaultSpawnPosition = new Tuple<BookmarkRail, Vector2>(instance.rails[0], Vector2.zero);
                        spawnPosition = defaultSpawnPosition;
                    }

                    //Make sure we never put two bookmarks at exactly the same position to ensure reliable indexing
                    defaultSpawnPosition = MoveDefaultSpawnPosition(defaultSpawnPosition);

                    if (!subBookmarkPositions.ContainsKey(spawnPosition.Item1))
                    {
                        subBookmarkPositions[spawnPosition.Item1] = new List<(SerializedBookmark, int)>();
                    }
                    var serializedBookmark = new SerializedBookmark
                    {
                        position = spawnPosition.Item2,
                        prefabIndex = bookmark.SerializedBookmark.prefabIndex,
                        isMirrored = bookmark.SerializedBookmark.isMirrored
                    };
                    subBookmarkPositions[spawnPosition.Item1].Add((serializedBookmark, bookmark.recipeIndex));
                });

                serialized.serializedRails = instance.rails.Select(rail => GetSerialized(rail, subBookmarkPositions)).ToList();

                DoFailsafeIfNeeded(instance, serialized, subBookmarkPositions, defaultSpawnPosition);

                //Since we messed with the order of the bookmarks we need to change the saved recipe order to ensure recipes are in their proper bookmarks on load (if the mod gets uninstalled)
                RearrangeSavedBookmarksToWorkForBookmarkOrder(serialized, subBookmarkPositions);
            }, null, true);
            result = serialized;
            return false;
        }

        private static Tuple<BookmarkRail, Vector2> MoveDefaultSpawnPosition(Tuple<BookmarkRail, Vector2> defaultSpawnPosition)
        {
            defaultSpawnPosition = new Tuple<BookmarkRail, Vector2>(defaultSpawnPosition.Item1, new Vector2(defaultSpawnPosition.Item2.x + 0.09952f, defaultSpawnPosition.Item2.y));
            return defaultSpawnPosition;
        }

        private static void DoFailsafeIfNeeded(BookmarkController instance, SerializedBookmarkController serialized, Dictionary<BookmarkRail, List<(SerializedBookmark, int)>> subBookmarkPositions, Tuple<BookmarkRail, Vector2> defaultSpawnPosition)
        {
            var missingBookmarkCount = Managers.SaveLoad.SelectedProgressState.savedRecipes.Count - serialized.serializedRails.Take(serialized.serializedRails.Count - 2).Sum(r => r.serializedBookmarks.Count);
            if (missingBookmarkCount > 0)
            {
                var errorMessage = "ERROR: There are not enough saved bookmarks when saving. Running failsafe to fix file!";
                Plugin.PluginLogger.LogError(errorMessage);
                Ex.SaveErrorMessage(errorMessage);

                for (var i = 0; i < missingBookmarkCount; i++)
                {
                    //Make sure we never put two bookmarks at exactly the same position to ensure reliable indexing
                    defaultSpawnPosition = MoveDefaultSpawnPosition(defaultSpawnPosition);

                    var spawnPosition = GetSpawnPosition(instance, SpaceType.Large, subBookmarkPositions)
                                    ?? GetSpawnPosition(instance, SpaceType.Medium, subBookmarkPositions)
                                    ?? GetSpawnPosition(instance, SpaceType.Small, subBookmarkPositions)
                                    ?? GetSpawnPosition(instance, SpaceType.Min, subBookmarkPositions)
                                    ?? defaultSpawnPosition
                                    ?? new Tuple<BookmarkRail, Vector2>(instance.rails[0], Vector2.zero);

                    if (!subBookmarkPositions.ContainsKey(spawnPosition.Item1))
                    {
                        subBookmarkPositions[spawnPosition.Item1] = new List<(SerializedBookmark, int)>();
                    }
                    var serializedBookmark = new SerializedBookmark
                    {
                        position = spawnPosition.Item2,
                        prefabIndex = 0,
                        isMirrored = false
                    };
                    subBookmarkPositions[spawnPosition.Item1].Add((serializedBookmark, -1));
                }
                serialized.serializedRails = instance.rails.Select(rail => GetSerialized(rail, subBookmarkPositions)).ToList();
            }
        }

        private static void RearrangeSavedBookmarksToWorkForBookmarkOrder(SerializedBookmarkController serialized, Dictionary<BookmarkRail, List<(SerializedBookmark, int)>> subBookmarkPositions)
        {
            //Construct an intlist we can use to convert the saved recipes list to and from our save order
            var subBookmarkList = subBookmarkPositions.Values.SelectMany(v => v).ToList();
            var intList = new List<(int, int)>();
            var addedSubBookmarks = 0;
            var normalRailCount = serialized.serializedRails.Count - 2;
            for (var ri = 0; ri < normalRailCount; ri++)
            {
                serialized.serializedRails[ri].serializedBookmarks.ForEach(sb =>
                {
                    var newIndex = intList.Count;
                    if (!TryFirst(subBookmarkList, b => b.Item1 == sb, out var subBookmarkIndex))
                    {
                        intList.Add((intList.Count - addedSubBookmarks, newIndex));
                        return;
                    }
                    intList.Add((subBookmarkIndex.Item2, newIndex));
                    addedSubBookmarks++;
                });
            }

            var recipeList = new List<SerializedPotionRecipe>();
            for (var i = 0; i < Managers.SaveLoad.SelectedProgressState.savedRecipes.Count; i++)
            {
                var oldIndex = intList[i].Item1;

                oldIndex = GetRecipeIndexForFailsafeIfNeeded(intList, i, oldIndex);
                recipeList.Add(Managers.SaveLoad.SelectedProgressState.savedRecipes[oldIndex]);
            }
            Managers.SaveLoad.SelectedProgressState.savedRecipes = recipeList;

            //Invert our intList so it can be used on load to fix the order of the saved recipes
            StaticStorage.SavedRecipePositions = intList.OrderBy(i => i.Item1).Select(i => i.Item2).ToList();
        }

        private static int GetRecipeIndexForFailsafeIfNeeded(List<(int, int)> intList, int newIndex, int oldIndex)
        {
            //If we ran the failsafe earlier then that index will have a -1.
            //The recipe for the missing bookmark must be found at this point.
            if (oldIndex == -1)
            {
                //Find the first missing index
                for (var i2 = 0; i2 < Managers.SaveLoad.SelectedProgressState.savedRecipes.Count; i2++)
                {
                    if (intList.Any(item => item.Item1 == i2)) continue;
                    oldIndex = i2;
                    intList[newIndex] = new(i2, intList[newIndex].Item2);

                    var firstBookmarkGroup = StaticStorage.BookmarkGroups.Values.FirstOrDefault();
                    if (firstBookmarkGroup == null)
                    {
                        firstBookmarkGroup = new List<BookmarkStorage>();
                        StaticStorage.BookmarkGroups[0] = firstBookmarkGroup;
                    }
                    var position = firstBookmarkGroup.FirstOrDefault()?.SerializedBookmark?.position ?? new Vector2(0.5f, 0);
                    position = new Vector2(position.x + 0.09953334f, position.y);
                    firstBookmarkGroup.Add(new BookmarkStorage
                    {
                        recipeIndex = i2,
                        SerializedBookmark = new SerializedBookmark
                        {
                            position = position,
                            prefabIndex = 0,
                            isMirrored = false
                        }
                    });

                    break;
                }
            }

            return oldIndex;
        }

        private static SerializedBookmarkRail GetSerialized(BookmarkRail rail, Dictionary<BookmarkRail, List<(SerializedBookmark, int)>> subBookmarks)
        {
            var serialized = rail.railBookmarks.Select(bookmark => bookmark.GetSerialized()).ToList();
            if (subBookmarks.TryGetValue(rail, out List<(SerializedBookmark, int)> curSubBookmarks))
            {
                serialized = serialized.Concat(curSubBookmarks.Select(b => b.Item1)).ToList();
                serialized.Sort((b1, b2) => b1.position.x.CompareTo(b2.position.x));
            }
            return new SerializedBookmarkRail
            {
                serializedBookmarks = serialized
            };
        }

        private static bool TryFirst<T>(List<T> values, Func<T, bool> compareFunc, out T first)
        {
            first = default;
            foreach (var item in values)
            {
                if (compareFunc(item))
                {
                    first = item;
                    return true;
                }
            }
            return false;
        }
        public static Tuple<BookmarkRail, Vector2> GetSpawnPosition(BookmarkController bookmarkController, SpaceType spaceType, Dictionary<BookmarkRail, List<(SerializedBookmark, int)>> subBookmarks)
        {
            var nonSpecialRails = bookmarkController.rails.Except(new[] { StaticStorage.SubRail, StaticStorage.InvisiRail }).ToList();
            foreach (var rail in nonSpecialRails)
            {
                var emptySegments = GetEmptySegments(rail, spaceType, subBookmarks);
                if (emptySegments.Any())
                {
                    var x = rail.inverseSpawnOrder ? emptySegments.Last().max : emptySegments.First().min;
                    var spawnHeight = typeof(BookmarkController).GetField("spawnHeight", BindingFlags.NonPublic | BindingFlags.Instance)
                                                                .GetValue(bookmarkController) as MinMaxFloat;
                    var y = spawnHeight.GetRandom();
                    return new Tuple<BookmarkRail, Vector2>(rail, new Vector2(x, y));
                }
            }
            return null;
        }

        private static List<MinMaxFloat> GetEmptySegments(BookmarkRail rail, SpaceType spaceType, Dictionary<BookmarkRail, List<(SerializedBookmark, int)>> subBookmarks)
        {
            var emptySegments = rail.GetEmptySegments(spaceType);
            if (!subBookmarks.ContainsKey(rail)) return emptySegments;
            var remainingSubBookmarks = subBookmarks[rail].OrderBy(b => b.Item1.position.x).ToList();
            for (var i = 0; i < emptySegments.Count; i++)
            {
                if (!remainingSubBookmarks.Any()) break;
                var curEmptySegment = emptySegments[i];
                var curSubBookmark = remainingSubBookmarks[0];
                if (curSubBookmark.Item1.position.x < curEmptySegment.min) continue;
                var newMinMax = new MinMaxFloat(curEmptySegment.min, curSubBookmark.Item1.position.x);
                var newIndex = i + 1;
                emptySegments.Insert(newIndex, newMinMax);
                remainingSubBookmarks.RemoveAt(0);
            }
            return emptySegments;
        }
    }
}
