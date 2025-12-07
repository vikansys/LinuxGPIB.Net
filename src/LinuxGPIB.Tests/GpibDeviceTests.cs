using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using LinuxGPIB.Net.Tests.Fakes;

namespace LinuxGPIB.Net.Tests;

[TestClass, ExcludeFromCodeCoverage, TestCategory("Unit")]
public class GpibDeviceUnitTests
{
    [TestMethod]
    public void Ctor_Succeeds_When_IbDev_Returns_ValidDescriptor()
    {
        var fake = new FakeGpibLowLevel
        {
            NextUd = 5,
            FailIbDev = false
        };

        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 1,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // No exception = success.
        Assert.IsNotNull(device);
    }

    [TestMethod]
    public void Ctor_ThrowsExactly_GpibException_When_IbDev_Fails()
    {
        var fake = new FakeGpibLowLevel
        {
            FailIbDev = true
        };

        Assert.ThrowsExactly<GpibException>(() =>
            new GpibDevice(
                boardIndex: 0,
                primaryAddress: 1,
                secondaryAddress: 0,
                timeout: GpibTimeout.T10s,
                eot: 1,
                eos: 0,
                lowLevel: fake));
    }

    [TestMethod]
    [DataRow(GpibLineEnding.None, "CMD", "CMD")]
    [DataRow(GpibLineEnding.Lf, "CMD", "CMD\n")]
    [DataRow(GpibLineEnding.Cr, "CMD", "CMD\r")]
    [DataRow(GpibLineEnding.CrLf, "CMD", "CMD\r\n")]
    public void Write_Appends_Correct_Terminator(GpibLineEnding lineEnding, string input, string expected)
    {
        var fake = new FakeGpibLowLevel();
        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake)
        {
            DefaultLineEnding = lineEnding
        };

        device.Write(input);

