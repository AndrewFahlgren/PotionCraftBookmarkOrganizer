using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class AddGroupCornerIconToBookmarkPatch
    {
        [HarmonyPatch(typeof(Bookmark), "UpdateVisualState")]
        public class Bookmark_UpdateVisualState
        {
            static void Postfix(Bookmark __instance)
            {
                Ex.RunSafe(() => UpdateCornerIconScaleToMatchActiveState(__instance));
            }
        }

        [HarmonyPatch(typeof(InactiveBookmarkButton), "OnGrabPrimary")]
        public class InactiveBookmarkButton_OnGrabPrimary
        {
            static void Postfix(InactiveBookmarkButton __instance)
            {
                Ex.RunSafe(() => UpdateCornerIconScaleToMatchActiveState(__instance));
            }
        }

        [HarmonyPatch(typeof(InactiveBookmarkButton), "OnReleasePrimary")]
        public class InactiveBookmarkButton_OnReleasePrimary
        {
            static void Postfix(InactiveBookmarkButton __instance)
            {
                Ex.RunSafe(() => UpdateCornerIconScaleToMatchActiveState(__instance));
            }
        }

        [HarmonyPatch(typeof(RecipeBook), "UpdateBookmarkIcon")]
        public class RecipeBook_UpdateBookmarkIcon
        {
            static void Postfix(RecipeBook __instance, int index)
            {
                Ex.RunSafe(() => AddGroupBookmarkIconToBookmarkOnFirstContentUpdate(__instance, index));
            }
        }

        private static Sprite LoadedCornerSprite;
        private static Sprite LoadedCornerMaskSprite;

        private static void AddGroupBookmarkIconToBookmarkOnFirstContentUpdate(RecipeBook instance, int index)
        {
            var bookmark = instance.bookmarkControllersGroupController.GetBookmarkByIndex(index);
            if (bookmark.gameObject.transform.Find(StaticStorage.CornerIconGameObjectName) != null) return;

            if (LoadedCornerSprite == null)
                LoadedCornerSprite = SaveLoadService.GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Group_bookmark_icon.png", null, true);
            if (LoadedCornerMaskSprite == null)
                LoadedCornerMaskSprite = SaveLoadService.GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Group_bookmark_icon_mask.png", null, true);

            var cornerGameObject = new GameObject(StaticStorage.CornerIconGameObjectName);
            cornerGameObject.transform.parent = bookmark.transform;
            var renderer = cornerGameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadedCornerSprite;
            var copyFromRenderer = bookmark.activeBookmarkButton.spriteRendererIcon;
            renderer.sortingLayerID = copyFromRenderer.sortingLayerID;
            renderer.sortingLayerName = copyFromRenderer.sortingLayerName;
            renderer.sortingOrder = copyFromRenderer.sortingOrder;
            var mask = cornerGameObject.AddComponent<SpriteMask>();
            mask.sprite = LoadedCornerMaskSprite;
            mask.sortingLayerID = copyFromRenderer.sortingLayerID;
            mask.sortingLayerName = copyFromRenderer.sortingLayerName;
            mask.sortingOrder = copyFromRenderer.sortingOrder;

            cornerGameObject.transform.localPosition = new Vector2(0.245f, 0.675f);
            cornerGameObject.transform.rotation = Quaternion.identity;
            cornerGameObject.transform.localEulerAngles = Vector3.zero;

            var isGroupParent = RecipeBookService.IsBookmarkGroupParent(index);
            cornerGameObject.SetActive(isGroupParent);
        }

        private static void UpdateCornerIconScaleToMatchActiveState(Bookmark instance)
        {
            UpdateCornerIconScaleToMatchActiveState(instance.inactiveBookmarkButton, instance);
        }

        private static void UpdateCornerIconScaleToMatchActiveState(InactiveBookmarkButton instance, Bookmark bookmark = null)
        {
            if (bookmark == null) bookmark = instance.GetComponentInParent<Bookmark>();
            if (bookmark == null) return;
            if (bookmark == StaticStorage.StaticBookmark) return;
            var cornerGameObject = bookmark.transform.Find(StaticStorage.CornerIconGameObjectName)?.gameObject;
            if (cornerGameObject == null) return;
            cornerGameObject.transform.localScale = bookmark.CurrentVisualState == Bookmark.VisualState.Active 
                                                        ? new Vector3(1.05f, 1.05f, 1.05f) 
                                                        : Managers.Cursor.grabbedInteractiveItem == instance 
                                                            ? new Vector3(0.95f, 0.95f, 0.95f) 
                                                            : Vector3.one;
        }
    }
}
