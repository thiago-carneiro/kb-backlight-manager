// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace KbBacklightManager;

/// <summary>
/// A notification-area app with no window: it restores the ThinkPad keyboard backlight and
/// offers its few settings in the tray menu.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Never load DLLs from the current directory
        Native.SetDllDirectory(string.Empty);
        Log.Enabled = args.Contains("--log");

        // One instance per user session
        using Mutex single = new(true, @"Local\KB Backlight Manager", out bool first);
        if (!first)
        {
            _ = Native.MessageBox(IntPtr.Zero, Texts.Get("Running"), Texts.AppName, Native.MB_OK | Native.MB_ICONINFORMATION);
            return 0;
        }
        if (!ThinkPadBacklight.IsAvailable)
        {
            Log.Write("Backlight driver not found");
            _ = Native.MessageBox(IntPtr.Zero, Texts.Get("NoDriver"), Texts.AppName, Native.MB_OK | Native.MB_ICONWARNING);
            return 1;
        }

        SettingsStore store = new();
        AppSettings settings = store.Load();
        Log.Write(Texts.AppName + " started; restore " + (settings.Enabled ? "on" : "off") + ", level " + settings.Level);

        using MessageWindow window = new();
        using BacklightKeeper keeper = new(window, settings.Level);
        using TrayIcon tray = new(window);

        void UpdateTip() => tray.Update(Texts.AppName + "\n" + Texts.LevelLine(keeper.Level));

        keeper.LevelChanged += (_, _) =>
        {
            settings.Level = keeper.Level;
            store.Save(settings);
            Log.Write("Level chosen with Fn+Space: " + keeper.Level);
            UpdateTip();
        };
        tray.MenuBuilder = () => Menu(settings, store, keeper);
        UpdateTip();
        keeper.Enabled = settings.Enabled;

        while (Native.GetMessage(out Native.Message message, IntPtr.Zero, 0, 0) > 0)
        {
            Native.TranslateMessage(ref message);
            Native.DispatchMessage(ref message);
        }
        Log.Write(Texts.AppName + " stopped");
        return 0;
    }

    private static List<TrayMenuItem> Menu(AppSettings settings, SettingsStore store, BacklightKeeper keeper) => new()
    {
        new(Texts.LevelLine(keeper.Level), null),
        TrayMenuItem.Separator,
        new(Texts.Get("Restore"), () =>
        {
            settings.Enabled = !settings.Enabled;
            keeper.Enabled = settings.Enabled;
            store.Save(settings);
        }, settings.Enabled),
        new(Texts.Get("Startup"), () => Startup.SetEnabled(!Startup.IsEnabled), Startup.IsEnabled),
        TrayMenuItem.Separator,
        new(Texts.Get("Project"), () => Open(Texts.ProjectUrl)),
        new(Texts.Get("Tip"), () => Open(Texts.TipUrl)),
        TrayMenuItem.Separator,
        new(Texts.Get("Exit"), () => Native.PostQuitMessage(0)),
    };

    private static void Open(string url)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception error)
        {
            Log.Write("Could not open " + url + ": " + error.Message);
        }
    }
}
