using System.IO;
using MHWStatOverlay.Core;
using Newtonsoft.Json;

namespace MHWStatOverlay.Memory;

public class PointerDef
{
    public long BaseOffset { get; set; }
    public int[] Offsets { get; set; } = [];
}

public class VersionAddresses
{
    public PointerDef MonsterBasePtr { get; set; } = new();
    public PointerDef MonsterHpCurrent { get; set; } = new();
    public PointerDef MonsterHpMax { get; set; } = new();
    public PointerDef MonsterPartBase { get; set; } = new();
    public int MonsterPartSize { get; set; }
    public int MonsterPartHpOffset { get; set; }
    public int MonsterPartHpMaxOffset { get; set; }
    public int MonsterPartBreakOffset { get; set; }
    public int MaxMonsterParts { get; set; } = 16;

    public PointerDef PlayerDamageBase { get; set; } = new();
    public int PlayerDamageSize { get; set; }
    public int PlayerNameOffset { get; set; }
    public int PlayerDamageOffset { get; set; }
    public int MaxPlayers { get; set; } = 4;

    public PointerDef QuestBase { get; set; } = new();
    public int QuestIdOffset { get; set; }
    public int QuestTimerOffset { get; set; }
    public int QuestTypeOffset { get; set; }

    public int MaxMonsters { get; set; } = 3;
    public int MonsterNextOffset { get; set; }
}

public class AddressMap
{
    public VersionAddresses? BaseGame { get; set; }
    public VersionAddresses? Iceborne { get; set; }

    public VersionAddresses? GetAddresses(MHWVersion version)
    {
        return version switch
        {
            MHWVersion.BaseGame => BaseGame,
            MHWVersion.Iceborne => Iceborne,
            _ => null
        };
    }

    public static AddressMap LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return CreateDefault();

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<AddressMap>(json) ?? CreateDefault();
    }

    public void SaveToFile(string path)
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public static AddressMap CreateDefault()
    {
        // These are placeholder offsets based on known community research.
        // Real offsets need to be verified with Cheat Engine for each game version.
        return new AddressMap
        {
            Iceborne = new VersionAddresses
            {
                MonsterBasePtr = new PointerDef { BaseOffset = 0x50098E0, Offsets = [0x698, 0x0] },
                MonsterHpCurrent = new PointerDef { BaseOffset = 0, Offsets = [0x7670, 0x60, 0x04] },
                MonsterHpMax = new PointerDef { BaseOffset = 0, Offsets = [0x7670, 0x60, 0x08] },
                MonsterPartBase = new PointerDef { BaseOffset = 0, Offsets = [0x7670, 0x70] },
                MonsterPartSize = 0x1F8,
                MonsterPartHpOffset = 0x0C,
                MonsterPartHpMaxOffset = 0x10,
                MonsterPartBreakOffset = 0x18,
                MaxMonsterParts = 16,
                MonsterNextOffset = 0x698,
                MaxMonsters = 3,

                PlayerDamageBase = new PointerDef { BaseOffset = 0x5009C70, Offsets = [0x258, 0x38] },
                PlayerDamageSize = 0x2A0,
                PlayerNameOffset = 0x49,
                PlayerDamageOffset = 0x48,
                MaxPlayers = 4,

                QuestBase = new PointerDef { BaseOffset = 0x5009C50, Offsets = [0x30] },
                QuestIdOffset = 0x08,
                QuestTimerOffset = 0x14,
                QuestTypeOffset = 0x10,
            },
            BaseGame = new VersionAddresses
            {
                MonsterBasePtr = new PointerDef { BaseOffset = 0x40730E0, Offsets = [0x698, 0x0] },
                MonsterHpCurrent = new PointerDef { BaseOffset = 0, Offsets = [0x7670, 0x60, 0x04] },
                MonsterHpMax = new PointerDef { BaseOffset = 0, Offsets = [0x7670, 0x60, 0x08] },
                MonsterPartBase = new PointerDef { BaseOffset = 0, Offsets = [0x7670, 0x70] },
                MonsterPartSize = 0x1F8,
                MonsterPartHpOffset = 0x0C,
                MonsterPartHpMaxOffset = 0x10,
                MonsterPartBreakOffset = 0x18,
                MaxMonsterParts = 16,
                MonsterNextOffset = 0x698,
                MaxMonsters = 3,

                PlayerDamageBase = new PointerDef { BaseOffset = 0x4073F70, Offsets = [0x258, 0x38] },
                PlayerDamageSize = 0x2A0,
                PlayerNameOffset = 0x49,
                PlayerDamageOffset = 0x48,
                MaxPlayers = 4,

                QuestBase = new PointerDef { BaseOffset = 0x4073F50, Offsets = [0x30] },
                QuestIdOffset = 0x08,
                QuestTimerOffset = 0x14,
                QuestTypeOffset = 0x10,
            }
        };
    }
}
