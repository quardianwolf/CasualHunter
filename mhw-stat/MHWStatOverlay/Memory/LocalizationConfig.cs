using System.IO;
using Newtonsoft.Json.Linq;

namespace MHWStatOverlay.Memory;

public class LocalizationConfig
{
    public Dictionary<string, string> Strings { get; set; } = new();

    public string Get(string key)
    {
        return Strings.TryGetValue(key, out var value) ? value : key;
    }

    public static LocalizationConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new LocalizationConfig();

        var json = File.ReadAllText(path);
        var jObj = JObject.Parse(json);
        var strings = jObj["Strings"] as JObject;

        var config = new LocalizationConfig();
        if (strings == null) return config;

        foreach (var prop in strings.Properties())
        {
            config.Strings[prop.Name] = prop.Value.Value<string>() ?? prop.Name;
        }

        return config;
    }
}
