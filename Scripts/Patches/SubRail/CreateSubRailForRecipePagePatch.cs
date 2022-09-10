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
                return;
            }
            RecipeBookService.SetupListeners(); //TODO move to a more appropriate spot

            SetupInvisiRail(); //TODO once we start making multiple sub rails here this needs to move to a more appropriate spot where it is called once
            var containers = SetupBookmarkContainer(parentPage);
            SetupRail(containers.Item1, containers.Item2);
            CreateStaticRailCopyForPage(parentPage);
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


            var maskSprite = GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Bookmark_organizer_recipe_slot_bottom_left_mask.png", null, true);
            if (maskSprite == null) return (null, null);


            var copyFromRenderer = typeof(RecipeBookLeftPageContent).GetField("titleDecorRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(page) as SpriteRenderer;
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
                var sprite = GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Bookmark_organizer_recipe_slot_{i}.png");
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

            //StaticStorage.SubRailActiveBookmarkLayer = CreateBookmarkLayer(subRailBookmarkContainer, maskSprite, sortingLayerId, sortingLayerName, currentSortOrder, "ActiveBookmarkLayer").Item1;
            StaticStorage.SubRailLayers.Reverse();

            //Position the sprite at the bottom left corner of the page
            subRailBookmarkContainer.transform.localPosition = new Vector2(-8.17f, -4.1f);
            subRailPages.transform.localPosition = new Vector2(-7.4718f, - 4.1f);
            return (subRailPages, subRailBookmarkContainer);
        }

        private static void CreateStaticRailCopyForPage(RecipeBookLeftPageContent parentPage)
        {
            var staticParent = new GameObject("StaticSubRail");
            staticParent.transform.parent = parentPage.transform;
            staticParent.transform.localPosition = new Vector2(-3.459f, -4.1f);
            var sortingCopyFrom = parentPage.transform.parent.Find("Scratches").GetComponent<SpriteRenderer>();
            var sortingGroup = staticParent.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerID = sortingCopyFrom.sortingLayerID;
            sortingGroup.sortingLayerName = sortingCopyFrom.sortingLayerName;
            sortingGroup.sortingOrder = sortingCopyFrom.sortingOrder + 1;

            var staticPages = UnityEngine.Object.Instantiate(StaticStorage.SubRailPages, staticParent.transform);
            staticPages.transform.localPosition = new Vector2(0.0025f, 0f);
            staticPages.SetActive(false);
            var staticBookmarkContainer = new GameObject("StaticBookmarkContainer");
            staticBookmarkContainer.transform.parent = staticParent.transform;
            staticBookmarkContainer.transform.localPosition = new Vector2(-0.6982f, 0f);
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
            maskObject.transform.localPosition += new Vector3(0, -1.61f, 0.96f);
            return (bookmarkLayer, maskObject);
        }

        private static Sprite GenerateSpriteFromImage(string path, Vector2? pivot = null, bool createComplexMesh = false)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            byte[] data;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                data = memoryStream.ToArray();
            }
            Texture2D texture;
            if (createComplexMesh)
            {
                texture = new Texture2D(0, 0, TextureFormat.ARGB32, false, false)
                {
                    filterMode = FilterMode.Bilinear
                };
            }
            else
            {
                texture = new Texture2D(0, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
                {
                    filterMode = FilterMode.Bilinear
                };
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.wrapModeU = TextureWrapMode.Clamp;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.wrapModeW = TextureWrapMode.Clamp;
            if (!texture.LoadImage(data))
            {
                Plugin.PluginLogger.LogError($"ERROR: Failed to load Bookmark_organizer_recipe_slot.png.");
                return null;
            }
            var actualPivot = pivot.HasValue ? pivot.Value : new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), actualPivot, 100, 1, createComplexMesh ? SpriteMeshType.Tight : SpriteMeshType.FullRect);
        }
    }
}
