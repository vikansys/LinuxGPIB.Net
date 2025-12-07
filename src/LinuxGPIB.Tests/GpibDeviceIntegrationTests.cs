using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LinuxGPIB.Net.Tests
{
    [TestClass, ExcludeFromCodeCoverage]
    public class GpibDeviceIntegrationTests
    {
        [TestMethod]
        [TestCategory("Integration")]
        public async Task Idn_Query_Roundtrip_Works_When_Hardware_Is_Available()
        {
            // Use an environment variable to control whether this test should run
            // e.g. export LINUXGPIB_IT_ENABLED=1 on a machine with hardware
            var enabled = Environment.GetEnvironmentVariable("LINUXGPIB_IT_ENABLED");
            if (!string.Equals(enabled, "1", StringComparison.Ordinal))
            {
                Assert.Inconclusive("Hardware integration tests are disabled. Set LINUXGPIB_IT_ENABLED=1 to enable.");
            }

            // Optionally configurable via env vars too
            int boardIndex      = int.Parse(Environment.GetEnvironmentVariable("LINUXGPIB_IT_BOARD")      ?? "0", CultureInfo.InvariantCulture);
            int primaryAddress  = int.Parse(Environment.GetEnvironmentVariable("LINUXGPIB_IT_PRIMARY")    ?? "1", CultureInfo.InvariantCulture);
            int secondaryAddress= int.Parse(Environment.GetEnvironmentVariable("LINUXGPIB_IT_SECONDARY")  ?? "0", CultureInfo.InvariantCulture);

            using var device = new GpibDevice(primaryAddress: primaryAddress, secondaryAddress: secondaryAddress, boardIndex: boardIndex);

            device.Write("*IDN?");
            var idn = device.Read();

            Console.WriteLine($"Instrument IDN: {idn}");

            // Minimal sanity check – just assert we got something non-empty back
            Assert.IsFalse(string.IsNullOrWhiteSpace(idn), "Expected non-empty *IDN? response.");
        }
    }
}
