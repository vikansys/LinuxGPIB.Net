// LinuxGPIB.Net
// Copyright (C) 2025 Vikansys
//
//This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <https://www.gnu.org/licenses/>.
using System.Text;
using LinuxGPIB.Net.Interop;
using System.Buffers;
using System.Diagnostics;
using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net;

public sealed class GpibDevice : IGpibDevice
{
    private readonly IGpibLowLevel _lowLevel;
    private readonly int _ud; // Unit Descriptor
    private bool _disposed;
    
    // Crucial for looping reads: indicates the EOI line was asserted
    internal const int END = 1 << 13; // EOI or EOS detected
    internal const int TimeoutMs = 15000;
    internal const int PollIntervalMs = 50;

    /// <summary>
    /// MAV (Message Available) bit is 0x10, bit 4 in the Status Byte
    /// </summary>
    public const int MAV = 0x10; // Message Available Bit

    /// <summary>
    /// RQS (Request Service) is the GPIB interrupt bit 0x40, bit 6 in the Status Byte register.
    /// </summary>
    public const int RQS = 0x40;

    public GpibLineEnding DefaultLineEnding { get; set; } = GpibLineEnding.Lf;

    public readonly GpibTimeout Timeout;

    public readonly GpibAddress Address;

    
    /// <summary>
    /// Initializes a new connection to a GPIB device using its primary and
    /// secondary address numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is not thread-safe. If accessed from multiple threads,
    /// callers must coordinate access externally or use
    /// <see cref="Management.IGpibDeviceManager"/>
    /// to ensure safe serialized access.
    /// </para>
    /// <para>
    /// The constructor opens a device session immediately. The session
    /// remains active until the instance is disposed.
    /// </para>
    /// </remarks>
    /// <param name="primaryAddress">
    /// The primary bus address of the instrument (1–30).
    /// </param>
    /// <param name="secondaryAddress">
    /// The secondary bus address (0 for none, otherwise 1–31).
    /// </param>
    /// <param name="timeout">
    /// The bus timeout for read/write operations.
    /// </param>
    /// <param name="boardIndex">
    /// The GPIB controller board index (typically 0).
    /// </param>
    /// <param name="eot">
    /// Whether to assert the EOI line at the end of writes
    /// (1 = assert, 0 = do not assert).
    /// </param>
    /// <param name="eos">
    /// The End-Of-String mode (0 for disabled, otherwise ASCII byte value).
    /// </param>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the library is used on a non-Linux platform.
    /// </exception>
    /// <exception cref="GpibException">
    /// Thrown if the underlying native call fails to open the device.
    /// </exception>
    public GpibDevice(int primaryAddress, int secondaryAddress = 0, GpibTimeout timeout = GpibTimeout.T10s, int boardIndex = 0, int eot = 1, int eos = 0) 
    : this(boardIndex, primaryAddress, secondaryAddress, timeout, eot, eos, new NativeGpibLowLevel())
    {
    }

    /// <summary>
    /// Initializes a new connection to a GPIB device using a
    /// <see cref="GpibAddress"/> value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is not thread-safe. If accessed by multiple threads,
    /// callers must synchronize access manually or operate through a
    /// <see cref="LinuxGPIB.Net.Management.IGpibDeviceManager"/>.
    /// </para>
    /// <para>
    /// The constructor opens a session to the device immediately. The
    /// connection remains active until the instance is disposed.
    /// </para>
    /// </remarks>
    /// <param name="address">
    /// The GPIB address of the target instrument.
    /// </param>
    /// <param name="timeout">
    /// The bus timeout used for I/O operations.
    /// </param>
    /// <param name="boardIndex">
    /// The GPIB controller board index (typically 0).
    /// </param>
    /// <param name="eot">
    /// Whether to assert the EOI line at the end of writes.
    /// </param>
    /// <param name="eos">
    /// The End-Of-String mode (0 for disabled, otherwise an ASCII value).
    /// </param>
    /// <exception cref="GpibException">
    /// Thrown if the device cannot be opened due to a native library error.
    /// </exception>
    public GpibDevice(GpibAddress address, GpibTimeout timeout = GpibTimeout.T10s, int boardIndex = 0, int eot = 1, int eos = 0) 
    : this(boardIndex, address.Primary, address.Secondary, timeout, eot, eos, new NativeGpibLowLevel())
    {
    }

