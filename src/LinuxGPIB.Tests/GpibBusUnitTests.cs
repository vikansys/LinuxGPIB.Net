using System.Diagnostics.CodeAnalysis;
using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net.Tests
{
    [TestClass, ExcludeFromCodeCoverage, TestCategory("Unit")]
    public sealed class GpibBusUnitTests
    {
        [TestMethod]
        public void Ctor_NullLowLevel_Throws()
        {
            // Arrange
            IGpibLowLevel? lowLevel = null;

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => new GpibBus(0, lowLevel!));
        }

        [TestMethod]
        [DataRow(-1, 10)] // min < 0
        [DataRow(1, 31)]  // max > 30
        [DataRow(10, 5)]  // min > max
        public void DiscoverDevices_InvalidRange_ThrowsArgument(int minPrimary, int maxPrimary)
        {
            // Arrange
            var fake = new FakeBusLowLevel();
            var bus = new GpibBus(0, fake);

            // Act + Assert
            Assert.Throws<ArgumentException>(() =>
                bus.DiscoverDevices(minPrimary, maxPrimary));
        }

        [TestMethod]
        public void DiscoverDevices_NoDevices_ReturnsEmptyList()
        {
            // Arrange
            var fake = new FakeBusLowLevel
            {
                Results = Array.Empty<ushort>(),
                Ibcnt = 0
            };

            var bus = new GpibBus(0, fake);

            // Act
            var devices = bus.DiscoverDevices();

            // Assert
            Assert.IsTrue(fake.FindLstnCalled, "FindLstn should be called.");
            Assert.IsEmpty(devices, "Expected no devices to be discovered.");
        }

        [TestMethod]
        public void DiscoverDevices_SingleDevice_ParsesPadAndSadCorrectly_AndBuildsPadListWithNoAddr()
        {
            // Arrange
            // raw = 0x0205 => PAD = 0x05 = 5, SAD = 0x02 = 2
            var fake = new FakeBusLowLevel
            {
                Results = new[] { (ushort)0x0205 },
                Ibcnt = 1
            };

            var bus = new GpibBus(0, fake);

            // Act
            var devices = bus.DiscoverDevices(minPrimary: 5, maxPrimary: 7);

            // Assert: device decoding
            var device = AssertSingle(devices);
            Assert.AreEqual(5, device.Primary);
            Assert.AreEqual(2, device.Secondary);

            // Assert: pad list was built correctly
            Assert.IsNotNull(fake.LastPadList);
            var padList = fake.LastPadList!;
            Assert.HasCount(4, padList, "Expected three pads plus NOADDR terminator.");
            Assert.AreEqual((ushort)5, padList[0]);
            Assert.AreEqual((ushort)6, padList[1]);
            Assert.AreEqual((ushort)7, padList[2]);
            Assert.AreEqual(FakeBusLowLevel.NOADDR, padList[3]);
        }

        [TestMethod]
        public void DiscoverDevices_MultipleDevices_ParsesAllWithinIbcnt()
        {
            // Arrange
            // Encodings:
            // 0x0001 => PAD=1,SAD=0
            // 0x0302 => PAD=2,SAD=3
            // 0x1F00 => PAD=0,SAD=0x1F
            var fake = new FakeBusLowLevel
            {
                Results = new[] { (ushort)0x0001, (ushort)0x0302, (ushort)0x1F00 },
                Ibcnt = 3
            };

            var bus = new GpibBus(0, fake);

            // Act
            var devices = bus.DiscoverDevices(1, 30);

            // Assert
            Assert.HasCount(3, devices);

            Assert.AreEqual(1, devices[0].Primary);
            Assert.AreEqual(0, devices[0].Secondary);

            Assert.AreEqual(2, devices[1].Primary);
            Assert.AreEqual(3, devices[1].Secondary);

            Assert.AreEqual(0, devices[2].Primary);
            Assert.AreEqual(0x1F, devices[2].Secondary);
        }

        [TestMethod]
        public void DiscoverDevices_TruncatesWhenIbcntGreaterThanMaxResults()
        {
            // Arrange
            // min=0,max=0 -> countPads=1 -> maxResults=32
            // We simulate more "found" than maxResults.
            var manyResults = new ushort[40];
            for (int i = 0; i < manyResults.Length; i++)
            {
                // PAD=i, SAD=0
                manyResults[i] = (ushort)(i & 0xFF);
            }

            var fake = new FakeBusLowLevel
            {
                Results = manyResults,
                Ibcnt = 40 // larger than maxResults (32)
            };

            var bus = new GpibBus(0, fake);

            // Act
            var devices = bus.DiscoverDevices(minPrimary: 1, maxPrimary: 1);

            // Assert
            // Implementation does: found = min(ThreadIbcnt, maxResults) => min(40,32) = 32
            Assert.HasCount(32, devices, "Expected results to be truncated to maxResults.");
            for (int i = 0; i < devices.Count; i++)
            {
                Assert.AreEqual(i, devices[i].Primary, $"Unexpected PAD at index {i}");
                Assert.AreEqual(0, devices[i].Secondary, $"Unexpected SAD at index {i}");
            }
        }

        [TestMethod]
        public void DiscoverDevices_UsesProvidedBoardIndex()
        {
            // Arrange
            var fake = new FakeBusLowLevel
            {
                Results = Array.Empty<ushort>(),
                Ibcnt = 0
            };

            int boardIndex = 2;
            var bus = new GpibBus(boardIndex, fake);

            // Act
            _ = bus.DiscoverDevices(1, 10);

            // Assert
            Assert.IsTrue(fake.FindLstnCalled);
            Assert.AreEqual(boardIndex, fake.LastBoardIndex);
        }

        private static GpibAddress AssertSingle(IReadOnlyList<GpibAddress> devices)
        {
            Assert.HasCount(1, devices, $"Expected exactly one device, got {devices.Count}.");
            return devices[0];
        }

        /// <summary>
        /// Minimal fake IGpibLowLevel for testing GpibBus.DiscoverDevices.
        /// Only FindLstn and ThreadIbcnt are used; others can be no-ops or throw.
        /// </summary>
        private sealed class FakeBusLowLevel : IGpibLowLevel
        {
            public const ushort NOADDR = 0xFFFF;

            public ushort[] Results { get; set; } = Array.Empty<ushort>();
            public int Ibcnt { get; set; }

            public bool FindLstnCalled { get; private set; }
            public int LastBoardIndex { get; private set; }
            public ushort[]? LastPadList { get; private set; }

            public int IbDev(int boardIndex, int pad, int sad, int tmo, int eot, int eos)
                => 0;

            public int IbOnl(int ud, int v) => 0;

            public int IbWrt(int ud, ReadOnlySpan<byte> data) => 0;

            public int IbRd(int ud, Span<byte> buffer) => 0;

            public int IbClr(int ud) => 0;

            public int IbRsp(int ud) => 0;

            public int ThreadIbsta() => 0;

            public int ThreadIberr() => 0;

            public int ThreadIbcnt() => Ibcnt;

            public string GpibErrorString(int error) => $"Error {error}";

            public void FindLstn(int boardIndex, ushort[] padList, ushort[] resultList, int maxNumResults)
            {
                FindLstnCalled = true;
                LastBoardIndex = boardIndex;
                LastPadList = (ushort[])padList.Clone();

                // Simulate native FindLstn: copy up to min(Results.Length, maxNumResults)
                int toCopy = Math.Min(Results.Length, maxNumResults);
                Array.Clear(resultList, 0, resultList.Length);
                Array.Copy(Results, 0, resultList, 0, toCopy);
            }
        }
    }
}
