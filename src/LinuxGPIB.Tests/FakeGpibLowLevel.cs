using System.Diagnostics.CodeAnalysis;
using System.Text;
using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net.Tests.Fakes;

[ExcludeFromCodeCoverage]
internal sealed class FakeGpibLowLevel : IGpibLowLevel
{
    // Simulate descriptor
    public int NextUd { get; set; } = 1;

    // Controls whether IbDev should fail
    public bool FailIbDev { get; set; }

    // Capture last written bytes
    public byte[]? LastWrite { get; private set; }

    // Simulated read buffer & status
    private byte[] _readBuffer = [];
    private int _readOffset;
    private int _lastCount;
    private int _ibsta;
    private int _iberr;
    private bool _assertEndOnLastRead;

    // For SerialPoll / STB (Status Byte)
    public int SerialPollStatusByte { get; set; }

    // Configure a single response for Read()
    public void SetReadResponse(string data, bool assertEnd = true)
    {
        _readBuffer = Encoding.ASCII.GetBytes(data);
        _readOffset = 0;
        _lastCount = 0;
        _ibsta = 0;
        _iberr = 0;
        _assertEndOnLastRead = assertEnd;
    }
    

    public int IbDev(int boardIndex, int pad, int sad, int tmo, int eot, int eos)
    {
        if (FailIbDev)
        {
            // Lib would typically set ibsta/iberr here; we just simulate failure.
            _iberr = 1;
            _ibsta = GpibLowLevelExtensions.ERR;
            return -1;
        }

        _iberr = 0;
        _ibsta = 0;
        return NextUd;
    }

    public int IbOnl(int ud, int v)
    {
        // Just pretend success
        _iberr = 0;
        _ibsta = 0;
        return 0;
    }

    public int IbWrt(int ud, ReadOnlySpan<byte> data)
    {
        LastWrite = data.ToArray();
        _iberr = 0;
        _ibsta = 0; // no ERR/TIMO
        _lastCount = data.Length;
        return 0;
    }

    public int IbRd(int ud, Span<byte> buffer)
    {
        if (_readOffset >= _readBuffer.Length)
        {
            _lastCount = 0;
            _iberr = 0;

            // When we've exhausted the buffer, optionally assert END
            if (_assertEndOnLastRead)
            {
                _ibsta |= GpibDevice.END; // END bit
            }

            return 0;
        }

        int remaining = _readBuffer.Length - _readOffset;
        int toCopy = Math.Min(remaining, buffer.Length);

        _readBuffer.AsSpan(_readOffset, toCopy).CopyTo(buffer);
        _readOffset += toCopy;
        _lastCount = toCopy;
        _iberr = 0;

        // If after this read we've consumed everything, assert END if requested
        if (_readOffset >= _readBuffer.Length && _assertEndOnLastRead)
        {
            _ibsta |= GpibDevice.END; // END bit
        }
        else
        {
            _ibsta &= ~GpibDevice.END; // clear END
        }

        return 0;
    }

    public int IbClr(int ud)
    {
        _iberr = 0;
        _ibsta = 0;
        return 0;
    }

    public int IbRsp(int ud)
    {
        // no error
        _iberr = 0;
        _ibsta = 0;
        return SerialPollStatusByte;
    }

    public int ThreadIbsta() => _ibsta;
    public int ThreadIberr() => _iberr;
    public int ThreadIbcnt() => _lastCount;

    public string GpibErrorString(int error) => $"Fake error {error}";

    public void FindLstn(int boardIndex, ushort[] padList, ushort[] resultList, int maxNumResults)
    {
        throw new NotImplementedException();
    }
}

[ExcludeFromCodeCoverage]
/// <summary>
/// Low-level impl that throws if IbWrt / IbRd are ever called,
/// used to assert the early-return on length == 0.
/// </summary>
internal sealed class GuardedLowLevel : IGpibLowLevel
{
    public bool IbWrtCalled { get; private set; }
    public bool IbRdCalled { get; private set; }

    public int IbDev(int boardIndex, int pad, int sad, int tmo, int eot, int eos) => 1;

    public int IbOnl(int ud, int v) => 0;

    public int IbWrt(int ud, ReadOnlySpan<byte> data)
    {
        IbWrtCalled = true;
        throw new InvalidOperationException("IbWrt should not be called in this test.");
    }

    public int IbRd(int ud, Span<byte> buffer)
    {
        IbRdCalled = true;
        throw new InvalidOperationException("IbRd should not be called in this test.");
    }

    public int IbClr(int ud) => 0;

    public int IbRsp(int ud) => 0;

    public int ThreadIbsta() => 0;
    public int ThreadIberr() => 0;
    public int ThreadIbcnt() => 0;

    public string GpibErrorString(int error) => $"Guarded error {error}";

    public void FindLstn(int boardIndex, ushort[] padList, ushort[] resultList, int maxNumResults)
    {
        throw new NotImplementedException();
    }
}

[ExcludeFromCodeCoverage]
/// <summary>
/// Minimal low-level impl that lets us control ibsta/iberr and the error string
/// to drive the two branches in CheckError.
/// </summary>
internal sealed class ErrorSimulationLowLevel : IGpibLowLevel
{
    private readonly int _status;
    private readonly int _error;
    private readonly string _errorMessage;

    public ErrorSimulationLowLevel(int status, int error, string errorMessage)
    {
        _status = status;
        _error = error;
        _errorMessage = errorMessage;
    }

    public int IbDev(int boardIndex, int pad, int sad, int tmo, int eot, int eos) => 1;

    public int IbOnl(int ud, int v) => 0;

    public int IbWrt(int ud, ReadOnlySpan<byte> data) => 0;

    public int IbRd(int ud, Span<byte> buffer) => 0;

    public int IbClr(int ud) => 0;

    public int IbRsp(int ud) => 0;

    public int ThreadIbsta() => _status;
    public int ThreadIberr() => _error;
    public int ThreadIbcnt() => 0;

    public string GpibErrorString(int error) => _errorMessage;

    public void FindLstn(int boardIndex, ushort[] padList, ushort[] resultList, int maxNumResults)
    {
        throw new NotImplementedException();
    }
}
