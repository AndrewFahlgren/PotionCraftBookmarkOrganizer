using HarmonyLib;
using PotionCraft.Assemblies.GamepadNavigation;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class FixUnknownSlotCrash
    { 
        [HarmonyPatch(typeof(Slot), "SaveSettingsFromChild")]
        public class Slot_SaveSettingsFromChild
        {
            static bool Prefix(Slot __instance)
            {
                return Ex.RunSafe(() => NullCheckCursorAnchorSubObject(__instance));
            }
        }

        private static bool NullCheckCursorAnchorSubObject(Slot instance)
        {
            if (instance.cursorAnchorSubObject == null) return false;
            if (instance.cursorAnchorSubObject.transform == null) return false;
            return true;
        }

    }
}
