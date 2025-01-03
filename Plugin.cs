﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace PotionCraftBookmarkOrganizer
{
    [BepInPlugin(PLUGIN_GUID, "PotionCraftBookmarkOrganizer", PLUGIN_VERSION)]
    [BepInProcess("Potion Craft.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.fahlgorithm.potioncraftbookmarkorganizer";
        public const string PLUGIN_VERSION = "2.0.0.0";

        public static ManualLogSource PluginLogger {get; private set; }

        private void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_GUID);
            PluginLogger.LogInfo($"Plugin {PLUGIN_GUID}: Patch Succeeded!");
        }
    }
}
