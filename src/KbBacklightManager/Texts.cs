// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace KbBacklightManager;

/// <summary>The app's few texts, in English and Portuguese, chosen by the Windows display language.</summary>
internal static class Texts
{
    public const string AppName = "KB Backlight Manager";
    public const string ProjectUrl = "https://github.com/thiago-carneiro/kb-backlight-manager";
    public const string TipUrl = "https://ko-fi.com/thiagopcarneiro";

    internal static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Level"] = "Keyboard backlight: {0}",
        ["Off"] = "Off",
        ["Low"] = "Low",
        ["High"] = "High",
        ["Restore"] = "Restore the backlight automatically",
        ["Startup"] = "Start with Windows",
        ["Project"] = "Project page on GitHub",
        ["Tip"] = "Support on Ko-fi",
        ["Exit"] = "Exit",
        ["NoDriver"] = "KB Backlight Manager needs a Lenovo ThinkPad with Lenovo's power management driver, which comes with the ThinkPad drivers. It was not found on this PC, so the app will close.",
        ["Running"] = "KB Backlight Manager is already running in the notification area.",
    };

    internal static readonly IReadOnlyDictionary<string, string> Portuguese = new Dictionary<string, string>
    {
        ["Level"] = "Luz do teclado: {0}",
        ["Off"] = "Desligada",
        ["Low"] = "Baixa",
        ["High"] = "Alta",
        ["Restore"] = "Restaurar a luz do teclado automaticamente",
        ["Startup"] = "Iniciar com o Windows",
        ["Project"] = "Página do projeto no GitHub",
        ["Tip"] = "Apoiar no Ko-fi",
        ["Exit"] = "Sair",
        ["NoDriver"] = "O KB Backlight Manager precisa de um ThinkPad da Lenovo com o driver de gerenciamento de energia da Lenovo, que vem com os drivers do ThinkPad. Ele não foi encontrado neste PC, então o app vai fechar.",
        ["Running"] = "O KB Backlight Manager já está aberto na área de notificação.",
    };

    // Portuguese (any region) when Windows is in Portuguese; English otherwise
    private static readonly IReadOnlyDictionary<string, string> Current =
        (Native.GetUserDefaultUILanguage() & 0x3FF) == 0x16 ? Portuguese : English;

    public static string Get(string key) => Current.TryGetValue(key, out string? text) ? text : English[key];

    public static string Level(BacklightLevel level) => Get(level.ToString());

    /// <summary>"Keyboard backlight: High".</summary>
    public static string LevelLine(BacklightLevel level) => Get("Level").Replace("{0}", Level(level), StringComparison.Ordinal);
}
