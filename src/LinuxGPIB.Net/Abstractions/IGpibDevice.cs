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
namespace LinuxGPIB.Net.Abstractions;

public interface IGpibDevice : IDisposable
{
    /// <summary>
    /// Gets or sets the default line ending (termination sequence) to append to write operations.
    /// </summary>
    GpibLineEnding DefaultLineEnding { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether the instrument has a message available in its output buffer.
    /// </summary>
    bool IsMessageAvailable { get; }
    
    /// <summary>
    /// Gets a value indicating whether the instrument is requesting service (RQS bit is set).
    /// </summary>
    bool IsServiceRequestAsserted { get; }

    /// <summary>
    /// Sends a GPIB Device Clear (DCL) to the instrument.  
    /// This stops any ongoing I/O on the device and resets its interface state.
    /// </summary>
    void Clear();

    
    /// <summary>
    /// Reads a response into a rented buffer and returns it as an ASCII string,
    /// trimming trailing CR/LF and null characters.
    /// </summary>
    string Read();
    
    /// <summary>
    /// Reads data into the provided buffer. Returns number of bytes read.
    /// </summary>
    int ReadBytes(Span<byte> buffer);
    
    /// <summary>
    /// Performs a Serial Poll of the device and returns the 8-bit Status Byte Register (STB).
    /// This is primarily used to check if the Message Available (MAV) bit is set after a long operation.
    /// </summary>
    /// <returns>The 8-bit Status Byte (STB) as an integer.</returns>
    int SerialPoll();
    
    /// <summary>
    /// Asynchronously waits for a message to become available on the instrument's output buffer 
    /// by continuously checking the <see cref="IsMessageAvailable"/> property.
    /// </summary>
    /// <param name="pollIntervalMs">The delay (in milliseconds) between each poll request. Defaults to 50ms.</param>
    /// <param name="timeoutMs">The maximum time (in milliseconds) to wait for the message.</param>
    /// <exception cref="TimeoutException">Thrown if the message does not become available within the timeout period.</exception>
    Task WaitForMessageAsync(CancellationToken cancellationToken = default, int pollIntervalMs = 50, int timeoutMs = 15000);
    
    /// <summary>
    /// Asynchronously waits for the instrument to assert a Service Request (SRQ) by continuously checking 
    /// the <see cref="IsServiceRequestAsserted"/> property.
    /// </summary>
    /// <param name="pollIntervalMs">The delay (in milliseconds) between each poll request. Defaults to 50ms.</param>
    /// <param name="timeoutMs">The maximum time (in milliseconds) to wait for the SRQ.</param>
    /// <exception cref="TimeoutException">Thrown if the SRQ is not asserted within the timeout period.</exception>
    Task WaitForServiceRequestAsync(CancellationToken cancellationToken = default, int pollIntervalMs = 50, int timeoutMs = 15000);

    /// <summary>
    /// Writes an ASCII command string to the bus, appending the configured line ending.
    /// </summary>
    void Write(string command);

    /// <summary>
    /// Writes raw bytes to the bus.
    /// </summary>
    void WriteBytes(ReadOnlySpan<byte> data);
}