        Assert.IsNotNull(fake.LastWrite);
        var written = Encoding.ASCII.GetString(fake.LastWrite!);
        Assert.AreEqual(expected, written);
    }

    [TestMethod]
    public void Read_Returns_Trimmed_Ascii_String()
    {
        var fake = new FakeGpibLowLevel();
        // Simulate instrument sending ASCII with CR/LF terminator
        fake.SetReadResponse("MEAS:VAL 123.45\r\n", assertEnd: true);

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        var result = device.Read();

        Assert.AreEqual("MEAS:VAL 123.45", result);
    }

    [TestMethod]
    public async Task WaitForMessageAsync_Completes_When_MAV_Is_Already_Available()
    {
        var fake = new FakeGpibLowLevel
        {
            // Make IbSrsp return a status byte with MAV (0x10) set
            SerialPollStatusByte = 0x10
        };

        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 1,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        await device.WaitForMessageAsync(
            cancellationToken: CancellationToken.None,
            pollIntervalMs: 5,
            timeoutMs: 1000);
    }

    [TestMethod]
    public async Task WaitForMessageAsync_ThrowsExactly_Timeout_When_MAV_Never_Set()
    {
        var fake = new FakeGpibLowLevel
        {
            SerialPollStatusByte = 0 // MAV never set
        };

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () =>
        {
            await device.WaitForMessageAsync(
                cancellationToken: CancellationToken.None,
                pollIntervalMs: 5,
                timeoutMs: 50);
        });
    }

    [TestMethod]
    public async Task WaitForMessageAsync_Throws_OperationCanceled_When_Canceled()
    {
        var fake = new FakeGpibLowLevel
        {
            SerialPollStatusByte = 0 // MAV never set
        };

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(5); // very quick cancellation

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await device.WaitForMessageAsync(
                cancellationToken: cts.Token,
                pollIntervalMs: 10,
                timeoutMs: 15000);
        });
    }


    [TestMethod]
    public void Ctor_ThrowsExactly_ArgumentNullException_When_LowLevel_Is_Null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new GpibDevice(
                boardIndex: 0,
                primaryAddress: 1,
                secondaryAddress: 0,
                timeout: GpibTimeout.T10s,
                eot: 1,
                eos: 0,
                lowLevel: null!));
    }

    [TestMethod]
    public async Task WaitForServiceRequestAsync_Completes_When_RQS_Is_Already_Available()
    {
        var fake = new FakeGpibLowLevel
        {
            // IbSrsp returns a status byte with RQS (0x40) set
            SerialPollStatusByte = GpibDevice.RQS
        };

        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 1,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        await device.WaitForServiceRequestAsync(
            cancellationToken: CancellationToken.None,
            pollIntervalMs: 5,
            timeoutMs: 200);
    }

    [TestMethod]
    public async Task WaitForServiceRequestAsync_ThrowsExactly_Timeout_When_RQS_Never_Set()
    {
        var fake = new FakeGpibLowLevel
        {
            SerialPollStatusByte = 0 // RQS never set
        };

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () =>
        {
            await device.WaitForServiceRequestAsync(
                cancellationToken: CancellationToken.None,
                pollIntervalMs: 5,
                timeoutMs: 50);
        });
    }

    [TestMethod]
    public async Task WaitForServiceRequestAsync_Throws_OperationCanceled_When_Canceled()
    {
        var fake = new FakeGpibLowLevel
        {
            SerialPollStatusByte = 0 // RQS never set
        };

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(5);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await device.WaitForServiceRequestAsync(
                cancellationToken: cts.Token,
                pollIntervalMs: 10,
                timeoutMs: 15000);
        });
    }

    [TestMethod]
    public void Dispose_Prevents_Further_Use()
    {
        var fake = new FakeGpibLowLevel();

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        device.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => device.Write("CMD"));
        Assert.ThrowsExactly<ObjectDisposedException>(() => device.Read());
        Assert.ThrowsExactly<ObjectDisposedException>(() => device.Clear());
    }

    [TestMethod]
    public void Dispose_Can_Be_Called_Multiple_Times()
    {
        var fake = new FakeGpibLowLevel();

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        device.Dispose();
        device.Dispose(); // should not throw
    }

    [TestMethod]
    public void Read_Stops_On_EOI_Even_Without_Terminator()
    {
        var fake = new FakeGpibLowLevel();
        fake.SetReadResponse("DATA", assertEnd: true);

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        var result = device.Read();

        Assert.AreEqual("DATA", result);
    }

    [TestMethod]
    public void Read_Stops_When_BytesRead_Is_Zero_And_End_Not_Set()
    {
        var fake = new FakeGpibLowLevel();
        // No END bit will ever be set
        fake.SetReadResponse("DATA", assertEnd: false);

        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, fake);

        var result = device.Read();

        Assert.AreEqual("DATA", result);
    }


    [TestMethod]
    public void WriteBytes_Does_Nothing_When_Data_Length_Is_Zero()
    {
        var lowLevel = new GuardedLowLevel();
        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, lowLevel);

        device.WriteBytes(ReadOnlySpan<byte>.Empty);

        Assert.IsFalse(lowLevel.IbWrtCalled, "IbWrt should not be called when data.Length == 0.");
    }

    [TestMethod]
    public void ReadBytes_Returns_Zero_When_Buffer_Length_Is_Zero()
    {
        var lowLevel = new GuardedLowLevel();
        var device = new GpibDevice(
            0, 1, 0, GpibTimeout.T10s, 1, 0, lowLevel);

        var result = device.ReadBytes(Span<byte>.Empty);

        Assert.AreEqual(0, result);
        Assert.IsFalse(lowLevel.IbRdCalled, "IbRd should not be called when buffer.Length == 0.");
    }

    [TestMethod]
    public void Validate_ThrowsExactly_GpibException_On_Timeout_Status()
    {
        const int timoStatus = 1 << 14;
        const int errorCode = 42;

        var lowLevel = new ErrorSimulationLowLevel(
            status: timoStatus,
            error: errorCode,
            errorMessage: "ignored for timeout case");

        var ex = Assert.ThrowsExactly<GpibException>(
            () => lowLevel.Validate("TestOperation"));

        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }


    [TestMethod]
