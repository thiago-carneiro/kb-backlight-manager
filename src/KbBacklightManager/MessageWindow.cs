// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KbBacklightManager;

/// <summary>
/// A hidden top-level window that receives notifications (power, display, tray icon) and runs
/// timers, on the thread that creates it. It is top-level, not message-only, because
/// message-only windows do not receive broadcasts.
/// </summary>
internal sealed class MessageWindow : IDisposable
{
    public delegate bool MessageHandler(int message, IntPtr wParam, IntPtr lParam, out IntPtr result);

    private const string ClassName = "KbBacklightManager.MessageWindow";

    // Kept alive for as long as the class is registered
    private static readonly Native.WindowProcedure Procedure = WindowProc;
    private static readonly Dictionary<IntPtr, MessageWindow> Windows = new();
    private static bool classRegistered;

    private readonly Dictionary<uint, Action> timers = new();

    public MessageWindow()
    {
        RegisterClass();
        Handle = Native.CreateWindowEx(Native.WS_EX_TOOLWINDOW, ClassName, string.Empty, Native.WS_POPUP,
            0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, Native.GetModuleHandle(null), IntPtr.Zero);
        if (Handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        Windows[Handle] = this;
    }

    /// <summary>Handlers are called in order until one returns true.</summary>
    public event MessageHandler? MessageReceived;

    public IntPtr Handle { get; private set; }

    /// <summary>Starts (or restarts) a repeating timer; it runs until stopped.</summary>
    public void StartTimer(uint milliseconds, Action callback, uint id)
    {
        timers[id] = callback;
        Native.SetTimer(Handle, (UIntPtr)id, milliseconds, IntPtr.Zero);
    }

    public void StopTimer(uint id)
    {
        if (timers.Remove(id))
        {
            Native.KillTimer(Handle, (UIntPtr)id);
        }
    }

    public void Dispose()
    {
        if (Handle == IntPtr.Zero)
        {
            return;
        }
        foreach (uint id in timers.Keys)
        {
            Native.KillTimer(Handle, (UIntPtr)id);
        }
        timers.Clear();
        Windows.Remove(Handle);
        Native.DestroyWindow(Handle);
        Handle = IntPtr.Zero;
    }

    private static void RegisterClass()
    {
        if (classRegistered)
        {
            return;
        }
        Native.WindowClassEx windowClass = new()
        {
            Size = Marshal.SizeOf<Native.WindowClassEx>(),
            WindowProc = Procedure,
            Instance = Native.GetModuleHandle(null),
            ClassName = ClassName,
        };
        if (Native.RegisterClassEx(ref windowClass) == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        classRegistered = true;
    }

    private static IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (Windows.TryGetValue(hwnd, out MessageWindow? window))
            {
                if (message == Native.WM_TIMER)
                {
                    uint id = unchecked((uint)wParam.ToInt64());
                    if (window.timers.TryGetValue(id, out Action? callback))
                    {
                        callback();
                        return IntPtr.Zero;
                    }
                }

                MessageHandler? handlers = window.MessageReceived;
                if (handlers != null)
                {
                    foreach (MessageHandler handler in handlers.GetInvocationList())
                    {
                        if (handler(message, wParam, lParam, out IntPtr result))
                        {
                            return result;
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            // Exceptions must not cross back into Windows
            Log.Write("Error handling message 0x" + message.ToString("X4", CultureInfo.InvariantCulture) + ": " + error);
        }
        return Native.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
