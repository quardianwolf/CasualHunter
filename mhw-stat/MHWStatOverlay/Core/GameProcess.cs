using System.Diagnostics;

namespace MHWStatOverlay.Core;

public class GameProcess
{
    private const string ProcessName = "MonsterHunterWorld";
    private Process? _process;

    public bool IsRunning => _process != null && !_process.HasExited;
    public IntPtr BaseAddress => _process?.MainModule?.BaseAddress ?? IntPtr.Zero;
    public int ProcessId => _process?.Id ?? 0;

    public bool Find()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        if (processes.Length == 0)
            return false;

        _process = processes[0];
        return true;
    }

    public void Reset()
    {
        _process?.Dispose();
        _process = null;
    }

    public long GetModuleSize()
    {
        if (_process?.MainModule == null) return 0;
        return _process.MainModule.ModuleMemorySize;
    }
}
