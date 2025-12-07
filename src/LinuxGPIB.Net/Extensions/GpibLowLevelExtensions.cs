using System.Text;
using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net;

internal static class GpibLowLevelExtensions
{
    // Constants for Status Bitmask (ibsta)
    internal const int ERR = 1 << 15; // Error detected
    internal const int TIMO = 1 << 14; // Timeout

    internal static void Validate(this IGpibLowLevel lowLevel, string operation, ReadOnlySpan<byte> command = default)
    {
        int status = lowLevel.ThreadIbsta();

        if ((status & TIMO) != 0)
        {
            int error = lowLevel.ThreadIberr();
            throw new GpibException(
                $"The '{operation}' operation timed out.",
                operation,
                Encoding.ASCII.GetString(command),
                status,
                error);
        }

        if ((status & ERR) != 0)
        {
            int error = lowLevel.ThreadIberr();
            string message = lowLevel.GpibErrorString(error);
            throw new GpibException(
                message, 
                operation, 
                Encoding.ASCII.GetString(command), 
                status, 
                error);
        }
    }
}