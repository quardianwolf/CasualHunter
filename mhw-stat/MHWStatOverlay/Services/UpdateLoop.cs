using System.IO;
using MHWStatOverlay.Core;
using MHWStatOverlay.Memory;
using MHWStatOverlay.Models;

namespace MHWStatOverlay.Services;

public class UpdateLoop : IDisposable
{
    private readonly GameProcess _gameProcess;
    private readonly MemoryReader _reader;
    private readonly PatternScanner _patternScanner;
    private readonly PointerScanner _ptrScanner;
    private readonly MonsterData _monsterData;
    private readonly PlayerData _playerData;
    private readonly SettingsService _settings;
    private readonly MemoryConfig _memoryConfig;
    private readonly string _logPath;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    // Resolved RIP addresses (stable, found once via pattern scan)
    private IntPtr _monsterRipAddr;
    private IntPtr _damageRipAddr;
    private IntPtr _nameRipAddr;
    private bool _patternsResolved;

    public event Action<List<Monster>>? MonstersUpdated;
    public event Action<List<Player>>? PlayersUpdated;
    public event Action<Quest>? QuestUpdated;
    public event Action<bool>? ConnectionChanged;
    public event Action<string>? StatusMessage;

    public bool IsConnected { get; private set; }

    public UpdateLoop(SettingsService settings)
    {
        _settings = settings;
        _gameProcess = new GameProcess();
        _reader = new MemoryReader();
        _patternScanner = new PatternScanner(_reader, _gameProcess);
        _ptrScanner = new PointerScanner(_reader);

        _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

        // Load SmartHunter config files
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Log($"Data dir: {dataDir}");
        Log($"Memory.json exists: {File.Exists(Path.Combine(dataDir, "Memory.json"))}");

        _memoryConfig = MemoryConfig.LoadFromFile(Path.Combine(dataDir, "Memory.json"));
        var monsterConfig = MonsterConfig.LoadFromFile(Path.Combine(dataDir, "MonsterData.json"));
        var localization = LocalizationConfig.LoadFromFile(Path.Combine(dataDir, "en-US.json"));

        Log($"ProcessName from config: {_memoryConfig.ProcessName}");
        Log($"MonsterPattern: {_memoryConfig.MonsterPattern.PatternString}");
        Log($"Loaded {monsterConfig.Monsters.Count} monster definitions");
        Log($"Loaded {localization.Strings.Count} localization strings");

        _monsterData = new MonsterData(_reader, _ptrScanner, monsterConfig, localization, Log);
        _playerData = new PlayerData(_reader, Log);
    }

