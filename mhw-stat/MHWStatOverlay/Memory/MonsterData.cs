using System.Text;
using MHWStatOverlay.Core;
using MHWStatOverlay.Models;

namespace MHWStatOverlay.Memory;

public class MonsterData
{
    private readonly MemoryReader _reader;
    private readonly PointerScanner _ptrScanner;

    // SmartHunter exact offsets (decompiled from SmartHunter.exe):
    //
    // Monster traversal from monsterBaseList:
    //   monsterAddresses[0] = monsterBaseList  (the base IS the first monster!)
    //   monsterAddresses[1] = Read(monsterBaseList - 48) + 64
    //   monsterAddresses[2] = Read(Read(monsterBaseList - 48) + 16) + 64
    //
    // Monster ID: Read(monsterAddr + 0x2A0) → ptr, then ReadString(ptr + 0x0C, 32)
    // Also: Read(monsterAddr + 0x8A00) → ptr, then ReadString(ptr + 0x0C, 32) for alt ID
    //
    // HP component: monsterAddr + 0x7670 → deref → hpComponent
    //   MaxHealth: hpComponent + 0x60
    //   CurrentHealth: hpComponent + 0x64
    //
    // Parts: monsterAddr + 0x14528  (PartCollection)
    //   FirstPart: collection + 0x1C
    //   Part HP: current=+0x04, max=+0x00, broken=+0x0C
    //   NextPart: +0x1F8 (504 bytes stride)
    //
    // RemovableParts: monsterAddr + 0x14528 + 8864 - 240 - 240 - 240 - 56 = +0x166F0
    //   FirstRemovable: +0x78
    //   HP: current=+0x10, max=+0x0C, broken=+0x18
    //   NextRemovable: +0x78

    private const int MonsterStartOfStructOffset = 0x40;  // 64

    private const int MonsterHpComponentOffset = 0x7670;   // 30320
    private const int HpMaxOffset = 0x60;                  // 96
    private const int HpCurrentOffset = 0x64;              // 100

    private const int MonsterNamePtrOffset = 0x2A0;        // 672 → ptr to model
    private const int MonsterNameIdOffset = 0x0C;          // 12, from model ptr
    private const int MonsterNameIdLength = 32;

    private const int MonsterAltNamePtrOffset = 0x8A00;    // 35328 → alt ptr
    private const int SizeScaleOffset = 0x188;             // 392
    private const int ScaleModifierOffset = 0x7730;        // 30512

    // Rage/Enrage — HunterPie sMonsterStatus struct at 0x1BE30
    private const int EnrageStatusOffset = 0x1BE30;
    // sMonsterStatus layout: +0x14 IsActive, +0x24 Duration, +0x28 MaxDuration
    private const int EnrageIsActiveOffset = 0x14;
    private const int EnrageDurationOffset = 0x24;
    private const int EnrageMaxDurationOffset = 0x28;

    private const int MonsterStaminaOffset = 0x1C0F0;      // 114928 - stamina timer

    // Alatreon specific
    private const int MonsterGameIdOffset = 0x12280;
    private const int AlatreonElementStateOffset = 0x20910;  // float, not int
    private const int AlatreonToppleCounterOffset = 0x20920;
    private const int AlatreonElementThresholdOffset = 0x20878;  // counts DOWN, 0 = topple

    // Status effects (paralysis, stun, poison, etc.)
    private const int StatusEffectCollectionOffset = 0x19900;  // 104704
    private const int StatusEffectNextPtr = 0x08;
    private const int StatusEffectIdOffset = 0x158;            // 344
    private const int StatusEffectMaxDuration = 0x19C;         // 412
    private const int StatusEffectCurrentBuildup = 0x1B8;      // 440
    private const int StatusEffectMaxBuildup = 0x1C8;          // 456
    private const int StatusEffectCurrentDuration = 0x1F8;     // 504
    private const int StatusEffectTimesActivated = 0x200;      // 512
    private const int MaxStatusEffects = 20;

