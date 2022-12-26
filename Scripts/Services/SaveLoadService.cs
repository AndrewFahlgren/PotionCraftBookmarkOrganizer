using Newtonsoft.Json;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PotionCraftBookmarkOrganizer.Scripts.Services
{
    /// <summary>
    /// Service responsible for 
    /// </summary>
    public static class SaveLoadService
    {
        /// <summary>
        /// Stores bookmark groups dictionary at the end of the save file with a custom field name
        /// </summary>
        public static void StoreBookmarkGroups(ref string result)
        {
            string modifiedResult = null;
            var savedStateJson = result;
            Ex.RunSafe(() =>
            {
                if (!StaticStorage.BookmarkGroups.Any()) return;
                //Serialize recipe groups to json

                var toSerialize = new StaticStorage.SavedStaticStorage
                {
                    BookmarkGroups = StaticStorage.BookmarkGroups,
                    SavedRecipePositions = StaticStorage.SavedRecipePositions,
                    ErrorLog = StaticStorage.ErrorLog,
                    BookmarkManagerVersion = Plugin.PLUGIN_GUID
                };
                var serializedGroups = JsonConvert.SerializeObject(toSerialize, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                var serialized = $",\"{StaticStorage.BookmarkGroupsJsonSaveName}\":{serializedGroups}";
                //Insert custom field at the end of the save file at the top level
                var insertIndex = savedStateJson.LastIndexOf('}');
                modifiedResult = savedStateJson.Insert(insertIndex, serialized);
            });
            if (!string.IsNullOrEmpty(modifiedResult))
            {
                result = modifiedResult;
            }
        }

        /// <summary>
        /// Reads the raw json string to find our custom field and parse any bookmark groups within it
        /// </summary>
        public static bool RetreiveStoredBookmarkGroups(Type type)
        {
            if (type != typeof(ProgressState)) return true;
            var stateJsonString = StaticStorage.StateJsonString;
            StaticStorage.StateJsonString = null;
            if (string.IsNullOrEmpty(stateJsonString))
            {
                Plugin.PluginLogger.LogInfo("Error: stateJsonString is empty. Cannot load bookmark groups.");
                return true;
            }

            //Check if there are any existing bookmark groups in save file
            var keyIndex = stateJsonString.IndexOf(StaticStorage.BookmarkGroupsJsonSaveName);
            if (keyIndex == -1)
            {
                Plugin.PluginLogger.LogInfo("No existing bookmark groups found during load");
                return true;
            }

            //Deserialize the bookmark groups from json using our dummy class
            var deserialized = JsonConvert.DeserializeObject<BookmarkGroupsDeserialized>(stateJsonString, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            if (deserialized.SavedStaticStorage?.BookmarkGroups == null)
            {
                Plugin.PluginLogger.LogError("Error: An error occured during bookmark group deserialization");
                return true;
            }

            StaticStorage.BookmarkGroups = deserialized.SavedStaticStorage.BookmarkGroups;
            StaticStorage.SavedRecipePositions = deserialized.SavedStaticStorage.SavedRecipePositions;

            return true;
        }

        /// <summary>
        /// This method retrieves the raw json string and stores it in static storage for later use.
        /// The StateJsonString is inaccessible later on when we need it so this method is necessary to provide access to it.
        /// </summary>
        public static bool RetrieveStateJsonString(File instance)
        {
            StaticStorage.StateJsonString = instance.StateJsonString;
            return true;
        }

        /// <summary>
        /// Clears out any stored static data from a previous game file if this isn't the first load of the session
        /// </summary>
        public static bool ClearFileSpecificDataOnFileLoad()
        {
            StaticStorage.BookmarkGroups.Clear();
            StaticStorage.SavedRecipePositions = null;
            return true;
        }

        private class BookmarkGroupsDeserialized
        {
            [JsonProperty(StaticStorage.BookmarkGroupsJsonSaveName)]
            public StaticStorage.SavedStaticStorage SavedStaticStorage { get; set; }
        }

        public static Sprite GenerateSpriteFromImage(string path, Vector2? pivot = null, bool createComplexMesh = false)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            byte[] data;
            using (var memoryStream = new System.IO.MemoryStream())
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
