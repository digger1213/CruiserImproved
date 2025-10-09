namespace CruiserImproved;

using LCVR.Player;
using System.Runtime.CompilerServices;

internal static class LCVRCompatibility
{
    public static string modGUID = "io.daxcess.lcvr";

    public static bool modEnabled
    {
        get
        {
            if(_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGUID);
            }
            return (bool)_enabled;
        }
    }

    public static bool inVrSession
    {
        get
        {
            if (modEnabled) return GetInVrSession();
            else return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static bool GetInVrSession()
    {
        return VRSession.InVR;
    }

   private static bool? _enabled;
}
