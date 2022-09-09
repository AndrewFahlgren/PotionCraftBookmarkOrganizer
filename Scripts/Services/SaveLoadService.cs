using Newtonsoft.Json;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

                var serializedGroups = JsonConvert.SerializeObject(StaticStorage.BookmarkGroups);

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
            var deserialized = JsonConvert.DeserializeObject<BookmarkGroupsDeserialized>(stateJsonString);
            if (deserialized.BookmarkGroups == null)
            {
                Plugin.PluginLogger.LogError("Error: An error occured during bookmark group deserialization");
                return true;
            }

            StaticStorage.BookmarkGroups = deserialized.BookmarkGroups;

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
            return true;
        }

        private class BookmarkGroupsDeserialized
        {
            [JsonProperty(StaticStorage.BookmarkGroupsJsonSaveName)]
            public Dictionary<int, List<BookmarkStorage>> BookmarkGroups { get; set; }
        }
    }
}
