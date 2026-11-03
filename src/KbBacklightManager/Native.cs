// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace KbBacklightManager;

/// <summary>Win32 declarations. Nothing here reads, hooks or injects keystrokes.</summary>
internal static class Native
{
    /// <summary>With an empty string, removes the current directory from the DLL search order.</summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDllDirectory(string path);

    [DllImport("kernel32.dll")]
    public static extern ushort GetUserDefaultUILanguage();

    // ---- The driver ----------------------------------------------------------------------

    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint OPEN_EXISTING = 3;
    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(SafeFileHandle device, uint ioControlCode, byte[] inBuffer, int inBufferSize,
        byte[] outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    /// <summary>Copies memory of this process without crashing on a bad address.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadProcessMemory(IntPtr process, IntPtr address, [Out] byte[] buffer, IntPtr size, out IntPtr bytesRead);

    // ---- Windows, messages, timers and power -------------------------------------------------

    public const int WM_NULL = 0x0000;
    public const int WM_SETTINGCHANGE = 0x001A;
    public const int WM_CONTEXTMENU = 0x007B;
    public const int WM_TIMER = 0x0113;
    public const int WM_POWERBROADCAST = 0x0218;
    public const int WM_APP = 0x8000;

    public const int PBT_APMSUSPEND = 0x0004;
    public const int PBT_APMRESUMEAUTOMATIC = 0x0012;
    public const int PBT_POWERSETTINGCHANGE = 0x8013;
    public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;

    public const int WS_POPUP = unchecked((int)0x80000000);
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    // Power setting broadcast when the console display turns off (0), on (1) or dims (2)
    public static readonly Guid GuidConsoleDisplayState = new("6FE69556-704A-47A0-8F24-C28D936FDA47");

    public delegate IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WindowClassEx
    {
        public int Size;
        public int Style;
        public WindowProcedure WindowProc;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        public string ClassName;
        public IntPtr SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public IntPtr Window;
        public int Id;
        public IntPtr WParam;
        public IntPtr LParam;
        public int Time;
        public Point Point;
        public int Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern ushort RegisterClassEx(ref WindowClassEx windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y,
        int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr DefWindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int RegisterWindowMessage(string name);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern UIntPtr SetTimer(IntPtr hwnd, UIntPtr id, uint milliseconds, IntPtr timerProc);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool KillTimer(IntPtr hwnd, UIntPtr id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetMessage(out Message message, IntPtr window, int filterMin, int filterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TranslateMessage(ref Message message);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr DispatchMessage(ref Message message);

    [DllImport("user32.dll")]
    public static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr RegisterPowerSettingNotification(IntPtr recipient, ref Guid powerSettingGuid, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterPowerSettingNotification(IntPtr handle);

    public const int MB_OK = 0x0000;
    public const int MB_ICONINFORMATION = 0x0040;
    public const int MB_ICONWARNING = 0x0030;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr owner, string text, string caption, int type);

    // ---- Tray icon and menu -------------------------------------------------------------

    public const int NIM_ADD = 0;
    public const int NIM_MODIFY = 1;
    public const int NIM_DELETE = 2;
    public const int NIM_SETVERSION = 4;
    public const int NIF_MESSAGE = 0x01;
    public const int NIF_ICON = 0x02;
    public const int NIF_TIP = 0x04;
    public const int NIF_SHOWTIP = 0x80;
    public const int NOTIFYICON_VERSION_4 = 4;
    public const int NIN_SELECT = 0x0400;
    public const int NIN_KEYSELECT = 0x0401;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NotifyIconData
    {
        public int Size;
        public IntPtr Window;
        public int Id;
        public int Flags;
        public int CallbackMessage;
        public IntPtr Icon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;

        public int State;
        public int StateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;

        public int VersionOrTimeout;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;

        public int InfoFlags;
        public Guid Item;
        public IntPtr BalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIcon(int message, ref NotifyIconData data);

    public const int MF_STRING = 0x0000;
    public const int MF_GRAYED = 0x0001;
    public const int MF_CHECKED = 0x0008;
    public const int MF_SEPARATOR = 0x0800;
    public const int TPM_RIGHTBUTTON = 0x0002;
    public const int TPM_RETURNCMD = 0x0100;
    public const int TPM_NONOTIFY = 0x0080;

    [DllImport("user32.dll")]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AppendMenu(IntPtr menu, int flags, UIntPtr id, string? text);

    [DllImport("user32.dll")]
    public static extern int TrackPopupMenuEx(IntPtr menu, int flags, int x, int y, IntPtr window, IntPtr parameters);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out Point point);

    // The app icon in the executable's resources (the id the .NET SDK gives ApplicationIcon)
    public const int AppIconId = 32512;
    public const int LIM_SMALL = 0;

    /// <summary>The icon at the size the notification area uses at the current DPI.</summary>
    [DllImport("comctl32.dll")]
    public static extern int LoadIconMetric(IntPtr instance, IntPtr name, int metric, out IntPtr icon);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr icon);
}
