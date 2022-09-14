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
        private static Vector2 CornerIconLocation = new Vector2(0.245f, 0.675f);


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
                Ex.RunSafe(() => UpdateCornerIconScaleToMatchActiveState(__instance, true));
            }
        }

        [HarmonyPatch(typeof(InactiveBookmarkButton), "OnReleasePrimary")]
        public class InactiveBookmarkButton_OnReleasePrimary
        {
            static void Postfix(InactiveBookmarkButton __instance)
            {
                Ex.RunSafe(() => UpdateCornerIconScaleToMatchActiveState(__instance, false));
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

            cornerGameObject.transform.localPosition = CornerIconLocation;
            cornerGameObject.transform.rotation = Quaternion.identity;
            cornerGameObject.transform.localEulerAngles = Vector3.zero;

            var isGroupParent = RecipeBookService.IsBookmarkGroupParent(index);
            cornerGameObject.SetActive(isGroupParent);
        }

        private static void UpdateCornerIconScaleToMatchActiveState(Bookmark instance)
        {
            UpdateCornerIconScaleToMatchActiveState(instance.inactiveBookmarkButton, false, instance);
        }

        private static void UpdateCornerIconScaleToMatchActiveState(InactiveBookmarkButton instance, bool isGrabbed, Bookmark bookmark = null)
        {
            if (bookmark == null) bookmark = instance.GetComponentInParent<Bookmark>();
            if (bookmark == null) return;
            if (bookmark == StaticStorage.StaticBookmark) return;
            var cornerGameObject = bookmark.transform.Find(StaticStorage.CornerIconGameObjectName)?.gameObject;
            if (cornerGameObject == null) return;
            var activeScale = 1.03f;
            var grabbedScale = 0.97f;
            cornerGameObject.transform.localScale = bookmark.CurrentVisualState == Bookmark.VisualState.Active 
                                                        ? new Vector3(activeScale, activeScale, activeScale) 
                                                        : isGrabbed
                                                            ? new Vector3(grabbedScale, grabbedScale, grabbedScale) 
                                                            : Vector3.one;
            cornerGameObject.transform.localPosition = CornerIconLocation + (bookmark.CurrentVisualState == Bookmark.VisualState.Active
                                                                                ? new Vector2(0.01f, 0.01f)
                                                                                : isGrabbed
                                                                                    ? new Vector2(-0.006f, -0.006f)
                                                                                    : Vector2.zero);
        }
    }
}