    private const int PartCollectionOffset = 0x14528;      // 83240
    private const int PartCollectionFirstPart = 0x1C;      // 28
    private const int PartMaxHpOffset = 0x00;
    private const int PartCurrentHpOffset = 0x04;
    private const int PartTimesBrokenOffset = 0x0C;        // 12
    private const int PartNextOffset = 0x1F8;              // 504
    private const int MaxPartCount = 16;

    // RemovablePartCollection = PartCollection + 8864 - 240 - 240 - 240 - 56
    //                         = 0x14528 + 0x22A0 - 0xF0 - 0xF0 - 0xF0 - 0x38
    //                         = 0x14528 + 0x1EC8 = 0x163F0
    private const int RemovablePartCollectionOffset = 0x163F0;
    private const int RemovableFirstPart = 0x78;           // 120
    private const int RemovableMaxHpOffset = 0x0C;
    private const int RemovableCurrentHpOffset = 0x10;
    private const int RemovableTimesBrokenOffset = 0x18;   // 24
    private const int RemovableNextOffset = 0x78;          // 120
    private const int MaxRemovablePartCount = 32;

    private const int MaxMonsters = 3;

    private readonly MonsterConfig _monsterConfig;
    private readonly LocalizationConfig _localization;
    private readonly Action<string>? _log;
    private bool _dumpDone;
    private bool _rageDumpDone;
    private int _tickCount;

    public MonsterData(MemoryReader reader, PointerScanner ptrScanner,
                       MonsterConfig monsterConfig, LocalizationConfig localization,
                       Action<string>? log = null)
    {
        _reader = reader;
        _ptrScanner = ptrScanner;
        _monsterConfig = monsterConfig;
        _localization = localization;
        _log = log;
    }

    public List<Monster> ReadMonsters(IntPtr monsterBaseList)
    {
        var monsters = new List<Monster>();
        if (monsterBaseList == IntPtr.Zero || monsterBaseList.ToInt64() < 0xFFFFFF)
            return monsters;

        // SmartHunter exact traversal:
        // monster[0] = monsterBaseList itself
        // monster[1] = Read(monsterBaseList - 48) + 64
        // monster[2] = Read(Read(monsterBaseList - 48) + 16) + 64

        var monsterAddresses = new IntPtr[3];
        monsterAddresses[0] = monsterBaseList;

        var prevPtr = _reader.ReadPointer(monsterBaseList - 48);
        if (prevPtr != IntPtr.Zero)
        {
            monsterAddresses[1] = prevPtr + MonsterStartOfStructOffset;

            var prevPrevPtr = _reader.ReadPointer(prevPtr + 16);
            if (prevPrevPtr != IntPtr.Zero)
                monsterAddresses[2] = prevPrevPtr + MonsterStartOfStructOffset;
        }

        if (!_dumpDone)
        {
            _dumpDone = true;
            _log?.Invoke($"=== MONSTER DIAGNOSTIC ===");
            _log?.Invoke($"MonsterBaseList: 0x{monsterBaseList:X}");
            _log?.Invoke($"Monster[0]: 0x{monsterAddresses[0]:X}");
            _log?.Invoke($"Monster[1]: 0x{monsterAddresses[1]:X}");
            _log?.Invoke($"Monster[2]: 0x{monsterAddresses[2]:X}");

            for (int i = 0; i < 3; i++)
            {
                if (monsterAddresses[i] == IntPtr.Zero) continue;
                string id = ReadMonsterId(monsterAddresses[i]);
                var hpComp = _reader.ReadPointer(monsterAddresses[i] + MonsterHpComponentOffset);
                float maxHp = 0, curHp = 0;
                if (hpComp != IntPtr.Zero)
                {
                    maxHp = _reader.ReadFloat(hpComp + HpMaxOffset);
                    curHp = _reader.ReadFloat(hpComp + HpCurrentOffset);
                }
                _log?.Invoke($"  Monster[{i}] ID=\"{id}\", hpComp=0x{hpComp:X}, HP={curHp}/{maxHp}");
            }
            _log?.Invoke($"=== END MONSTER DIAGNOSTIC ===");
        }

        for (int i = 0; i < MaxMonsters; i++)
        {
            if (monsterAddresses[i] == IntPtr.Zero) continue;

            var monster = ReadSingleMonster(monsterAddresses[i], i);
            if (monster != null && monster.MaxHP > 0)
                monsters.Add(monster);
        }

        return monsters;
    }