    private void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }

    public void Start()
    {
        Stop();
        Log("=== UpdateLoop Started ===");
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _loopTask?.Wait(TimeSpan.FromSeconds(2));
        _cts?.Dispose();
        _cts = null;
        _loopTask = null;
    }

    private async Task RunLoop(CancellationToken ct)
    {
        int loopCount = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                loopCount++;

                if (!_gameProcess.IsRunning)
                {
                    if (IsConnected)
                    {
                        Log("Game process lost, disconnecting");
                        IsConnected = false;
                        _patternsResolved = false;
                        ConnectionChanged?.Invoke(false);
                        StatusMessage?.Invoke("MHW baglantisi kesildi");
                        _reader.Detach();
                    }

                    if (loopCount % 10 == 1) // Log every 10th attempt
                        Log($"Searching for process: {_memoryConfig.ProcessName}");

                    if (_gameProcess.Find())
                    {
                        Log($"Found process! PID={_gameProcess.ProcessId}, BaseAddr=0x{_gameProcess.BaseAddress:X}, ModuleSize={_gameProcess.GetModuleSize()}");
                        StatusMessage?.Invoke($"MHW bulundu (PID: {_gameProcess.ProcessId})");

                        if (_reader.Attach(_gameProcess.ProcessId))
                        {
                            Log("Successfully attached to process");
                            IsConnected = true;
                            ConnectionChanged?.Invoke(true);
                            StatusMessage?.Invoke("MHW'ye baglanildi, pattern tarama yapiliyor...");
                        }
                        else
                        {
                            Log("FAILED to attach! (Admin yetkisi gerekebilir)");
                            StatusMessage?.Invoke("HATA: Process'e baglanamadi - Admin olarak calistirin!");
                            _gameProcess.Reset();
                        }
                    }
                }

                if (IsConnected && !_patternsResolved)
                {
                    Log("Starting pattern scan...");
                    StatusMessage?.Invoke("Pattern tarama yapiliyor...");
                    ResolvePatterns();

                    if (_patternsResolved)
                    {
                        Log($"Patterns resolved! MonsterRIP=0x{_monsterRipAddr:X}, DamageRIP=0x{_damageRipAddr:X}, NameRIP=0x{_nameRipAddr:X}");
                        StatusMessage?.Invoke("Baglandi - veri okunuyor");
                    }
                    else
                    {
                        Log($"Pattern scan FAILED.");
                        StatusMessage?.Invoke("Pattern bulunamadi - offset'ler guncel olmayabilir");
                    }
                }

                if (IsConnected && _patternsResolved)
                {
                    // Re-read pointer chains every tick (game reallocates objects)
                    var monsterBaseList = ReadMultiLevelPointer(_monsterRipAddr, 1688, 0, 312, 0);
                    var monsters = _monsterData.ReadMonsters(monsterBaseList);
                    MonstersUpdated?.Invoke(monsters);

                    var damageCollection = ReadMultiLevelPointer(_damageRipAddr, 2888);
                    var players = _playerData.ReadPlayers(damageCollection);
                    PlayersUpdated?.Invoke(players);

                    // Also refresh name collection address
                    if (_nameRipAddr != IntPtr.Zero)
                    {
                        int nameBase = _reader.ReadInt32(_nameRipAddr);
                        _playerData.SetNameCollectionAddress((IntPtr)nameBase);
                    }

                    QuestUpdated?.Invoke(new Quest { IsActive = monsters.Count > 0 });

                    if (loopCount % 100 == 0)
                        Log($"Reading OK: {monsters.Count} monsters, {players.Count} players");
                }

                await Task.Delay(_settings.Settings.PollingRateMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.GetType().Name}: {ex.Message}");
                await Task.Delay(500, ct);
            }
        }
    }

    private void ResolvePatterns()
    {
        // Pattern scan finds the AOB instruction addresses (stable for game session)
        // RIP resolution gives global pointer addresses (also stable)
        // But the VALUES at those pointers change as the game loads/unloads entities
        // So we store only the RIP addresses and re-read chains every tick

        var monsterPatternAddr = _patternScanner.FindPattern(_memoryConfig.MonsterPattern);
        Log($"MonsterPattern scan result: 0x{monsterPatternAddr:X}");
        if (monsterPatternAddr != IntPtr.Zero)
        {
            _monsterRipAddr = _patternScanner.ResolveRipRelative(monsterPatternAddr);
            Log($"MonsterBase RIP resolved: 0x{_monsterRipAddr:X}");
        }

        var damagePatternAddr = _patternScanner.FindPattern(_memoryConfig.PlayerDamagePattern);
        Log($"DamagePattern scan result: 0x{damagePatternAddr:X}");
        if (damagePatternAddr != IntPtr.Zero)
        {
            _damageRipAddr = _patternScanner.ResolveRipRelative(damagePatternAddr);
            Log($"DamageBase RIP resolved: 0x{_damageRipAddr:X}");
        }

        var namePatternAddr = _patternScanner.FindPattern(_memoryConfig.PlayerNamePattern);
        Log($"PlayerNamePattern scan result: 0x{namePatternAddr:X}");
        if (namePatternAddr != IntPtr.Zero)
        {
            _nameRipAddr = _patternScanner.ResolveRipRelative(namePatternAddr);
            Log($"PlayerNameBase RIP resolved: 0x{_nameRipAddr:X}");
        }

        _patternsResolved = _monsterRipAddr != IntPtr.Zero;
    }

    private IntPtr ReadMultiLevelPointer(IntPtr address, params long[] offsets)
    {
        var current = address;
        foreach (long offset in offsets)
        {
            var derefed = _reader.ReadPointer(current);
            if (derefed == IntPtr.Zero) return IntPtr.Zero;
            current = (IntPtr)(derefed.ToInt64() + offset);
        }
        return current;
    }

    public void Dispose()
    {
        Stop();
        _reader.Dispose();
        GC.SuppressFinalize(this);
    }
}
