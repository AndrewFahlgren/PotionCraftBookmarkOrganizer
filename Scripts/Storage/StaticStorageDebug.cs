using PotionCraft.ObjectBased.UIElements.Bookmarks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Storage
{
    public class StaticStorageDebug : MonoBehaviour
    {
        public Dictionary<int, List<BookmarkStorage>> BookmarkGroups => StaticStorage.BookmarkGroups;
        public List<Transform> SubRailLayers => StaticStorage.SubRailLayers;
        public Transform InvisiRailLayer => StaticStorage.InvisiRailLayer;
        public BookmarkRail SubRail => StaticStorage.SubRail;
        public GameObject SubRailActiveBookmarkLayer => StaticStorage.SubRailActiveBookmarkLayer;
        public BookmarkRail InvisiRail => StaticStorage.InvisiRail;
        public bool AddedListeners => StaticStorage.AddedListeners;
        public bool RemovingSubRailBookMarksForPageTurn => StaticStorage.RemovingSubRailBookMarksForPageTurn;
        public bool AddingSubRailBookMarksForPageTurn => StaticStorage.AddingSubRailBookMarksForPageTurn;
    }
}
