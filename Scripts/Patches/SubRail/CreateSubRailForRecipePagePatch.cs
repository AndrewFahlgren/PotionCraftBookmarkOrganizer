using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ObjectBased.UIElements.PotionCustomizationPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.ClassOverrides;
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
    public class CreateSubRailForRecipePagePatch
    { 
        //This method is chosen because it runs during the RecipeBookLeftPageContent load callback.
        //This gives access to the left page content and is ensured to run after the recipe book is setup.
        [HarmonyPatch(typeof(PotionCustomizationPanel), "OnPanelContainerStart")]
        public class PotionCustomizationPanel_OnPanelContainerStart
        {
            static void Postfix(PotionCustomizationPanel __instance)
            {
                Ex.RunSafe(() => CreateSubRailForRecipePage(__instance));
            }
        }

        private static void CreateSubRailForRecipePage(PotionCustomizationPanel instance)
        {
            var parentPage = instance.gameObject.GetComponentInParent<RecipeBookLeftPageContent>();
            if (parentPage == null) return;
            if (StaticStorage.SubRail != null)
            {
                CreateStaticRailCopyForPage(parentPage);
                ShrinkDescriptionBox(parentPage);
                return;
            }
            RecipeBookService.SetupListeners(); //TODO move to a more appropriate spot

            SetupInvisiRail();
            var containers = SetupBookmarkContainer(parentPage);
            SetupRail(containers.Item1, containers.Item2);
            CreateStaticRailCopyForPage(parentPage);
            ShrinkDescriptionBox(parentPage);
        }

        private static void SetupRail(GameObject subRailPages, GameObject subRailBookmarkContainer)
        {
            var parentController = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            var railGameObject = new GameObject(StaticStorage.SubRailName);
            railGameObject.transform.parent = parentController.gameObject.transform;
            railGameObject.transform.localPosition = subRailBookmarkContainer.transform.localPosition;

            var subRail = railGameObject.AddComponent<BookmarkRail>();
            StaticStorage.SubRail = subRail;
            var parent = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            railGameObject.transform.parent = parent.transform;
            subRailBookmarkContainer.transform.parent = subRail.transform;
            subRailBookmarkContainer.transform.localPosition = Vector2.zero;
            subRail.bookmarksContainer = subRailBookmarkContainer.transform;
            var transform = StaticStorage.SubRailLayers.First();
            typeof(BookmarkRail).GetField("activeLayer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(subRail, transform);
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            var pageRenderer = subRailPages.GetComponentInChildren<SpriteRenderer>();
            subRail.size = new Vector2(pageRenderer.size.y - 0.5f, 0.7f);
            subRail.direction = BookmarkRail.Direction.BottomToTop;
            subRail.bottomLine = typeof(BookmarkRail).GetMethod("GetBottomLine", BindingFlags.Instance | BindingFlags.NonPublic)
                                                     .Invoke(subRail, null) as Tuple<Vector2, Vector2>;
            typeof(BookmarkRail).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(subRail, null);

            subRail.bookmarkController.rails.Add(subRail);

            SetupStaticBookmark(subRail);
        }

        private static void SetupStaticBookmark(BookmarkRail subRail)
        {
            var nextNormalEmptySpace = SubRailService.GetNextEmptySpaceOnRail(subRail, true).Value;
            var xPositionOffset = subRail.bookmarkController.minBookmarksSpace * 0.75f;
            //For some reason a y value of 0 is offset slightly from bookmarks which are dragged on manually. The static bookmark should be at the closest possible y value so offset it here.
            var staticBookmarkPosition = new Vector2(nextNormalEmptySpace.x + xPositionOffset, -0.04f);
            StaticStorage.StaticBookmark = subRail.SpawnBookmarkAt(0, staticBookmarkPosition, false);
            StaticStorage.StaticBookmark.transform.parent = StaticStorage.SubRailActiveBookmarkLayer.transform;
            //var sortGroup = StaticStorage.StaticBookmark.GetComponent<SortingGroup>();
            ////Make the sorting order greater than 0 so it is always shown above other bookmarks
            //sortGroup.sortingOrder = 1;
            //Make sure we don't grab bookmarks which are under the static bookmark
            //StaticStorage.StaticBookmark.SetRaycastPriorityLevel(StaticStorage.StaticBookmark.activeBookmarkButton.raycastPriorityLevel - 1000);
            subRail.railBookmarks.Clear();
        }

        private static void SetupInvisiRail()
        {
            var parentController = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            var railGameObject = new GameObject(StaticStorage.InvisiRailName);
            railGameObject.transform.localPosition = new Vector2(0, 300);
            railGameObject.transform.parent = parentController.gameObject.transform;
            var bookmarkContainer = new GameObject("InvisiRailBookmarkContainer");
            var bookmarkLayer = new GameObject("Layer 0");
            bookmarkLayer.transform.parent = bookmarkContainer.transform;
            StaticStorage.InvisiRailLayer = bookmarkLayer.transform;

            var invisiRail = railGameObject.AddComponent<BookmarkRail>();
            StaticStorage.InvisiRail = invisiRail;
            var parent = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            railGameObject.transform.parent = parent.transform;
            bookmarkContainer.transform.parent = invisiRail.transform;
            bookmarkContainer.transform.localPosition = Vector2.zero;
            invisiRail.bookmarksContainer = bookmarkContainer.transform;
            var transform = bookmarkLayer.transform;
            typeof(BookmarkRail).GetField("activeLayer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(invisiRail, transform);
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            invisiRail.size = new Vector2(100f, 0.7f);
            invisiRail.direction = BookmarkRail.Direction.BottomToTop;
            invisiRail.bottomLine = typeof(BookmarkRail).GetMethod("GetBottomLine", BindingFlags.Instance | BindingFlags.NonPublic)
                                                        .Invoke(invisiRail, null) as Tuple<Vector2, Vector2>;
            typeof(BookmarkRail).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(invisiRail, null);

            invisiRail.bookmarkController.rails.Add(invisiRail);
        }

        private static (GameObject, GameObject) SetupBookmarkContainer(RecipeBookLeftPageContent page)
        {
            var subRailBookmarkContainer = new GameObject("SubRailBookmarkContainer");
            var subRailPages = new GameObject("SubRailPages");
            StaticStorage.SubRailPages = subRailPages;
            var pageContainer = Managers.Potion.recipeBook.transform.Find("ContentContainer").Find("BackgroundPages");
            subRailPages.transform.parent = pageContainer;


            var maskSprite = SaveLoadService.GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Bookmark_organizer_recipe_slot_bottom_left_mask.png", null, true);
            if (maskSprite == null) return (null, null);


            var copyFromRenderer = typeof(RecipeBookLeftPageContent).GetField("titleDecorLeftRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(page) as SpriteRenderer;
            var sortingLayerId = copyFromRenderer.sortingLayerID;
            var sortingLayerName = copyFromRenderer.sortingLayerName;
            var layerCount = 4;
            var currentSortOrder = 511;
            for (var i = layerCount - 1; i >= 0; i--)
            {
                //Create a game object for this specific page
                var pageLayer = new GameObject($"Layer{i}");
                pageLayer.transform.parent = subRailPages.transform;
                pageLayer.AddComponent<PageLayer>();

                var renderer = pageLayer.AddComponent<SpriteRenderer>();
                var sprite = SaveLoadService.GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Bookmark_organizer_recipe_slot_{i}.png");
                if (sprite == null) return (null, null);
                renderer.sprite = sprite;
                renderer.sortingLayerID = sortingLayerId;
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = currentSortOrder;

                var collider = pageLayer.AddComponent<BoxCollider2D>();
                if (i == 0) collider.isTrigger = true;
                collider.size = renderer.size;

                var bookmarkLayer = CreateBookmarkLayer(subRailBookmarkContainer, maskSprite, sortingLayerId, sortingLayerName, currentSortOrder, $"Layer {i}");
                //Add it three times to make our 4 layers act like 12 layers
                StaticStorage.SubRailLayers.Add(bookmarkLayer.Item1.transform);
                StaticStorage.SubRailLayers.Add(bookmarkLayer.Item1.transform);
                StaticStorage.SubRailLayers.Add(bookmarkLayer.Item1.transform);

                if (i == 0)
                {
                    var maskObject = bookmarkLayer.Item2;
                    var debugRenderer = maskObject.AddComponent<SpriteRenderer>();
                    debugRenderer.sprite = maskSprite;

                    var dummyInteractiveItem = maskObject.AddComponent<DummyInteractiveItem>();
                    dummyInteractiveItem.raycastPriorityLevel = -11000;
                    dummyInteractiveItem.cursorRadiusCollision = false;
                    dummyInteractiveItem.showOnlyFingerWhenInteracting = true;
                    maskObject.AddComponent<PolygonCollider2D>();
                    maskObject.layer = PotionCraft.Layers.UI;

                    debugRenderer.enabled = false;
                }

                //Add 20 to the sorting layer to emulate pages used for the main bookmark rails
                currentSortOrder += 20;
            }

            StaticStorage.SubRailActiveBookmarkLayer = CreateBookmarkLayer(subRailBookmarkContainer, maskSprite, sortingLayerId, sortingLayerName, currentSortOrder - 11, "ActiveBookmarkLayer").Item1;
            StaticStorage.SubRailLayers.Reverse();

            //Position the sprite at the bottom left corner of the page
            subRailBookmarkContainer.transform.localPosition = new Vector2(-7.594f, -4.13f); //new Vector2(-8.17f, -4.1f);

            subRailPages.transform.localPosition = new Vector2(-7.165f, -4.07f); //new Vector2(-7.181f, -4.082f); TODO apply this everywhere else
            return (subRailPages, subRailBookmarkContainer);
        }

        private static void CreateStaticRailCopyForPage(RecipeBookLeftPageContent parentPage)
        {
            var staticParent = new GameObject("StaticSubRail");
            staticParent.transform.parent = parentPage.transform;
            staticParent.transform.localPosition = new Vector2(-3.443f, -4.07f);//new Vector2(-3.459f, -4.1f);
            var sortingCopyFrom = parentPage.transform.parent.Find("Scratches").GetComponent<SpriteRenderer>();
            var sortingGroup = staticParent.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerID = sortingCopyFrom.sortingLayerID;
            sortingGroup.sortingLayerName = sortingCopyFrom.sortingLayerName;
            sortingGroup.sortingOrder = sortingCopyFrom.sortingOrder + 1;

            var staticPages = UnityEngine.Object.Instantiate(StaticStorage.SubRailPages, staticParent.transform);
            staticPages.transform.localPosition = new Vector2(0.2933f, 0f);
            staticPages.SetActive(false);
            var staticBookmarkContainer = new GameObject("StaticBookmarkContainer");
            staticBookmarkContainer.transform.parent = staticParent.transform;
            staticBookmarkContainer.transform.localPosition = new Vector2(-0.1365f, -.06f);//new Vector2(-0.6982f, 0f);
            staticBookmarkContainer.SetActive(false);
            StaticStorage.StaticRails[parentPage] = (staticBookmarkContainer, staticPages);
        }

        private static (GameObject, GameObject) CreateBookmarkLayer(GameObject subRailBookmarkContainer, Sprite maskSprite, int sortingLayerId, string sortingLayerName, int currentSortOrder, string layerName)
        {
            //Setup the actual bookmark container for this layer
            var bookmarkLayer = new GameObject(layerName);
            bookmarkLayer.transform.parent = subRailBookmarkContainer.transform;
            var sortingGroup = bookmarkLayer.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerID = sortingLayerId;
            sortingGroup.sortingLayerName = sortingLayerName;
            sortingGroup.sortingOrder = currentSortOrder - 10;

            var maskObject = new GameObject("BookmarkMask");
            maskObject.transform.parent = bookmarkLayer.transform;
            var mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = maskSprite;
            maskObject.transform.rotation = Quaternion.Euler(0, 0, 270);
            maskObject.transform.localPosition += new Vector3(0.06f, -1.61f, 0.96f);
            return (bookmarkLayer, maskObject);
        }

        private static void ShrinkDescriptionBox(RecipeBookLeftPageContent parentPage)
        {
            var inputFieldCanvas = parentPage.potionCustomizationPanel.transform.Find("InputFieldCanvas").transform;
            var descriptionTop = inputFieldCanvas.Find("DescriptionTop");
            var descriptionBottom = inputFieldCanvas.Find("DescriptionBottom");
            var descriptionBorderTop = inputFieldCanvas.Find("DescriptionBackground Top");
            var descriptionBorderBottom = inputFieldCanvas.Find("DescriptionBackground Bottom");
            var descriptionBorderLeft = inputFieldCanvas.Find("DescriptionBackground Left");
            var descriptionBorderRight = inputFieldCanvas.Find("DescriptionBackground Right");
            var inputFieldCanvasLines = inputFieldCanvas.Find("Maskable");
            var positionOffset = new Vector3(0.3309f, 0);
            descriptionBorderTop.transform.localPosition += positionOffset;
            descriptionBorderBottom.transform.localPosition += positionOffset;
            descriptionTop.transform.localPosition += positionOffset;
            descriptionBottom.transform.localPosition += positionOffset;
            inputFieldCanvasLines.transform.localPosition += positionOffset;
            positionOffset = new Vector3(0.02f, 0);
            descriptionBorderRight.transform.localPosition += positionOffset;
            positionOffset = new Vector3(0.64f, 0);
            descriptionBorderLeft.transform.localPosition += positionOffset;
            var sourceScale = descriptionBorderTop.transform.localScale;
            var borderScale = new Vector3(sourceScale.x * 0.9018f, sourceScale.y, sourceScale.z);
            descriptionBorderTop.transform.localScale = borderScale;
            descriptionBorderBottom.transform.localScale = borderScale;
            descriptionTop.transform.localScale = borderScale;
            descriptionBottom.transform.localScale = borderScale;
            var scale = new Vector3(0.9018f, 1f, 1f);
            inputFieldCanvasLines.transform.localScale = scale;
        }
    }
}
