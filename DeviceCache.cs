using System;
using System.IO;

public static class DeviceCache
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "tsd_config.txt"
    );

    public static string GetTsdNumber()
    {
        if (File.Exists(FilePath))
        {
            return File.ReadAllText(FilePath).Trim();
        }
        return string.Empty;
    }

    public static void SaveTsdNumber(string tsdNumber)
    {
        File.WriteAllText(FilePath, tsdNumber);
    }
}