    private Monster? ReadSingleMonster(IntPtr monsterAddr, int index)
    {
        var hpComponent = _reader.ReadPointer(monsterAddr + MonsterHpComponentOffset);
        if (hpComponent == IntPtr.Zero) return null;

        float maxHp = _reader.ReadFloat(hpComponent + HpMaxOffset);
        if (maxHp <= 0) return null;

        float currentHp = _reader.ReadFloat(hpComponent + HpCurrentOffset);

        string monsterId = ReadMonsterId(monsterAddr);

        // Filter: only include actual monsters, not NPCs
        // Use GameId as backup — some monsters have wrong string IDs
        int gameId = _reader.ReadInt32(monsterAddr + MonsterGameIdOffset);
        if (string.IsNullOrEmpty(monsterId) || !monsterId.StartsWith("em"))
        {
            if (gameId <= 0) return null; // not a valid entity
            // Use gameId to pass filter even if string ID is wrong
        }

        string monsterName = ResolveMonsterName(monsterId);

        // Read rage from HunterPie sMonsterStatus struct
        var enrageBase = monsterAddr + EnrageStatusOffset;
        int enrageActive = _reader.ReadInt32(enrageBase + EnrageIsActiveOffset);
        float rageDuration = _reader.ReadFloat(enrageBase + EnrageDurationOffset);
        float rageMaxDuration = _reader.ReadFloat(enrageBase + EnrageMaxDurationOffset);

        float staminaTimer = _reader.ReadFloat(monsterAddr + MonsterStaminaOffset);

        _tickCount++;
        // Periodic Alatreon element scan (every 30 ticks ≈ 3 seconds)
        if (gameId == 87 && _tickCount % 30 == 0)
        {
            float v878 = _reader.ReadFloat(monsterAddr + 0x20878);
            float v8B8 = _reader.ReadFloat(monsterAddr + 0x208B8);
            float v910 = _reader.ReadFloat(monsterAddr + 0x20910);
            float v938 = _reader.ReadFloat(monsterAddr + 0x20938);
            int topple = _reader.ReadInt32(monsterAddr + 0x20920);
            _log?.Invoke($"ALA: 878={v878:F0} 8B8={v8B8:F0} 910={v910:F1} 938={v938:F1} topple={topple}");
        }

        // Alatreon-specific data (GameId 87)
        int alatreonElementState = 0;
        int alatreonToppleCount = 0;
        bool isAlatreon = gameId == 87 || monsterId == "em120_00";
        float alatreonElementRemaining = 0;
        if (isAlatreon)
        {
            alatreonElementState = _reader.ReadInt32(monsterAddr + AlatreonElementStateOffset);
            alatreonToppleCount = _reader.ReadInt32(monsterAddr + AlatreonToppleCounterOffset);
            alatreonElementRemaining = _reader.ReadFloat(monsterAddr + AlatreonElementThresholdOffset);
        }

        // One-time diagnostic dump for rage and alatreon
        var monster = new Monster
        {
            Id = index,
            MonsterId = monsterId,
            CurrentHP = currentHp,
            MaxHP = maxHp,
            Name = monsterName,
            RageTimer = (enrageActive != 0 && rageDuration < rageMaxDuration && rageMaxDuration > 0)
                ? Math.Max(0, rageMaxDuration - rageDuration) : 0,
            RageDuration = rageMaxDuration,
            StaminaTimer = Math.Max(0, staminaTimer),
            GameId = gameId,
            AlatreonElementState = alatreonElementState,
            AlatreonToppleCount = alatreonToppleCount,
            AlatreonElementRemaining = Math.Max(0, alatreonElementRemaining),
        };

        ReadParts(monsterAddr, monsterId, monster);
        ReadRemovableParts(monsterAddr, monsterId, monster);
        ReadStatusEffects(monsterAddr, monster);

        // One-time diagnostic for Alatreon
        if (!_rageDumpDone)
        {
            _rageDumpDone = true;
            _log?.Invoke($"=== RAGE/ALATREON DIAGNOSTIC ===");
            _log?.Invoke($"Monster: {monsterId} at 0x{monsterAddr:X}");
            _log?.Invoke($"Rage: Active={enrageActive}, Duration={rageDuration:F1}, MaxDuration={rageMaxDuration:F1}");

            _log?.Invoke($"GameId: {gameId} (87=Alatreon)");

            if (isAlatreon)
            {
                _log?.Invoke($"Alatreon: ToppleCount={alatreonToppleCount}");

                // ToppleCount (0x20920) works! Scan nearby for elemental buildup floats
                _log?.Invoke($"Scan 0x20900-0x20A00:");
                var chunk = _reader.ReadBytes(monsterAddr + 0x20900, 0x100);
                if (chunk != null)
                    for (int r = 0; r < chunk.Length; r += 4)
                    {
                        float fval = BitConverter.ToSingle(chunk, r);
                        int ival = BitConverter.ToInt32(chunk, r);
                        if ((fval > 1 && fval < 50000) || (ival > 0 && ival < 200))
                            _log?.Invoke($"  +0x{0x20900 + r:X}: f={fval:F2} i={ival}");
                    }

                // Also scan wider: 0x208xx, 0x20Axx
                foreach (int sb in new[] { 0x20800, 0x20A00, 0x20B00 })
                {
                    var c2 = _reader.ReadBytes(monsterAddr + sb, 0x100);
                    if (c2 == null) continue;
                    bool header = false;
                    for (int r = 0; r < c2.Length; r += 4)
                    {
                        float fv = BitConverter.ToSingle(c2, r);
                        if (fv > 10 && fv < 50000)
                        {
                            if (!header) { _log?.Invoke($"Scan 0x{sb:X}:"); header = true; }
                            _log?.Invoke($"  +0x{sb + r:X}: f={fv:F2}");
                        }
                    }
                }
            }
            _log?.Invoke($"=== END DIAGNOSTIC ===");
        }

        return monster;
    }

