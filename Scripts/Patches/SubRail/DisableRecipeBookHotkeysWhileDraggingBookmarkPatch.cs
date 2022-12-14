using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.Core.ValueContainers;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class DisableRecipeBookHotkeysWhileDraggingBookmarkPatch
    { 
        [HarmonyPatch(typeof(CurlPageCornerController), "FlipPage")]
        public class CurlPageCornerController_FlipPage
        {
            static bool Prefix()
            {
                return Ex.RunSafe(() => DisableRecipeBookHotkeysWhileDraggingBookmark());
            }
        }

        private static bool DisableRecipeBookHotkeysWhileDraggingBookmark()
        {
            return Managers.Cursor.grabbedInteractiveItem is not BookmarkButtonInactive;
        }
    }
}
