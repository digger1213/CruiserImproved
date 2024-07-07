using HarmonyLib;

namespace DiggCruiserImproved.Patches
{
    [HarmonyPatch(typeof(BaboonBirdAI))]
    internal class BaboonBirdAIPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static public void Start_Postfix(BaboonBirdAI __instance)
        {
            CruiserImproved.Log.LogMessage("Patched Baboon sizeLimit from " + __instance.enemyType.SizeLimit + " to " + NavSizeLimit.NoLimit);
            __instance.enemyType.SizeLimit = NavSizeLimit.NoLimit;
        }
    }
}
