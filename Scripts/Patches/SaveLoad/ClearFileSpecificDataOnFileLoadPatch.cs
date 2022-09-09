using HarmonyLib;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.SaveFileSystem;
using PotionCraftBookmarkOrganizer.Scripts.Services;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class ClearFileSpecificDataOnFileLoadPatch
    {
        [HarmonyPatch(typeof(SaveLoadManager), "LoadFile")]
        public class SaveLoadManager_LoadFile
        {
            static bool Prefix()
            {
                return Ex.RunSafe(() => SaveLoadService.ClearFileSpecificDataOnFileLoad());
            }
        }
    }
}
