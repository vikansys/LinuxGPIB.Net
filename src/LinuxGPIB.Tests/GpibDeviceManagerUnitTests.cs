using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LinuxGPIB.Net;
using LinuxGPIB.Net.Abstractions;
using LinuxGPIB.Net.Management;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LinuxGPIB.Net.Tests
{
    [TestClass, ExcludeFromCodeCoverage, TestCategory("Unit")]
    public sealed class GpibDeviceManagerTests
    {
        private static readonly GpibAddress Addr1 = new(5, 0);
        private static readonly GpibAddress Addr2 = new(10, 0);

        [TestMethod]
        public void Ctor_NullFactory_Throws()
        {
            // Arrange
            Func<GpibAddress, IGpibDevice>? factory = null;

            // Act + Assert
            Assert.Throws<ArgumentNullException>(
                () => new GpibDeviceManager(factory!, idleTimeout: TimeSpan.FromMinutes(5)));
        }

        [TestMethod]
        public async Task ExecuteAsync_NullAction_Throws()
        {
            // Arrange
            var manager = new GpibDeviceManager(_ => new TrackingDevice());

            Func<IGpibDevice, Task>? action = null;

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => manager.ExecuteAsync(Addr1, action!));
        }

        [TestMethod]
        public async Task ExecuteAsync_CreatesDeviceOncePerAddress_AndReuses()
        {
            // Arrange
            int factoryCallCount = 0;
            TrackingDevice? createdDevice = null;

            var manager = new GpibDeviceManager(addr =>
            {
                factoryCallCount++;
                createdDevice = new TrackingDevice();
                return createdDevice;
            });

            // Act
            await manager.ExecuteAsync(Addr1, dev =>
            {
                Assert.AreSame(createdDevice, dev);
                return Task.CompletedTask;
            });

            await manager.ExecuteAsync(Addr1, dev =>
            {
                // Should be the same device instance
                Assert.AreSame(createdDevice, dev);
                return Task.CompletedTask;
            });

            // Assert
            Assert.AreEqual(1, factoryCallCount, "Factory should be called once per address.");
        }

        [TestMethod]
        public async Task ExecuteAsync_DifferentAddresses_UseDifferentDevices()
        {
            // Arrange
            int factoryCallCount = 0;
            TrackingDevice? device1 = null;
            TrackingDevice? device2 = null;

            var manager = new GpibDeviceManager(addr =>
            {
                factoryCallCount++;
                if (addr.Equals(Addr1))
                {
                    device1 = new TrackingDevice();
                    return device1;
                }

                device2 = new TrackingDevice();
                return device2!;
            });

            // Act
            await manager.ExecuteAsync(Addr1, dev =>
            {
                Assert.AreSame(device1, dev);
                return Task.CompletedTask;
            });

            await manager.ExecuteAsync(Addr2, dev =>
            {
                Assert.AreSame(device2, dev);
                return Task.CompletedTask;
            });

            // Assert
            Assert.AreEqual(2, factoryCallCount, "Factory should be called once for each distinct address.");
            Assert.IsNotNull(device1);
            Assert.IsNotNull(device2);
            Assert.AreNotSame(device1, device2);
        }

        [TestMethod]
        public async Task Dispose_DisposesDevices_AndPreventsFurtherExecute()
        {
            // Arrange
            var device = new TrackingDevice();
            var manager = new GpibDeviceManager(_ => device);

            // Use the manager once so it creates and caches the device
            await manager.ExecuteAsync(Addr1, _ => Task.CompletedTask);

            // Act
            manager.Dispose();

            // Assert: device should be disposed
            Assert.IsTrue(device.Disposed, "Device should be disposed when manager is disposed.");

            // Further ExecuteAsync should throw ObjectDisposedException
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => manager.ExecuteAsync(Addr1, _ => Task.CompletedTask));
        }

        [TestMethod]
        public async Task CleanupIdleDevices_RemovesAndDisposesIdleDevices()
        {
            // Arrange
            int factoryCallCount = 0;
            TrackingDevice? firstDevice = null;
            var idleTimeout = TimeSpan.FromMilliseconds(10);

            var manager = new GpibDeviceManager(addr =>
            {
                factoryCallCount++;
                var dev = new TrackingDevice();
                if (factoryCallCount == 1)
                    firstDevice = dev;
                return dev;
            }, idleTimeout);

            // Use the device once so it's created and tracked
            await manager.ExecuteAsync(Addr1, _ => Task.CompletedTask);

            Assert.IsNotNull(firstDevice, "Factory should have created a device.");

            // Wait long enough for it to be considered idle
            Thread.Sleep(idleTimeout + TimeSpan.FromMilliseconds(20));

            // Invoke CleanupIdleDevices via reflection (it's private)
            var cleanupMethod = typeof(GpibDeviceManager)
                .GetMethod("CleanupIdleDevices", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(cleanupMethod, "CleanupIdleDevices method not found via reflection.");

            cleanupMethod.Invoke(manager, null);

            // First device should have been disposed
            Assert.IsTrue(firstDevice!.Disposed, "Idle device should be disposed by cleanup.");

            // Next ExecuteAsync for same address should create a new device (entry was removed)
            await manager.ExecuteAsync(Addr1, _ => Task.CompletedTask);

            Assert.AreEqual(2, factoryCallCount, "Factory should be called again after idle device cleanup.");
        }

        private sealed class TrackingDevice : IGpibDevice, IDisposable
        {
            public bool Disposed { get; private set; }
            public GpibLineEnding DefaultLineEnding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public bool IsMessageAvailable => throw new NotImplementedException();

            public bool IsServiceRequestAsserted => throw new NotImplementedException();

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                Disposed = true;
            }

            public T Query<T>(string command)
            {
                throw new NotImplementedException();
            }

            public string Query(string command)
            {
                throw new NotImplementedException();
            }

            public Task<T> QueryAsync<T>(string query, CancellationToken cancellationToken = default, int pollIntervalMs = 50, int timeoutMs = 15000)
            {
                throw new NotImplementedException();
            }

            public Task<string> QueryAsync(string query, CancellationToken cancellationToken = default, int pollIntervalMs = 50, int timeoutMs = 15000)
            {
                throw new NotImplementedException();
            }

            public T Read<T>()
            {
                throw new NotImplementedException();
            }

            public string Read()
            {
                throw new NotImplementedException();
            }

            public int ReadBytes(Span<byte> buffer)
            {
                throw new NotImplementedException();
            }

            public int SerialPoll()
            {
                throw new NotImplementedException();
            }

            public Task WaitForMessageAsync(CancellationToken cancellationToken = default, int pollIntervalMs = 50, int timeoutMs = 15000)
            {
                throw new NotImplementedException();
            }

            public Task WaitForServiceRequestAsync(CancellationToken cancellationToken = default, int pollIntervalMs = 50, int timeoutMs = 15000)
            {
                throw new NotImplementedException();
            }

            public void Write(string command)
            {
                throw new NotImplementedException();
            }

            public void WriteBytes(ReadOnlySpan<byte> data)
            {
                throw new NotImplementedException();
            }
        }
    }
}
