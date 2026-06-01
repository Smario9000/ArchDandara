/*
 * ArchDandara documentation
 * Purpose: Routes mod logging through category and level filters.
 * Why: Central logging lets diagnostics go to file while keeping MelonLoader console output under player control.
 * Notes: All user-visible diagnostic output should use this wrapper so Debug.txt and category filtering work consistently.
 */

using System;
using System.IO;
using ArchDandara.Config;
using MelonLoader;
using MelonLoader.Utils;

public static class MLLog
{
    public static void Msg(string message)
    {
        Write("Msg", message, null);
    }

    public static void Warning(string message)
    {
        Write("Warning", message, null);
    }

    public static void Error(string message)
    {
        Write("Error", message, null);
    }

    private static void Write(string level, string message, Exception exception)
    {
        // Console visibility is controlled by MLDandaraConfig. Suppressed logs can still be
        // appended to Latest.log so deep debugging data is available without flooding the console.
        if (MLDandaraConfig.ShouldPrintToConsole(level, message))
        {
            WriteConsole(level, message);
            return;
        }

        if (MLDandaraConfig.FileOnlySuppressedLogs)
            WriteFileOnly(level, message, exception);
    }

    private static void WriteConsole(string level, string message)
    {
        switch (level)
        {
            case "Warning":
                MelonLogger.Warning(message);
                break;
            case "Error":
                MelonLogger.Error(message);
                break;
            default:
                MelonLogger.Msg(message);
                break;
        }
    }

    private static void WriteFileOnly(string level, string message, Exception exception)
    {
        try
        {
            string path = GetLatestLogPath();
            if (string.IsNullOrEmpty(path))
                return;

            using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] [Dandara Randomizer] " +
                                 FormatLevel(level) + message);
                if (exception != null)
                    writer.WriteLine(exception);
            }
        }
        catch
        {
        }
    }

    private static string FormatLevel(string level)
    {
        if (level == "Warning")
            return "[Warning] ";
        if (level == "Error")
            return "[Error] ";

        return "";
    }

    private static string GetLatestLogPath()
    {
        // MelonLoader keeps Latest.log beside the game folder, not inside this mod's data folder.
        // Resolve it dynamically so portable installs and alternate drive paths still work.
        string userData = MelonEnvironment.UserDataDirectory;
        if (string.IsNullOrEmpty(userData))
            return null;

        DirectoryInfo userDataDirectory = new DirectoryInfo(userData);
        DirectoryInfo gameDirectory = userDataDirectory.Parent;
        if (gameDirectory == null)
            return null;

        string logDirectory = Path.Combine(gameDirectory.FullName, "MelonLoader");
        if (!Directory.Exists(logDirectory))
            return null;

        return Path.Combine(logDirectory, "Latest.log");
    }
}
