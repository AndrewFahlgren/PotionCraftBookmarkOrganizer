﻿using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class LoadBookmarkGroupingDataFromSaveFilePatch 
    { 
        [HarmonyPatch(typeof(MapState), "LoadState")]
        public class MapState_LoadState
        {
            //static bool Prefix()
            //{
            //    return Ex.RunSafe(() => true);
            //}
            static void Postfix()
            {
                Ex.RunSafe(() => LoadBookmarkGroupingDataFromSaveFile());
            }
        }

        private static void LoadBookmarkGroupingDataFromSaveFile()
        {
        }
    }
}
