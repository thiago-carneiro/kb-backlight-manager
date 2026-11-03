# KB Backlight Manager

ThinkPads turn the keyboard backlight off when the screen turns off, after sleep and at every start, and leave it off. KB Backlight Manager puts it back.

It sits in the notification area and remembers the level you choose with **Fn+Space** (off, low or high). When the screen turns on, after sleep and when Windows starts, it restores that level. Choosing "off" with Fn+Space is respected: it stays off.

It is a single 2.4 MB program with no window: about 4 MB of memory, and it stops checking the keyboard while the screen is off.

## Requirements

- A Lenovo ThinkPad with a keyboard backlight.
- Lenovo's power management driver, which comes with the ThinkPad drivers (Lenovo System Update, Lenovo Vantage or the drivers from Lenovo's support site). Without it, the app says so and closes.
- Windows 10 or 11.

Tested on a ThinkPad T14 Gen 2. The driver's interface is not documented; if it does not work on your model, please [open an issue](https://github.com/thiago-carneiro/kb-backlight-manager/issues) with the model name.

## Using it

Run `KbBacklightManager.exe`. Its icon, a keyboard with lit keys, appears in the notification area; click it for the menu:

- **Keyboard backlight: High** — the level it keeps, the last one you chose with Fn+Space.
- **Restore the backlight automatically** — turn the restoring on or off.
- **Start with Windows** — so the backlight is back from the first sign-in.
- **Project page on GitHub** and **Support on Ko-fi**.
- **Exit**.

The menu is in Portuguese when Windows is, and in English otherwise.

## Privacy

KB Backlight Manager talks only to Lenovo's driver, to read and set the backlight level, and listens for the display turning on and off and for sleep. It never reads what you type, makes no network connections and has no telemetry. It keeps two settings (restore on or off, and the level) in `%LocalAppData%\KB Backlight Manager\settings.json`, and a log there only when started with `--log`.

## How it works

Lenovo's driver (`\\.\IBMPmDrv`) exposes the keyboard backlight to applications through two control codes: one reads the level, one sets it. The app follows the level every 3 seconds while the screen is on, so it knows what you chose with Fn+Space (a new level counts only when read twice in a row, so the firmware turning the light off is not taken for your choice). When Windows reports the display on, or the PC waking up, it restores that level, and checks again a few seconds later because the firmware sometimes turns the light off right after waking up.

Hat tip to [SaltwaterC/KeyboardBacklightForLenovo](https://github.com/SaltwaterC/KeyboardBacklightForLenovo), which showed that Lenovo's own driver exposes the keyboard backlight to applications. This implementation is its own. It started as part of [AutoLayout](https://github.com/thiago-carneiro/AutoLayout), which sets the keyboard layout by the keyboards connected, and became its own app because the backlight has nothing to do with layouts.

## Building

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download) and, because the app is compiled ahead of time (Native AOT), the Visual Studio C++ build tools ("Desktop development with C++", with the ARM64 tools for ARM64).

```powershell
.\build.ps1   # tests, then dist\win-x64\KbBacklightManager.exe and dist\win-arm64\KbBacklightManager.exe
```

The linker fails on paths longer than 260 characters; clone to a short path such as `C:\src\kb-backlight-manager` if the build stops with `LNK1104`. `tools\make-icon.ps1` redraws the icon.

A freshly built executable is not signed, so Windows 11 may block it when Smart App Control is on.

## Support

KB Backlight Manager is free. If it keeps your keys lit and you want to say thanks, you can [support it on Ko-fi](https://ko-fi.com/thiagopcarneiro).

## License

Apache License 2.0 — see [LICENSE](LICENSE) and [NOTICE](NOTICE).
