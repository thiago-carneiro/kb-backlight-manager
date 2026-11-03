// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KbBacklightManager;

/// <summary>One command of the tray menu.</summary>
/// <param name="Text">Menu text; null for a separator.</param>
/// <param name="Action">What clicking does; null for a line that only informs.</param>
/// <param name="IsChecked">Shown with a check mark.</param>
internal sealed record TrayMenuItem(string? Text, Action? Action, bool IsChecked = false)
{
    public static TrayMenuItem Separator { get; } = new(null, null);
}

/// <summary>
/// The notification-area icon, through Shell_NotifyIcon on the app's message window. A click
/// or a right-click opens the menu. It is added again when Explorer restarts.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private const int IconId = 1;
    private const int CallbackMessage = Native.WM_APP + 1;
    private const int TipMaxLength = 127;

    private readonly MessageWindow window;
    private readonly int taskbarCreatedMessage;
    private IntPtr icon;
    private string tip = string.Empty;
    private bool added;

    public TrayIcon(MessageWindow window)
    {
        this.window = window;
        taskbarCreatedMessage = Native.RegisterWindowMessage("TaskbarCreated");
        window.MessageReceived += OnMessage;
    }

    /// <summary>Builds the menu each time it opens, so it is always current.</summary>
    public Func<IReadOnlyList<TrayMenuItem>>? MenuBuilder { get; set; }

    public void Update(string newTip)
    {
        tip = newTip.Length > TipMaxLength ? newTip[..TipMaxLength] : newTip;
        if (icon == IntPtr.Zero)
        {
            LoadIcon();
        }
        Notify(added ? Native.NIM_MODIFY : Native.NIM_ADD);
    }

    public void Dispose()
    {
        window.MessageReceived -= OnMessage;
        if (added)
        {
            Notify(Native.NIM_DELETE);
            added = false;
        }
        if (icon != IntPtr.Zero)
        {
            Native.DestroyIcon(icon);
            icon = IntPtr.Zero;
        }
    }

    /// <summary>The app icon from the executable, at the notification area's size for the current DPI.</summary>
    private void LoadIcon()
    {
        IntPtr old = icon;
        int hresult = Native.LoadIconMetric(Native.GetModuleHandle(null), (IntPtr)Native.AppIconId, Native.LIM_SMALL, out icon);
        if (hresult < 0)
        {
            Log.Write("The icon was not found in the executable (0x" + hresult.ToString("X8", System.Globalization.CultureInfo.InvariantCulture) + ")");
        }
        if (old != IntPtr.Zero)
        {
            Native.DestroyIcon(old);
        }
    }

    private void Notify(int message)
    {
        Native.NotifyIconData data = new()
        {
            Size = Marshal.SizeOf<Native.NotifyIconData>(),
            Window = window.Handle,
            Id = IconId,
            Flags = Native.NIF_MESSAGE | Native.NIF_ICON | Native.NIF_TIP | Native.NIF_SHOWTIP,
            CallbackMessage = CallbackMessage,
            Icon = icon,
            Tip = tip,
            Info = string.Empty,
            InfoTitle = string.Empty,
        };
        if (Native.Shell_NotifyIcon(message, ref data))
        {
            if (message == Native.NIM_ADD)
            {
                added = true;
                data.VersionOrTimeout = Native.NOTIFYICON_VERSION_4;
                Native.Shell_NotifyIcon(Native.NIM_SETVERSION, ref data);
            }
        }
        else if (message == Native.NIM_MODIFY)
        {
            // Explorer may have restarted without telling us: add it again
            added = false;
            Notify(Native.NIM_ADD);
        }
    }

    private bool OnMessage(int message, IntPtr wParam, IntPtr lParam, out IntPtr result)
    {
        result = IntPtr.Zero;
        if (message == taskbarCreatedMessage && taskbarCreatedMessage != 0)
        {
            added = false;
            LoadIcon();
            Notify(Native.NIM_ADD);
            return true;
        }
        if (message == Native.WM_SETTINGCHANGE && added)
        {
            // Scaling may have changed: the icon at the new size
            LoadIcon();
            Notify(Native.NIM_MODIFY);
            return false;
        }
        if (message != CallbackMessage)
        {
            return false;
        }

        int notification = unchecked((int)lParam.ToInt64()) & 0xFFFF;
        if (notification is Native.NIN_SELECT or Native.NIN_KEYSELECT or Native.WM_CONTEXTMENU)
        {
            long position = wParam.ToInt64();
            ShowMenu((short)(position & 0xFFFF), (short)((position >> 16) & 0xFFFF));
            return true;
        }
        return false;
    }

    private void ShowMenu(int x, int y)
    {
        IReadOnlyList<TrayMenuItem> items = MenuBuilder?.Invoke() ?? Array.Empty<TrayMenuItem>();
        IntPtr menu = Native.CreatePopupMenu();
        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                TrayMenuItem item = items[i];
                if (item.Text == null)
                {
                    Native.AppendMenu(menu, Native.MF_SEPARATOR, UIntPtr.Zero, null);
                    continue;
                }
                int flags = Native.MF_STRING | (item.Action == null ? Native.MF_GRAYED : 0) | (item.IsChecked ? Native.MF_CHECKED : 0);
                Native.AppendMenu(menu, flags, (UIntPtr)(i + 1), item.Text);
            }

            // Required for the menu to close when clicking elsewhere
            Native.SetForegroundWindow(window.Handle);
            int chosen = Native.TrackPopupMenuEx(menu, Native.TPM_RIGHTBUTTON | Native.TPM_RETURNCMD | Native.TPM_NONOTIFY,
                x, y, window.Handle, IntPtr.Zero);
            Native.PostMessage(window.Handle, Native.WM_NULL, IntPtr.Zero, IntPtr.Zero);
            if (chosen > 0 && chosen <= items.Count)
            {
                items[chosen - 1].Action?.Invoke();
            }
        }
        finally
        {
            Native.DestroyMenu(menu);
        }
    }
}
