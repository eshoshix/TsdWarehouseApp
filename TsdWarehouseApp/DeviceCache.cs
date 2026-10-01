using System;
using System.IO;

public static class DeviceCache
{
    private static readonly string TsdNumberPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "tsd_config.txt"
    );

    private static readonly string TsdDefectsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "tsd_defects.txt"
    );

    public static string GetTsdNumber()
    {
        if (File.Exists(TsdNumberPath))
        {
            return File.ReadAllText(TsdNumberPath).Trim();
        }
        return string.Empty;
    }

    public static void SaveTsdNumber(string tsdNumber)
    {
        File.WriteAllText(TsdNumberPath, tsdNumber);
    }

    public static string GetTsdDefects()
    {
        if (File.Exists(TsdDefectsPath))
        {
            return File.ReadAllText(TsdDefectsPath).Trim();
        }
        return string.Empty;
    }

    public static void SaveTsdDefects(string defects)
    {
        File.WriteAllText(TsdDefectsPath, defects);
    }
}