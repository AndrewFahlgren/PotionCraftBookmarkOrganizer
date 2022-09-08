using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Setup indexes for subbookmark and adjust all other indexes as needed
    /// If the first bookmark has changed update main bookmark with that icon
    /// </summary>
    //public class DisableGrabbingForActiveStaticBookmarkPatch
    //{ 
    //    [HarmonyPatch(typeof(ActiveBookmarkButton), "OnGrabPrimary")]
    //    public class ActiveBookmarkButton_OnGrabPrimary
    //    {
    //        static bool Prefix(ActiveBookmarkButton __instance)
    //        {
    //            return Ex.RunSafe(() => DisableGrabbingForActiveStaticBookmark(__instance));
    //        }
    //    }

    //    private static bool DisableGrabbingForActiveStaticBookmark(ActiveBookmarkButton instance)
    //    {
    //        var bookmark = typeof(ActiveBookmarkButton).GetField("bookmark", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as Bookmark;

    //        return bookmark != StaticStorage.StaticBookmark;
    //        Managers.Cursor.ReleasePrimary();
    //    }
    //}
}
