using System.Runtime.InteropServices;
using MHWStatOverlay.Helpers;

namespace MHWStatOverlay.Core;

public class MemoryReader : IDisposable
{
    private IntPtr _processHandle;
    private bool _disposed;

    public bool IsAttached => _processHandle != IntPtr.Zero;

    public bool Attach(int processId)
    {
        Detach();
        _processHandle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_QUERY_INFORMATION,
            false, processId);
        return _processHandle != IntPtr.Zero;
    }

    public void Detach()
    {
        if (_processHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_processHandle);
            _processHandle = IntPtr.Zero;
        }
    }

    public byte[]? ReadBytes(IntPtr address, int size)
    {
        if (_processHandle == IntPtr.Zero) return null;
        var buffer = new byte[size];
        bool success = NativeMethods.ReadProcessMemory(_processHandle, address, buffer, size, out int bytesRead);
        return success && bytesRead == size ? buffer : null;
    }

    public T? Read<T>(IntPtr address) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        var bytes = ReadBytes(address, size);
        if (bytes == null) return null;

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    public IntPtr ReadPointer(IntPtr address)
    {
        var value = Read<long>(address);
        return value.HasValue ? (IntPtr)value.Value : IntPtr.Zero;
    }

    public float ReadFloat(IntPtr address) => Read<float>(address) ?? 0f;
    public int ReadInt32(IntPtr address) => Read<int>(address) ?? 0;
    public long ReadInt64(IntPtr address) => Read<long>(address) ?? 0;

    public void Dispose()
    {
        if (!_disposed)
        {
            Detach();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