    private void ReadParts(IntPtr monsterAddr, string monsterId, Monster monster)
    {
        var partCollection = monsterAddr + PartCollectionOffset;
        var partAddr = partCollection + PartCollectionFirstPart;

        var monsterDef = _monsterConfig.Monsters.GetValueOrDefault(monsterId);
        int partIndex = 0;

        for (int i = 0; i < MaxPartCount; i++)
        {
            float partMaxHp = _reader.ReadFloat(partAddr + PartMaxHpOffset);
            if (partMaxHp <= 0)
            {
                partAddr += PartNextOffset;
                continue;
            }

            float partCurrentHp = _reader.ReadFloat(partAddr + PartCurrentHpOffset);
            int breakCount = _reader.ReadInt32(partAddr + PartTimesBrokenOffset);

            string partName = $"Part {partIndex + 1}";
            if (monsterDef != null)
            {
                var nonRemovableParts = monsterDef.Parts.Where(p => !p.IsRemovable).ToList();
                if (partIndex < nonRemovableParts.Count)
                    partName = _localization.Get(nonRemovableParts[partIndex].StringId);
            }

            monster.Parts.Add(new MonsterPart
            {
                Index = partIndex,
                Name = partName,
                CurrentHP = partCurrentHp,
                MaxHP = partMaxHp,
                BreakCount = breakCount
            });

            partIndex++;
            partAddr += PartNextOffset;
        }
    }

