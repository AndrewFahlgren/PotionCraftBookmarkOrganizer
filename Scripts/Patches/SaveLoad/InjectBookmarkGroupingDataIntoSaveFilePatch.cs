using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraftBookmarkOrganizer.Scripts.Services;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class InjectBookmarkGroupingDataIntoSaveFilePatch
    { 
        [HarmonyPatch(typeof(SavedState), "ToJson")]
        public class SavedState_ToJson
        {
            static void Postfix(ref string __result)
            {
                SaveLoadService.StoreBookmarkGroups(ref __result);
            }
        }
    }
}
