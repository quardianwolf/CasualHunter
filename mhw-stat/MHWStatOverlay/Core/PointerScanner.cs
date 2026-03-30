namespace MHWStatOverlay.Core;

public class PointerScanner
{
    private readonly MemoryReader _reader;

    public PointerScanner(MemoryReader reader)
    {
        _reader = reader;
    }

    public IntPtr FollowPointerChain(IntPtr baseAddress, int[] offsets)
    {
        var current = baseAddress;

        for (int i = 0; i < offsets.Length; i++)
        {
            if (i < offsets.Length - 1)
            {
                current = _reader.ReadPointer(current + offsets[i]);
                if (current == IntPtr.Zero)
                    return IntPtr.Zero;
            }
            else
            {
                current += offsets[i];
            }
        }

        return current;
    }

    public float ReadFloatChain(IntPtr baseAddress, int[] offsets)
    {
        var addr = FollowPointerChain(baseAddress, offsets);
        return addr == IntPtr.Zero ? 0f : _reader.ReadFloat(addr);
    }

    public int ReadInt32Chain(IntPtr baseAddress, int[] offsets)
    {
        var addr = FollowPointerChain(baseAddress, offsets);
        return addr == IntPtr.Zero ? 0 : _reader.ReadInt32(addr);
    }
}
