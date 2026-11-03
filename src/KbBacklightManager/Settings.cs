// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KbBacklightManager;

public sealed class AppSettings
{
    /// <summary>Restore the backlight automatically.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The level last chosen with Fn+Space.</summary>
    public BacklightLevel Level { get; set; } = BacklightLevel.High;
}

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in the app's data folder. A file that
/// cannot be read gives the defaults; values are checked after loading.
/// </summary>
internal sealed class SettingsStore
{
    private const int MaxFileSize = 64 * 1024;

    private readonly string path;

    public SettingsStore(string? path = null)
    {
        this.path = path ?? Path.Combine(AppPaths.DataDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }
            using FileStream stream = File.OpenRead(path);
            if (stream.Length > MaxFileSize)
            {
                return new AppSettings();
            }
            return Sanitize(JsonSerializer.Deserialize(stream, SettingsJson.Default.AppSettings));
        }
        catch (Exception error) when (error is IOException or JsonException or NotSupportedException or UnauthorizedAccessException)
        {
            Log.Write("Settings could not be read (" + error.Message + "); using defaults");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(temporary, JsonSerializer.SerializeToUtf8Bytes(settings, SettingsJson.Default.AppSettings));
            // Replace in one step so a crash never leaves a half-written file
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Log.Write("Settings could not be saved: " + error.Message);
        }
    }

    internal static AppSettings Sanitize(AppSettings? settings)
    {
        settings ??= new AppSettings();
        if (!Enum.IsDefined(settings.Level))
        {
            settings.Level = BacklightLevel.High;
        }
        return settings;
    }
}

/// <summary>JSON contract for the settings file, generated at compile time (no reflection).</summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext
{
}