    internal GpibDevice(
    int boardIndex,
    int primaryAddress,
    int secondaryAddress,
    GpibTimeout timeout,
    int eot,
    int eos,
    IGpibLowLevel lowLevel)
    {
        ArgumentNullException.ThrowIfNull(lowLevel);
        Address = new GpibAddress(primaryAddress, secondaryAddress);
        _lowLevel = lowLevel;
        Timeout = timeout;
        _ud = _lowLevel.IbDev(boardIndex, primaryAddress, secondaryAddress, (int)timeout, eot, eos);

        if (_ud < 0)
        {
            throw new GpibException($"Failed to open GPIB device at address {primaryAddress}. Internal Error.");
        }
    }

    ~GpibDevice() => Dispose();

    public void Write(string command)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(command);
        
        // Append the configured terminator
        string terminated = DefaultLineEnding switch
        {
            GpibLineEnding.None => command,
            GpibLineEnding.Lf => command + '\n',
            GpibLineEnding.Cr => command + '\r',
            GpibLineEnding.CrLf => command + "\r\n",
            _ => command
        };

        WriteBytes(Encoding.ASCII.GetBytes(terminated));
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (data.Length == 0)
            return;

        _ = _lowLevel.IbWrt(_ud, data);

        _lowLevel.Validate(nameof(WriteBytes), data);
    }

    public int ReadBytes(Span<byte> buffer)
    {
        ThrowIfDisposed();

        if (buffer.Length == 0)
            return 0;

        _ = _lowLevel.IbRd(_ud, buffer);

        _lowLevel.Validate(nameof(ReadBytes));

        return _lowLevel.ThreadIbcnt();
    }

    public string Read()
    {
        ThrowIfDisposed();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
        using var ms = new MemoryStream();

        try
        {
            while (true)
            {
                int bytesRead = ReadBytes(buffer.AsSpan());

                if (bytesRead > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                // CRITICAL: Check the status *after* the read operation for flow control.
                int status = _lowLevel.ThreadIbsta();

                // If EOI (END) is set, we stop the loop.
                if ((status & END) != 0)
                {
                    break;
                }

                // If we read 0 bytes (and haven't hit EOI), we must stop to prevent an infinite loop.
                if (bytesRead == 0)
                {
                    break;
                }
            }

            var dataSpan = ms.GetBuffer().AsSpan(0, (int)ms.Length);
            return Encoding.ASCII.GetString(dataSpan).TrimEnd('\r', '\n', '\0');
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public int SerialPoll()
    {
        ThrowIfDisposed();

        int statusByte = _lowLevel.IbRsp(_ud);

        _lowLevel.Validate(nameof(SerialPoll));

        return statusByte;
    }

    public bool IsMessageAvailable => (SerialPoll() & MAV) != 0;

    public bool IsServiceRequestAsserted => (SerialPoll() & RQS) != 0;

    public async Task WaitForMessageAsync(CancellationToken cancellationToken = default, int pollIntervalMs = PollIntervalMs, int timeoutMs = TimeoutMs)
    {
        await WaitForDeviceAsync(nameof(WaitForMessageAsync), () => IsMessageAvailable, cancellationToken, pollIntervalMs, timeoutMs);
    }

    public async Task WaitForServiceRequestAsync(CancellationToken cancellationToken = default,int pollIntervalMs = PollIntervalMs, int timeoutMs = TimeoutMs)
    {
        await WaitForDeviceAsync(nameof(WaitForServiceRequestAsync), () => IsServiceRequestAsserted, cancellationToken, pollIntervalMs, timeoutMs);
    }

    /// <summary>
    /// Shared core logic for asynchronous device polling.
    /// </summary>
    private async Task WaitForDeviceAsync(string operation, Func<bool> interrupt, CancellationToken cancellationToken, int pollIntervalMs = PollIntervalMs, int timeoutMs = TimeoutMs)
    {
        ThrowIfDisposed();

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (interrupt())
            {
                return;
            }

            await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        // Enhanced exception message now includes the operation name.
        throw new TimeoutException($"The asynchronous '{operation}' operation timed out after {timeoutMs}ms.");
    }

    public void Clear()
    {
        ThrowIfDisposed();
        _ = _lowLevel.IbClr(_ud);
        _lowLevel.Validate(nameof(Clear));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _ = _lowLevel.IbOnl(_ud, 0);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}