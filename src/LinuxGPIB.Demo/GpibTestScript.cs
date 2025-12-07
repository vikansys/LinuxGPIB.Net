using System;
using System.IO;
using System.Threading.Tasks;

// Assuming the GpibDevice class and enums are available in this namespace
using LinuxGPIB.Net; 

public class GpibTestScript
{
    // Desired Test Parameters
    private const double DesiredFrequencyHz = 1000.0;
    private const double DesiredVoltageVpp = 2.0;
    
    // Acceptable tolerance for validation
    private const double Tolerance = 0.05; // 5%

    public static async Task RunAsync(int generatorAddress, int oscilloscopeAddress)
    {
        Console.WriteLine("--- GPIB Test Setup Verification ---");

        // The 'using' blocks ensure the Dispose() method is called to gracefully close the GPIB connection.
        try
        {
            // 1. Initialize Instruments
            // Note: T10s timeout is a safe default for synchronous commands
            using var generator = new GpibDevice(0, generatorAddress, timeout: GpibTimeout.T10s);
            using var oscilloscope = new GpibDevice(0, oscilloscopeAddress, timeout: GpibTimeout.T10s);

            Console.WriteLine($"\nInitialized Generator (Addr {generatorAddress}) and Oscilloscope (Addr {oscilloscopeAddress}).");

            // 2. Perform Identification and Clearing
            // We use synchronous Query() for simple identification
            string genId = generator.Query("*IDN?");
            string scopeId = oscilloscope.Query("*IDN?");
            
            Console.WriteLine($"Generator ID: {genId.Trim()}");
            Console.WriteLine($"Oscilloscope ID: {scopeId.Trim()}");

            // Clear the devices to reset their state before the test
            generator.Clear();
            oscilloscope.Clear();
            Console.WriteLine("Instruments cleared and ready.");

            // 3. Configure the Signal Generator (HP 33120A)
            Console.WriteLine($"\n--- Configuring Signal ---");
            string setCommand = $":APPLY:SIN {DesiredFrequencyHz}, {DesiredVoltageVpp}VPP";
            generator.Write(setCommand);
            Console.WriteLine($"Sent command: '{setCommand}' to Generator.");
            
            // Wait for the instrument to complete the operation (important for slow settings)
            generator.Write("*OPC"); // Operation Complete
            await generator.WaitForServiceRequestAsync();
            Console.WriteLine("Generator setup complete.");


            // 4. Configure and Query the Oscilloscope (HP 54600B)
            Console.WriteLine("\n--- Measuring Signal with Oscilloscope ---");
            
            // Set up channel 1 for measurement (assuming channel 1 is connected)
            oscilloscope.Write(":MEASURE:SOURCE CH1");
            
            // Query Voltage Peak-to-Peak
            string vppResult = oscilloscope.Query(":MEASURE:VPP?");
            double measuredVpp = double.Parse(vppResult.Trim());

            // Query Frequency
            string freqResult = oscilloscope.Query(":MEASURE:FREQ?");
            double measuredFreq = double.Parse(freqResult.Trim());

            Console.WriteLine($"Measured VPP: {measuredVpp:F3} V");
            Console.WriteLine($"Measured Freq: {measuredFreq:F3} Hz");

            
            // 5. Validation
            Console.WriteLine("\n--- Validation ---");
            bool vppPass = Math.Abs(measuredVpp - DesiredVoltageVpp) <= DesiredVoltageVpp * Tolerance;
            bool freqPass = Math.Abs(measuredFreq - DesiredFrequencyHz) <= DesiredFrequencyHz * Tolerance;
            
            if (vppPass && freqPass)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("TEST PASSED: Measured values are within tolerance.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TEST FAILED: Measured values deviated from the set point.");
            }
            Console.ResetColor();
        }
        catch (GpibException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[GPIB HARDWARE ERROR] Test Aborted: {ex.Message}");
            Console.WriteLine($"Status: 0x{ex.Status:X}, ErrorCode: {ex.ErrorCode}");
            Console.ResetColor();
        }
        catch (TimeoutException ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[TIMEOUT ERROR] Test Aborted: {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[GENERIC ERROR] Test Aborted: {ex.Message}");
            Console.ResetColor();
        }
    }
}