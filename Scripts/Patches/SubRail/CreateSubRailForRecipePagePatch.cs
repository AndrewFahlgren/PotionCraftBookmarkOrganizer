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

        //TODO We need to create another object to mask bookmark tails on the same layer as layer 4
        private static void CreateSubRailForRecipePage(PotionCustomizationPanel instance)
        {
            if (StaticStorage.CreatedSubRail) return;
            var parentPage = instance.gameObject.GetComponentInParent<RecipeBookLeftPageContent>();
            if (parentPage == null) return;

            var containers = SetupBookmarkContainer(parentPage);
            SetupRail(containers.Item1, containers.Item2);
        }

        private static void SetupRail(GameObject subRailPages, GameObject subRailBookmarkContainer)
        {
            if (StaticStorage.CreatedSubRail) return;
            StaticStorage.CreatedSubRail = true;
            var parentController = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers.First().bookmarkController;
            var railGameObject = new GameObject(StaticStorage.SubRailName);
            railGameObject.transform.parent = parentController.gameObject.transform;
            railGameObject.transform.localPosition = subRailBookmarkContainer.transform.localPosition;

            var subRail = railGameObject.AddComponent<BookmarkRail>();
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
        }

        private static (GameObject, GameObject) SetupBookmarkContainer(RecipeBookLeftPageContent page)
        {
            var subRailBookmarkContainer = new GameObject("SubRailBookmarkContainer");
            var subRailPages = new GameObject("SubRailPages");
            var pageContainer = Managers.Potion.recipeBook.transform.Find("ContentContainer").Find("BackgroundPages");
            subRailPages.transform.parent = pageContainer;


            var maskSprite = GenerateSpriteFromImage($"PotionCraftBookmarkOrganizer.InGameImages.Bookmark_organizer_recipe_slot_bottom_left_mask.png", null, true);//, new Vector2(0.18f, 0.5f));
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

                if (StaticStorage.SubRailLayers.Count < layerCount * 3)
                {
                    //Setup the actual bookmark container for this layer
                    var bookmarkLayer = new GameObject($"Layer {i}");
                    bookmarkLayer.transform.parent = subRailBookmarkContainer.transform;
                    //Add it three times to make our 4 layers act like 12 layers
                    StaticStorage.SubRailLayers.Add(bookmarkLayer.transform);
                    StaticStorage.SubRailLayers.Add(bookmarkLayer.transform);
                    StaticStorage.SubRailLayers.Add(bookmarkLayer.transform);
                    var sortingGroup = bookmarkLayer.AddComponent<SortingGroup>();
                    sortingGroup.sortingLayerID = sortingLayerId;
                    sortingGroup.sortingLayerName = sortingLayerName;
                    sortingGroup.sortingOrder = currentSortOrder - 10;

                    var maskObject = new GameObject("BookmarkMask");
                    maskObject.transform.parent = bookmarkLayer.transform;
                    //var maskSortingGroup = maskObject.AddComponent<SortingGroup>();
                    //maskSortingGroup.sortingLayerID = sortingLayerId;
                    //maskSortingGroup.sortingLayerName = sortingLayerName;
                    //maskSortingGroup.sortingOrder = currentSortOrder - 10;
                    var mask = maskObject.AddComponent<SpriteMask>();
                    mask.sprite = maskSprite;
                   //var offsetPercentage = .32f;
                    //var xOffset = maskSprite.bounds.size.x * offsetPercentage;
                    maskObject.transform.rotation = Quaternion.Euler(0, 0, 270);
                    maskObject.transform.localPosition += new Vector3(0, -1.61f, 0.96f);
                    //maskObject.transform.localPosition += new Vector3(xOffset, 0, 0);

                    if ( i == 0 )
                    {
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



                    //AddPolygonCollider2D(maskObject, maskSprite);
                }

                //Add 20 to the sorting layer to emulate pages used for the main bookmark rails
                currentSortOrder += 20;
            }

            StaticStorage.SubRailLayers.Reverse();

            //Position the sprite at the bottom left corner of the page
            //var backgroundSpriteRendererBounds = page.transform.parent.Find("Background").GetComponent<SpriteRenderer>().bounds;
            //var bottomLeft = new Vector2(backgroundSpriteRendererBounds.center.x - backgroundSpriteRendererBounds.extents.x, backgroundSpriteRendererBounds.center.y - backgroundSpriteRendererBounds.extents.y);
            //cuttoutGameObject.transform.position = new Vector2(bottomLeft.x + renderer.size.x / 2, bottomLeft.y + renderer.size.y / 2);

            subRailBookmarkContainer.transform.localPosition = new Vector2(-8.18f, -4.12f);
            subRailPages.transform.localPosition = new Vector2(-7.4818f, - 4.12f);
            return (subRailPages, subRailBookmarkContainer);
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
