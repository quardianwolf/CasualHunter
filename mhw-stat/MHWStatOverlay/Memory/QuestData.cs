using MHWStatOverlay.Core;
using MHWStatOverlay.Models;

namespace MHWStatOverlay.Memory;

public class QuestData
{
    private readonly MemoryReader _reader;
    private readonly PointerScanner _scanner;

    public QuestData(MemoryReader reader, PointerScanner scanner)
    {
        _reader = reader;
        _scanner = scanner;
    }

    public Quest ReadQuest(IntPtr gameBase, VersionAddresses addresses)
    {
        var questBase = addresses.QuestBase;

        var baseAddr = _scanner.FollowPointerChain(
            gameBase + (int)questBase.BaseOffset,
            questBase.Offsets);

        if (baseAddr == IntPtr.Zero)
            return new Quest { IsActive = false };

        int questId = _reader.ReadInt32(baseAddr + addresses.QuestIdOffset);
        float timer = _reader.ReadFloat(baseAddr + addresses.QuestTimerOffset);
        int questType = _reader.ReadInt32(baseAddr + addresses.QuestTypeOffset);

        return new Quest
        {
            Id = questId,
            TimerSeconds = timer,
            Type = MapQuestType(questType),
            IsActive = questId > 0,
            Name = $"Quest #{questId}"
        };
    }

    private static QuestType MapQuestType(int type)
    {
        return type switch
        {
            0 => QuestType.None,
            1 => QuestType.Hunt,
            2 => QuestType.Capture,
            3 => QuestType.Slay,
            4 => QuestType.Delivery,
            5 => QuestType.Event,
            6 => QuestType.Special,
            7 => QuestType.Arena,
            _ => QuestType.None
        };
    }
}
