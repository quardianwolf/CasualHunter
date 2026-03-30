using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MHWStatOverlay.Memory;

public class MonsterPartDef
{
    public string GroupId { get; set; } = "";
    public string StringId { get; set; } = "";
    public bool IsRemovable { get; set; }
}

public class MonsterSoftenPartDef
{
    public string StringId { get; set; } = "";
    public List<int> PartIds { get; set; } = new();
}

public class CrownDef
{
    public float Mini { get; set; }
    public float Silver { get; set; }
    public float Gold { get; set; }
}

public class MonsterDef
{
    public string Id { get; set; } = "";
    public string NameStringId { get; set; } = "";
    public List<MonsterPartDef> Parts { get; set; } = new();
    public List<MonsterSoftenPartDef> SoftenParts { get; set; } = new();
    public float BaseSize { get; set; }
    public float ScaleModifier { get; set; } = 1;
    public CrownDef Crowns { get; set; } = new();
    public bool IsElder { get; set; }
}

public class MonsterConfig
{
    public Dictionary<string, MonsterDef> Monsters { get; set; } = new();

    public static MonsterConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new MonsterConfig();

        var json = File.ReadAllText(path);
        var jObj = JObject.Parse(json);
        var monstersToken = jObj["Monsters"] as JObject;

        var config = new MonsterConfig();
        if (monstersToken == null) return config;

        foreach (var prop in monstersToken.Properties())
        {
            var mDef = prop.Value.ToObject<MonsterDef>() ?? new MonsterDef();
            mDef.Id = prop.Name;
            config.Monsters[prop.Name] = mDef;
        }

        return config;
    }
}
