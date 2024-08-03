using HarmonyLib;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(BaboonBirdAI))]
internal class BaboonBirdAIPatches
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    static public void Start_Postfix(BaboonBirdAI __instance)
    {
        //Fix baboon hawks requiring very high speed to run over
        __instance.enemyType.SizeLimit = NavSizeLimit.NoLimit;
    }
}
