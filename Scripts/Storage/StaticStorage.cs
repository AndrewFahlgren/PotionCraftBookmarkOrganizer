using PotionCraft.InputSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Storage
{
    public static class StaticStorage
    {
        public const string BookmarkGroupsJsonSaveName = "FahlgorithmBookmarkOrganizer";

        public const string SubRailName = "BottomToTopSubRail";
        public const string InvisiRailName = "BottomToTopInvisiRail";

        public const string CornerIconGameObjectName = "GroupCornerIcon";

        public static Dictionary<int, List<BookmarkStorage>> BookmarkGroups = new();
        public static List<int> SavedRecipePositions;
        public static List<string> ErrorLog = new();

        public static BookmarkRail SubRail;
        public static GameObject SubRailPages;
        public static BookmarkRail InvisiRail;
        public static Bookmark StaticBookmark;
        public static Dictionary<RecipeBookLeftPageContent, (GameObject, GameObject)> StaticRails = new ();

        public static List<Transform> SubRailLayers = new();
        public static Transform InvisiRailLayer;
        public static GameObject SubRailActiveBookmarkLayer;

        public static bool AddedListeners;
        public static bool IsLoaded;

        public static CommandInvokeRepeater HotkeyUp;
        public static CommandInvokeRepeater HotkeyDown;

        public static string StateJsonString;

        public class SavedStaticStorage
        {
            public Dictionary<int, List<BookmarkStorage>> BookmarkGroups { get; set; }
            public List<int> SavedRecipePositions { get; set; }
            public List<string> ErrorLog { get; set; }
            public string BookmarkManagerVersion { get; set; }
        }
    }
}
