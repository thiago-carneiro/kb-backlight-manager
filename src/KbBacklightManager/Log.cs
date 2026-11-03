// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace KbBacklightManager;

/// <summary>
/// Plain-text log, written only when the app is started with --log, and kept under a few
/// hundred KB: what happened to the backlight and when.
/// </summary>
internal static class Log
{
    private const long MaxSize = 256 * 1024;
    private static readonly object Sync = new();

    public static bool Enabled { get; set; }

    public static string FilePath => Path.Combine(AppPaths.DataDirectory, "log.txt");

    public static void Write(string message)
    {
        if (!Enabled)
        {
            return;
        }
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);
                FileInfo info = new(FilePath);
                if (info.Exists && info.Length > MaxSize)
                {
                    // Keep the previous log as a single backup
                    File.Delete(FilePath + ".1");
                    File.Move(FilePath, FilePath + ".1");
                }
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "  "
                    + OneLine(message) + Environment.NewLine;
                File.AppendAllText(FilePath, line, Encoding.UTF8);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Logging must never break the app
        }
    }

    /// <summary>Control characters as \uXXXX, so one event is always one line.</summary>
    internal static string OneLine(string text)
    {
        StringBuilder builder = new(text.Length);
        foreach (char c in text)
        {
            if (char.IsControl(c))
            {
                builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
}

/// <summary>Where the app keeps its settings and log.</summary>
internal static class AppPaths
{
    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KB Backlight Manager");
}
