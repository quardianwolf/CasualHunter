using System.IO;
using Newtonsoft.Json;

namespace MHWStatOverlay.Services;

public class OverlaySettings
{
    // General
    public int PollingRateMs { get; set; } = 100;
    public bool OverlayLocked { get; set; } = true;
    public double GlobalOpacity { get; set; } = 0.85;

    // Monster Widget
    public bool ShowMonsterWidget { get; set; } = true;
    public double MonsterWidgetX { get; set; } = 20;
    public double MonsterWidgetY { get; set; } = 20;
    public double MonsterWidgetWidth { get; set; } = 300;
    public bool ShowMonsterHP { get; set; } = true;
    public bool ShowMonsterHPValues { get; set; } = true;

    // Part Widget
    public bool ShowPartWidget { get; set; } = true;
    public double PartWidgetX { get; set; } = 20;
    public double PartWidgetY { get; set; } = 200;
    public double PartWidgetWidth { get; set; } = 280;
    public bool ShowPartBreakCount { get; set; } = true;

    // Damage Widget
    public bool ShowDamageWidget { get; set; } = true;
    public double DamageWidgetX { get; set; } = 20;
    public double DamageWidgetY { get; set; } = 500;
    public double DamageWidgetWidth { get; set; } = 300;
    public bool ShowDamageNumbers { get; set; } = true;
    public bool ShowDamagePercentage { get; set; } = true;

    // Quest Widget
    public bool ShowQuestWidget { get; set; } = true;
    public double QuestWidgetX { get; set; } = 20;
    public double QuestWidgetY { get; set; } = 700;
    public double QuestWidgetWidth { get; set; } = 250;
}

public class SettingsService
{
    private readonly string _settingsPath;

    public OverlaySettings Settings { get; set; } = new();

    public event Action? SettingsChanged;

    public void NotifyChanged() => SettingsChanged?.Invoke();

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CasualHunter");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    public void Load()
    {
        if (!File.Exists(_settingsPath))
        {
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            Settings = JsonConvert.DeserializeObject<OverlaySettings>(json) ?? new OverlaySettings();
        }
        catch
        {
            Settings = new OverlaySettings();
        }
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
        File.WriteAllText(_settingsPath, json);
        SettingsChanged?.Invoke();
    }
}
