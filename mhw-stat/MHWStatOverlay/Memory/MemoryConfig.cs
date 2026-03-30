using System.IO;
using MHWStatOverlay.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MHWStatOverlay.Memory;

public class MemoryConfig
{
    public string ProcessName { get; set; } = "MonsterHunterWorld";
    public PatternDef MonsterPattern { get; set; } = new();
    public PatternDef PlayerDamagePattern { get; set; } = new();
    public PatternDef PlayerNamePattern { get; set; } = new();
    public PatternDef CurrentPlayerNamePattern { get; set; } = new();
    public PatternDef CurrentWeaponPattern { get; set; } = new();
    public PatternDef PlayerBuffPattern { get; set; } = new();
    public PatternDef SelectedMonsterPattern { get; set; } = new();
    public PatternDef LobbyStatusPattern { get; set; } = new();
    public PatternDef DamageOnScreenPattern { get; set; } = new();

    public static MemoryConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new MemoryConfig();

        var json = File.ReadAllText(path);
        var jObj = JObject.Parse(json);

        var config = new MemoryConfig
        {
            ProcessName = jObj["ProcessName"]?.Value<string>() ?? "MonsterHunterWorld"
        };

        config.MonsterPattern = ParsePattern(jObj, "MonsterPattern");
        config.PlayerDamagePattern = ParsePattern(jObj, "PlayerDamagePattern");
        config.PlayerNamePattern = ParsePattern(jObj, "PlayerNamePattern");
        config.CurrentPlayerNamePattern = ParsePattern(jObj, "CurrentPlayerNamePattern");
        config.CurrentWeaponPattern = ParsePattern(jObj, "CurrentWeaponPattern");
        config.PlayerBuffPattern = ParsePattern(jObj, "PlayerBuffPattern");
        config.SelectedMonsterPattern = ParsePattern(jObj, "SelectedMonsterPattern");
        config.LobbyStatusPattern = ParsePattern(jObj, "LobbyStatusPattern");
        config.DamageOnScreenPattern = ParsePattern(jObj, "DamageOnScreenPattern");

        return config;
    }

    private static PatternDef ParsePattern(JObject root, string key)
    {
        var token = root[key];
        if (token == null) return new PatternDef { Name = key };

        return new PatternDef
        {
            Name = token["Name"]?.Value<string>() ?? key,
            PatternString = token["PatternString"]?.Value<string>() ?? "",
            LastResultAddress = token["LastResultAddress"]?.Value<string>()
        };
    }

    public void SaveToFile(string path)
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
