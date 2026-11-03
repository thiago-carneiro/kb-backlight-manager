// Copyright 2026 The KB Backlight Manager authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KbBacklightManager.Tests;

[TestClass]
public sealed class DriverAnswerTests
{
    [TestMethod]
    [DataRow(0x00050200u, BacklightLevel.Off)]
    [DataRow(0x00050201u, BacklightLevel.Low)]
    [DataRow(0x00050202u, BacklightLevel.High)]
    public void ReadyAnswers_GiveTheLevel(uint answer, BacklightLevel level)
    {
        Assert.AreEqual(level, ThinkPadBacklight.ParseAnswer(answer));
    }

    [TestMethod]
    [DataRow(0x00050203u)]   // a level the keyboard does not have
    [DataRow(0x00040202u)]   // not the "ready" pattern
    [DataRow(0x00000000u)]
    [DataRow(0xFFFFFFFFu)]
    public void OtherAnswers_AreNotLevels(uint answer)
    {
        Assert.IsNull(ThinkPadBacklight.ParseAnswer(answer));
    }

    [TestMethod]
    public void TheDriverIsQueriedWithoutThrowing()
    {
        // On a ThinkPad this reads the level; elsewhere it is simply unavailable
        _ = ThinkPadBacklight.TryGetLevel(out _);
    }
}

[TestClass]
public sealed class SettingsTests
{
    private string folder = string.Empty;

    [TestInitialize]
    public void CreateFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "KbBacklightManagerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(folder, true);

    private AppSettings LoadText(string json)
    {
        string path = Path.Combine(folder, "settings.json");
        File.WriteAllText(path, json);
        return new SettingsStore(path).Load();
    }

    [TestMethod]
    public void SavedSettings_LoadBackTheSame()
    {
        string path = Path.Combine(folder, "sub", "settings.json");
        new SettingsStore(path).Save(new AppSettings { Enabled = false, Level = BacklightLevel.Low });
        new SettingsStore(path).Save(new AppSettings { Enabled = false, Level = BacklightLevel.Low });   // replacing a file
        AppSettings loaded = new SettingsStore(path).Load();
        Assert.IsFalse(loaded.Enabled);
        Assert.AreEqual(BacklightLevel.Low, loaded.Level);
        StringAssert.Contains(File.ReadAllText(path), "\"Low\"", "Levels are saved by name");
    }

    [TestMethod]
    public void MissingFile_GivesTheDefaults()
    {
        AppSettings settings = new SettingsStore(Path.Combine(folder, "none.json")).Load();
        Assert.IsTrue(settings.Enabled);
        Assert.AreEqual(BacklightLevel.High, settings.Level);
    }

    [TestMethod]
    [DataRow("{\"Level\":")]
    [DataRow("{\"Level\":\"Blinding\"}")]
    [DataRow("not json")]
    [DataRow("")]
    public void BrokenFiles_GiveTheDefaults(string json)
    {
        AppSettings settings = LoadText(json);
        Assert.IsTrue(settings.Enabled);
        Assert.AreEqual(BacklightLevel.High, settings.Level);
    }

    [TestMethod]
    public void UndefinedLevel_IsReplaced()
    {
        Assert.AreEqual(BacklightLevel.High, LoadText("{\"Level\": 7}").Level);
    }

    [TestMethod]
    public void HugeFile_IsNotRead()
    {
        Assert.AreEqual(BacklightLevel.High, LoadText("{\"Level\":\"Low\"" + new string(' ', 100_000) + "}").Level);
    }
}

[TestClass]
public sealed class TextTests
{
    [TestMethod]
    public void BothLanguages_HaveTheSameTexts_AndPlaceholders()
    {
        CollectionAssert.AreEquivalent(Texts.English.Keys.ToList(), Texts.Portuguese.Keys.ToList());
        foreach (string key in Texts.English.Keys)
        {
            Assert.AreEqual(Texts.English[key].Contains("{0}", StringComparison.Ordinal),
                Texts.Portuguese[key].Contains("{0}", StringComparison.Ordinal), key);
        }
    }

    [TestMethod]
    public void EveryLevel_HasAName()
    {
        foreach (BacklightLevel level in Enum.GetValues<BacklightLevel>())
        {
            Assert.IsFalse(string.IsNullOrEmpty(Texts.Level(level)));
        }
    }

    [TestMethod]
    public void LogLines_CannotBeSplit()
    {
        Assert.AreEqual("a\\u000D\\u000Ab", Log.OneLine("a\r\nb"));
    }
}