    private void ReadRemovableParts(IntPtr monsterAddr, string monsterId, Monster monster)
    {
        var removableCollection = monsterAddr + RemovablePartCollectionOffset;
        var partAddr = removableCollection + RemovableFirstPart;

        var monsterDef = _monsterConfig.Monsters.GetValueOrDefault(monsterId);
        int removableIndex = 0;

        for (int i = 0; i < MaxRemovablePartCount; i++)
        {
            float partMaxHp = _reader.ReadFloat(partAddr + RemovableMaxHpOffset);
            if (partMaxHp <= 0)
            {
                partAddr += RemovableNextOffset;
                continue;
            }

            float partCurrentHp = _reader.ReadFloat(partAddr + RemovableCurrentHpOffset);
            int breakCount = _reader.ReadInt32(partAddr + RemovableTimesBrokenOffset);

            string partName = $"Removable {removableIndex + 1}";
            if (monsterDef != null)
            {
                var removableParts = monsterDef.Parts.Where(p => p.IsRemovable).ToList();
                if (removableIndex < removableParts.Count)
                    partName = _localization.Get(removableParts[removableIndex].StringId) + " *";
            }

            monster.Parts.Add(new MonsterPart
            {
                Index = monster.Parts.Count,
                Name = partName,
                CurrentHP = partCurrentHp,
                MaxHP = partMaxHp,
                BreakCount = breakCount
            });

            removableIndex++;
            partAddr += RemovableNextOffset;
        }
    }

    private void ReadStatusEffects(IntPtr monsterAddr, Monster monster)
    {
        // Use HunterPie's ailment system: pointer array at monster + 0x1BC40
        // Each pointer → ailment node, actual data at node + 0x148 (sMonsterStatus)
        // sMonsterStatus layout:
        //   +0x18: Buildup (float)
        //   +0x24: Duration (float)
        //   +0x28: MaxDuration (float)
        //   +0x34: Counter (int)
        //   +0x3C: MaxBuildup (float)
        const int AilmentArrayBase = 0x1BC40;
        const int AilmentStructOffset = 0x148;

        for (int i = 0; i < MaxStatusEffects; i++)
        {
            var ailPtr = _reader.ReadPointer(monsterAddr + AilmentArrayBase + (i * 8));
            if (ailPtr == IntPtr.Zero || ailPtr.ToInt64() < 0x10000 || ailPtr.ToInt64() > 0x7FFFFFFFFFFF)
                break;

            var ab = ailPtr + AilmentStructOffset;
            float buildup = _reader.ReadFloat(ab + 0x18);
            float maxBuildup = _reader.ReadFloat(ab + 0x3C);
            float duration = _reader.ReadFloat(ab + 0x24);
            float maxDuration = _reader.ReadFloat(ab + 0x28);
            int counter = _reader.ReadInt32(ab + 0x34);

            if (maxBuildup <= 0) continue;

            // Filter: only show ailments with meaningful buildup (>5%) or active duration
            float pct = buildup / maxBuildup * 100f;
            if (pct < 5 && duration <= 0 && counter <= 0) continue;

            var (name, icon) = MonsterStatusEffect.KnownEffects.GetValueOrDefault(
                i, ($"Status {i}", "\u25CF"));

            monster.StatusEffects.Add(new MonsterStatusEffect
            {
                Id = i,
                Name = name,
                Icon = icon,
                CurrentBuildup = Math.Max(0, buildup),
                MaxBuildup = maxBuildup,
                CurrentDuration = Math.Max(0, duration),
                MaxDuration = maxDuration,
                TimesActivated = counter
            });
        }
    }

    private string ReadMonsterId(IntPtr monsterAddr)
    {
        // SmartHunter: Read(monsterAddr + 0x2A0) → namePtr, then ReadString(namePtr + 0x0C)
        var namePtr = _reader.ReadPointer(monsterAddr + MonsterNamePtrOffset);
        if (namePtr == IntPtr.Zero) return "";

        var bytes = _reader.ReadBytes(namePtr + MonsterNameIdOffset, MonsterNameIdLength);
        if (bytes == null) return "";

        int nullIdx = Array.IndexOf(bytes, (byte)0);
        if (nullIdx < 0) nullIdx = bytes.Length;
        string fullPath = Encoding.UTF8.GetString(bytes, 0, nullIdx);

        // SmartHunter does: id.Split('\\').Last()
        // The string is like "em\em001_00" or just "em001_00"
        var parts = fullPath.Split('\\');
        return parts[^1]; // Last segment
    }

    private string ResolveMonsterName(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId)) return "Unknown";
        if (_monsterConfig.Monsters.TryGetValue(monsterId, out var def))
            return _localization.Get(def.NameStringId);
        return monsterId;
    }
}
