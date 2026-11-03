// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Win32;

namespace KbBacklightManager;

/// <summary>
/// "Start with Windows", through the current user's Run key (listed in Task Manager's
/// Startup apps). The state is read from the registry, so it is never out of date.
/// </summary>
internal static class Startup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KB Backlight Manager";

    private static string Command => "\"" + (Environment.ProcessPath ?? throw new InvalidOperationException("Unknown executable path")) + "\"";

    public static bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }
}
