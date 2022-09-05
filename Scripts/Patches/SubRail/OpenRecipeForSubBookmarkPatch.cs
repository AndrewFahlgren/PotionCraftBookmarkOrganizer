using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// Perform any shenanigans that are needed to get the right recipe on screen and setup the subRail bookmarks to show this as the active bookmark
    /// </summary>
    public class OpenRecipeForSubBookmarkPatch
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
