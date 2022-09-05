using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using System.Linq;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    /// <summary>
    /// If the index is in this bookmark controller and it is hidden return the group bookmark in its place
    /// </summary>
    public class ReturnDummyBookmarkForHiddenBookmarkByIndexPatch
    { 
        [HarmonyPatch(typeof(BookmarkController), "GetBookmarkByIndex")]
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
