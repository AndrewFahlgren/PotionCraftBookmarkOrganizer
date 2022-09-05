using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class SetupBookmarkMaskingPatch
    { 
        [HarmonyPatch(typeof(Bookmark), "Awake")]
        public class Bookmark_Awake
        {
            static void Postfix(Bookmark __instance)
            {
                Ex.RunSafe(() => SetupBookmarkMasking(__instance));
            }
        }

        private static void SetupBookmarkMasking(Bookmark instance)
        {
            var renderers = instance.GetComponentsInChildren<SpriteRenderer>().ToList();
            renderers.ForEach(renderer => renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask);
        }
    }
}
