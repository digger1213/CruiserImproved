using HarmonyLib;

namespace CruiserImproved.Patches
{
    [HarmonyPatch(typeof(BaboonBirdAI))]
    internal class BaboonBirdAIPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static public void Start_Postfix(BaboonBirdAI __instance)
        {
            __instance.enemyType.SizeLimit = NavSizeLimit.NoLimit;
        }
    }
}
