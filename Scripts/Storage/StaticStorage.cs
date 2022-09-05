using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Storage
{
    public static class StaticStorage
    {
        public const string SubRailName = "BottomToTopSubRail";

        public static Dictionary<int, List<int>> BookmarkGroups = new();
        public static List<Transform> SubRailLayers = new();
        public static bool CreatedSubRail;
    }
}
