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
using System.Collections.Concurrent;
using LinuxGPIB.Net.Abstractions;
using LinuxGPIB.Net.Management.Abstractions;

namespace LinuxGPIB.Net.Management;

public sealed class GpibDeviceManager : IGpibDeviceManager
{
    private readonly TimeSpan _idleTimeout;
    private readonly ConcurrentDictionary<GpibAddress, DeviceEntry> _devices = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;
    private readonly Func<GpibAddress, IGpibDevice> _deviceFactory;

    /// <summary>
    /// Creates a new <see cref="GpibDeviceManager"/> that provides coordinated,
    /// thread-safe access to GPIB devices.  
    /// </summary>
    /// <remarks>
    /// This constructor is intended for typical use cases where all devices
    /// can be created with the default <see cref="GpibDevice"/> configuration.
    /// A device instance is created on first use and automatically disposed
    /// after a period of inactivity.
    /// <para/>
    /// Operations targeting the same GPIB address are serialized to ensure
    /// thread safety. Operations targeting different devices may execute
    /// concurrently.
    /// </remarks>
    /// <param name="idleTimeout">
    /// The duration a device may remain unused before being
    /// automatically disposed. If <c>null</c>, a default of five minutes is used.
    /// </param>
    public GpibDeviceManager(TimeSpan? idleTimeout = null) : this(addr => new GpibDevice(addr), idleTimeout) { }

    /// <summary>
    /// Creates a new <see cref="GpibDeviceManager"/> using a custom factory
    /// function to construct <see cref="IGpibDevice"/> instances.
    /// </summary>
    /// <remarks>
    /// This overload is intended for advanced scenarios where callers need
    /// full control over device creation—for example:
    /// <list type="bullet">
    ///   <item><description>Setting device-specific defaults (line endings, timeouts, etc.)</description></item>
    ///   <item><description>Configuring devices differently based on their address</description></item>
    ///   <item><description>Injecting mock or wrapped devices for testing</description></item>
    ///   <item><description>Supporting extended or vendor-specific device implementations</description></item>
    /// </list>
    /// Devices created by the factory are cached per address and disposed
    /// automatically after the configured idle timeout.
    /// </remarks>
    /// <param name="deviceFactory">
    /// A function that constructs a new <see cref="IGpibDevice"/> for a given
    /// <see cref="GpibAddress"/>. The factory should be fast and side-effect free
    /// beyond creating and configuring the device.
    /// </param>
    /// <param name="idleTimeout">
    /// The duration a device may remain unused before being
    /// automatically disposed. If <c>null</c>, a default of five minutes is used.
    /// </param>
    public GpibDeviceManager(Func<GpibAddress, IGpibDevice> deviceFactory, TimeSpan? idleTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(deviceFactory);

        _deviceFactory = deviceFactory;
        _idleTimeout = idleTimeout ?? TimeSpan.FromMinutes(5);

        // Periodically check for idle devices
        _cleanupTimer = new Timer(
            _ => CleanupIdleDevices(),
            null,
            dueTime: TimeSpan.FromMinutes(1),
            period: TimeSpan.FromMinutes(1));
    }

    public async Task ExecuteAsync(GpibAddress address, Func<IGpibDevice, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        _ = await ExecuteAsync<object>(address, async (dev) =>
        {
            await action(dev).ConfigureAwait(false);
            return null!;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        GpibAddress address, 
        Func<IGpibDevice, Task<TResult>> action, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(action);

        var entry = _devices.GetOrAdd(address, addr =>
        {
            var device = _deviceFactory(addr);
            return new DeviceEntry(device);
        });

        // Ensure only one caller uses this device at a time
        await entry.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        TResult result;
        try
        {
            entry.LastUsedUtc = DateTime.UtcNow;
            result = await action(entry.Device).ConfigureAwait(false);
            entry.LastUsedUtc = DateTime.UtcNow;
        }
        finally
        {
            entry.Lock.Release();
        }

        return result;
    }

    private void CleanupIdleDevices()
    {
        if (_disposed) return;

        var now = DateTime.UtcNow;

        foreach (var kvp in _devices)
        {
            var address = kvp.Key;
            var entry = kvp.Value;

            // Try to acquire without waiting – don't kill in-use devices
            if (!entry.Lock.Wait(0))
                continue;

            try
            {
                var isIdle  = now - entry.LastUsedUtc >= _idleTimeout;
                if (isIdle  && _devices.TryRemove(address, out _))
                {
                    entry.Device.Dispose();
                }
            }
            finally
            {
                entry.Lock.Release();
            }
        }
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cleanupTimer.Dispose();

        foreach (var kvp in _devices)
        {
            var entry = kvp.Value;

            // Wait for any in-flight actions to complete
            entry.Lock.Wait();
            try
            {
                entry.Device.Dispose();
            }
            finally
            {
                entry.Lock.Release();
            }
        }

        _devices.Clear();
    }

    private sealed class DeviceEntry(IGpibDevice device)
    {
        public IGpibDevice Device { get; } = device;
        public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow;
        public SemaphoreSlim Lock { get; } = new(1, 1);
    }
}
