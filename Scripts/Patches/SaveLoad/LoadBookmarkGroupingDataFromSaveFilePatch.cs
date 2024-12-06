using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.IO;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class LoadBookmarkGroupingDataFromSaveFilePatch
    { 
        [HarmonyPatch(typeof(SaveLoadManager), "LoadProgressState")]
        public class SaveLoadManager_LoadProgressState
        {
            static bool Prefix()
            {
                return Ex.RunSafe(SaveLoadService.RetreiveStoredBookmarkGroups);
            }
        }
    }
}
