using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Storage
{
    public class BookmarkOrganizerManager : MonoBehaviour
    {
        public Dictionary<int, List<BookmarkStorage>> BookmarkGroups => StaticStorage.BookmarkGroups;
        public List<int> SavedRecipePositions => StaticStorage.SavedRecipePositions;
        public List<Transform> SubRailLayers => StaticStorage.SubRailLayers;
        public Transform InvisiRailLayer => StaticStorage.InvisiRailLayer;
        public BookmarkRail SubRail => StaticStorage.SubRail;
        public GameObject SubRailActiveBookmarkLayer => StaticStorage.SubRailActiveBookmarkLayer;
        public BookmarkRail InvisiRail => StaticStorage.InvisiRail;
        public bool AddedListeners => StaticStorage.AddedListeners;

        void Update()
        {
            if (StaticStorage.HotkeyDown == null || StaticStorage.HotkeyUp == null) return;
            if (Commands.roomDown.State == State.JustDowned)
            {
                RecipeBookService.FlipPageToNextGroup(false);
                StaticStorage.HotkeyDown.SetAction(repeater => RecipeBookService.FlipPageToNextGroup(false)).StopWhen(() => !CanHotkeysBeUsed());
            }
            else if (Commands.roomUp.State == State.JustDowned)
            { 
                RecipeBookService.FlipPageToNextGroup(true);
                StaticStorage.HotkeyUp.SetAction(repeater => RecipeBookService.FlipPageToNextGroup(true)).StopWhen(() => !CanHotkeysBeUsed());
            }
        }

        private bool CanHotkeysBeUsed() => !Managers.Input.HasInputGotToBeDisabled();
    }
}