public void Validate_ThrowsExactly_GpibException_On_Generic_Error_Status()
{
    const int errStatus = 1 << 15;
    const int errorCode = 99;
    const string errorMessage = "Synthetic error";

    var lowLevel = new ErrorSimulationLowLevel(
        status: errStatus,
        error: errorCode,
        errorMessage: errorMessage);

    var ex = Assert.ThrowsExactly<GpibException>(
        () => lowLevel.Validate("TestOperation"));

    Assert.AreEqual(errorMessage, ex.Message);
}

    [TestMethod]
    public void Read_T_Double_ParsesNumericResponse()
    {
        // Arrange
        var fake = new FakeGpibLowLevel();
        fake.SetReadResponse("1.234\n"); // SCPI-style numeric response
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act
        double value = device.Read<double>();

        // Assert
        Assert.AreEqual(1.234, value);
    }

    [TestMethod]
    public void Read_T_Int_ParsesIntegerResponse()
    {
        // Arrange
        var fake = new FakeGpibLowLevel();
        fake.SetReadResponse("42\n");
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act
        int value = device.Read<int>();

        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    [DataRow("1",  true)]
    [DataRow("0",  false)]
    [DataRow("ON", true)]
    [DataRow("OFF", false)]
    [DataRow("TRUE", true)]
    [DataRow("FALSE", false)]
    [DataRow(" on \n", true)]   // with whitespace
    [DataRow(" off\r", false)]
    public void Read_T_Bool_ParsesScpiBool(string response, bool expected)
    {
        // Arrange
        var fake = new FakeGpibLowLevel();
        fake.SetReadResponse(response);
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act
        bool value = device.Read<bool>();

        // Assert
        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    [DataRow("MAYBE")]
    [DataRow("2")]
    [DataRow("")]
    [DataRow(" YES ")]
    public void Read_T_Bool_InvalidScpiBool_ThrowsFormatException(string response)
    {
        // Arrange
        var fake = new FakeGpibLowLevel();
        fake.SetReadResponse(response);
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act + Assert
        Assert.Throws<FormatException>(() => device.Read<bool>());
    }

    [TestMethod]
    public void Query_T_Double_UsesConvertTo()
    {
        // Arrange
        var fake = new FakeGpibLowLevel();
        fake.SetReadResponse("3.14\n");
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act
        double value = device.Query<double>("MEAS:VOLT?");

        // Assert: numeric parse correct
        Assert.AreEqual(3.14, value);

        // And optionally assert the command was written:
        var written = Encoding.ASCII.GetString(fake.LastWrite!);
        Assert.Contains("MEAS:VOLT?", written);
    }

    [TestMethod]
    public async Task QueryAsync_ThrowsTimeoutException_When_MessageNeverAvailable()
    {
        // Arrange
        var fake = new FakeGpibLowLevel();
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await device.QueryAsync<double>("MEAS:VOLT?", timeoutMs: 1));
    }

    [TestMethod]
    public async Task Query_Async_T_Double_UsesConvertTo()
    {
        // Arrange
        var fake = new FakeGpibLowLevel() { SerialPollStatusByte = GpibDevice.MAV};
        fake.SetReadResponse("3.14\n");
        var device = new GpibDevice(
            boardIndex: 0,
            primaryAddress: 5,
            secondaryAddress: 0,
            timeout: GpibTimeout.T10s,
            eot: 1,
            eos: 0,
            lowLevel: fake);

        // Act
        double value = await device.QueryAsync<double>("MEAS:VOLT?");

        // Assert: numeric parse correct
        Assert.AreEqual(3.14, value);

        // And optionally assert the command was written:
        var written = Encoding.ASCII.GetString(fake.LastWrite!);
        Assert.Contains("MEAS:VOLT?", written);
    }

    [TestMethod]
    public void GpibDevice_StatusBits_Are_Ieee488_Conformant()
    {
        #pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual(0x10, GpibDevice.MAV, "MAV bit must be 0x10 (bit 4).");
        Assert.AreEqual(0x40, GpibDevice.RQS, "RQS bit must be 0x40 (bit 6).");
        Assert.AreEqual(1 << 13, GpibDevice.END, "END (EOI/EOS) must be bit 13 in ibsta.");
        Assert.AreEqual(1 << 14, GpibLowLevelExtensions.TIMO, "TIMO bit must be bit 14 in ibsta.");
        Assert.AreEqual(1 << 15, GpibLowLevelExtensions.ERR, "ERR bit must be bit 15 in ibsta.");
        #pragma warning restore MSTEST0032 // Assertion condition is always true
    }
}   
