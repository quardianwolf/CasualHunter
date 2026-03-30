using System.Text;
using MHWStatOverlay.Core;
using MHWStatOverlay.Models;

namespace MHWStatOverlay.Memory;

public class PlayerData
{
    private readonly MemoryReader _reader;
    private readonly Action<string>? _log;

    // SmartHunter exact offsets (decompiled):
    //
    // playerDamageCollectionAddress is already resolved by UpdateLoop
    // (it's after ReadMultiLevelPointer with offset 2888)
    //
    // For each player i:
    //   nameAddr = playerNameCollection + FirstPlayerName + i * PlayerNameLength
    //   damagePtr = playerDamageCollection + FirstPlayerPtr + i * NextPlayerPtr
    //   playerAddr = Read<ulong>(damagePtr)
    //   damage = Read<int>(playerAddr + PlayerDamage.Damage)

    private const int MaxPlayers = 4;

    // Offsets from SmartHunter DataOffsets
    private const int FirstPlayerPtr = 72;    // 0x48
    private const int NextPlayerPtr = 88;     // 0x58
    private const int PlayerDamageOffset = 72; // 0x48 (within player struct)

    private const int FirstPlayerName = 340693;  // 0x532D5
    private const int PlayerNameLength = 33;

    private IntPtr _nameCollectionAddr;

    public PlayerData(MemoryReader reader, Action<string>? log = null)
    {
        _reader = reader;
        _log = log;
    }

    public void SetNameCollectionAddress(IntPtr addr)
    {
        if (addr != _nameCollectionAddr)
        {
            _nameCollectionAddr = addr;
            _log?.Invoke($"PlayerData: NameCollection set to 0x{addr:X}");
        }
    }

    public List<Player> ReadPlayers(IntPtr damageCollectionAddr)
    {
        var players = new List<Player>();
        if (damageCollectionAddr == IntPtr.Zero) return players;

        int totalDamage = 0;
        var rawPlayers = new List<(string name, int damage, int index)>();

        for (int i = 0; i < MaxPlayers; i++)
        {
            // Read name from name collection
            string name = ReadPlayerName(i);

            // Read damage from damage collection
            // damagePtr = collection + FirstPlayerPtr + i * NextPlayerPtr
            var damagePtr = damageCollectionAddr + FirstPlayerPtr + (i * NextPlayerPtr);
            var playerAddr = _reader.ReadPointer(damagePtr);

            int damage = 0;
            if (playerAddr != IntPtr.Zero)
                damage = _reader.ReadInt32(playerAddr + PlayerDamageOffset);

            if (string.IsNullOrEmpty(name) && damage <= 0)
                continue;

            if (damage < 0) damage = 0;

            rawPlayers.Add((name, damage, i));
            totalDamage += damage;
        }

        foreach (var (name, damage, idx) in rawPlayers)
        {
            players.Add(new Player
            {
                Index = idx,
                Name = string.IsNullOrEmpty(name) ? $"Player {idx + 1}" : name,
                Damage = damage,
                DamagePercentage = totalDamage > 0 ? (float)damage / totalDamage * 100f : 0f
            });
        }

        return players;
    }

    private string ReadPlayerName(int playerIndex)
    {
        if (_nameCollectionAddr == IntPtr.Zero) return "";

        var nameAddr = _nameCollectionAddr + FirstPlayerName + (playerIndex * PlayerNameLength);
        var bytes = _reader.ReadBytes(nameAddr, PlayerNameLength);
        if (bytes == null) return "";

        int nullIndex = Array.IndexOf(bytes, (byte)0);
        if (nullIndex <= 0) return "";

        return Encoding.UTF8.GetString(bytes, 0, nullIndex);
    }
}
