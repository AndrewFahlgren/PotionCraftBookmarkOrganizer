using HarmonyLib;
using PotionCraft.SaveFileSystem;
using PotionCraftBookmarkOrganizer.Scripts.Services;

namespace PotionCraftBookmarkOrganizer.Scripts.Patches
{
    public class RetrieveStateJsonStringPatch
    {
        [HarmonyPatch(typeof(File), "Load")]
        public class File_Load
        {
            static bool Prefix(File __instance)
            {
                return Ex.RunSafe(() => SaveLoadService.RetrieveStateJsonString(__instance));
            }
        }
    }
}
