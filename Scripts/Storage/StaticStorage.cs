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

        public static Dictionary<int, List<BookmarkStorage>> BookmarkGroups = new();

        public static BookmarkRail SubRail;
        public static GameObject SubRailPages;
        public static BookmarkRail InvisiRail;
        public static Bookmark StaticBookmark;
        public static Dictionary<RecipeBookLeftPageContent, (GameObject, GameObject)> StaticRails = new ();

        public static List<Transform> SubRailLayers = new();
        public static Transform InvisiRailLayer;
        public static GameObject SubRailActiveBookmarkLayer;

        public static bool AddedListeners;
        public static bool RemovingSubRailBookMarksForPageTurn;
        public static bool AddingSubRailBookMarksForPageTurn;
        public static bool IsLoaded;

        public static CommandInvokeRepeater HotkeyUp;
        public static CommandInvokeRepeater HotkeyDown;

        public static string StateJsonString;
    }
}
