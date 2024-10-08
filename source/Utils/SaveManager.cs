﻿namespace CruiserImproved.Utils;
internal static class SaveManager
{
    static string SavePrefix = "CruiserImproved.";
    public static void Save<T>(string key, T data)
    {
        ES3.Save(SavePrefix + key, data, GameNetworkManager.Instance.currentSaveFileName);
    }

    public static bool TryLoad<T>(string key, out T data)
    {
        if (!ES3.KeyExists(SavePrefix + key, GameNetworkManager.Instance.currentSaveFileName))
        {
            data = default;
            return false;
        }
        data = ES3.Load<T>(SavePrefix + key, GameNetworkManager.Instance.currentSaveFileName);
        return true;
    }

    public static void Delete(string key)
    {
        ES3.DeleteKey(SavePrefix + key, GameNetworkManager.Instance.currentSaveFileName);
    }
}
