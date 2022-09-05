using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class LoadBookmarkGroupingDataFromSaveFilePatch 
    { 
        [HarmonyPatch(typeof(PotionInventoryObject), "CanBeInteractedNow")]
        public class PotionInventoryObject_CanBeInteractedNow
        {
            static bool Prefix()
            {
                return Ex.RunSafe(() => true);
            }
            static void Postfix()
            {
                Ex.RunSafe(() => { });
            }
        }
    }
}
