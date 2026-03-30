namespace MHWStatOverlay.Core;

public class PatternDef
{
    public string Name { get; set; } = "";
    public string PatternString { get; set; } = "";
    public string? LastResultAddress { get; set; }
}

public class PatternScanner
{
    private readonly MemoryReader _reader;
    private readonly GameProcess _gameProcess;

    public PatternScanner(MemoryReader reader, GameProcess gameProcess)
    {
        _reader = reader;
        _gameProcess = gameProcess;
    }

    public IntPtr FindPattern(PatternDef pattern)
    {
        // First try cached address
        if (!string.IsNullOrEmpty(pattern.LastResultAddress))
        {
            if (long.TryParse(pattern.LastResultAddress, System.Globalization.NumberStyles.HexNumber, null, out long cached))
            {
                var cachedAddr = (IntPtr)cached;
                if (VerifyPattern(cachedAddr, pattern.PatternString))
                    return cachedAddr;
            }
        }

        // Full scan
        var baseAddr = _gameProcess.BaseAddress;
        long moduleSize = _gameProcess.GetModuleSize();
        if (baseAddr == IntPtr.Zero || moduleSize == 0)
            return IntPtr.Zero;

        var patternBytes = ParsePattern(pattern.PatternString);
        if (patternBytes == null || patternBytes.Length == 0)
            return IntPtr.Zero;

        // Read in chunks to avoid huge allocations
        const int chunkSize = 0x100000; // 1MB chunks
        for (long offset = 0; offset < moduleSize; offset += chunkSize - patternBytes.Length)
        {
            int readSize = (int)Math.Min(chunkSize, moduleSize - offset);
            var chunk = _reader.ReadBytes(baseAddr + (int)offset, readSize);
            if (chunk == null) continue;

            int found = ScanChunk(chunk, patternBytes);
            if (found >= 0)
            {
                var resultAddr = baseAddr + (int)offset + found;
                pattern.LastResultAddress = resultAddr.ToString("X");
                return resultAddr;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Resolves a RIP-relative pointer from a pattern match.
    /// Pattern like "48 8B 0D ?? ?? ?? ??" means: at the ?? position there's a 32-bit relative offset.
    /// The actual address = instructionAddress + instructionLength + relativeOffset
    /// </summary>
    public IntPtr ResolveRipRelative(IntPtr patternAddr, int opcodeLength = 3, int instructionLength = 7)
    {
        var relativeOffset = _reader.Read<int>(patternAddr + opcodeLength);
        if (!relativeOffset.HasValue) return IntPtr.Zero;
        return patternAddr + instructionLength + relativeOffset.Value;
    }

    private bool VerifyPattern(IntPtr address, string patternString)
    {
        var patternBytes = ParsePattern(patternString);
        if (patternBytes == null) return false;

        var memory = _reader.ReadBytes(address, patternBytes.Length);
        if (memory == null) return false;

        for (int i = 0; i < patternBytes.Length; i++)
        {
            if (patternBytes[i].HasValue && memory[i] != patternBytes[i].Value)
                return false;
        }
        return true;
    }

    private static int ScanChunk(byte[] chunk, byte?[] pattern)
    {
        int limit = chunk.Length - pattern.Length;
        for (int i = 0; i <= limit; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (pattern[j].HasValue && chunk[i + j] != pattern[j].Value)
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    private static byte?[] ParsePattern(string patternString)
    {
        var parts = patternString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new byte?[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "??" || parts[i] == "?")
                result[i] = null;
            else
                result[i] = Convert.ToByte(parts[i], 16);
        }
        return result;
    }
}
