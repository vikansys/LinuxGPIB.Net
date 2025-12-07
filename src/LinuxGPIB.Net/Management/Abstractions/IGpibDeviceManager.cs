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
using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net.Management.Abstractions;

public interface IGpibDeviceManager : IDisposable
{
    /// <summary>
    /// Executes an asynchronous operation on the specified GPIB device
    /// and returns a result.  
    /// 
    /// This method ensures exclusive access to the device for the  
    /// duration of the operation. If other callers attempt to access  
    /// the same device concurrently, they are queued until the device  
    /// becomes available.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the value returned by the operation.
    /// </typeparam>
    /// <param name="address">
    /// The GPIB address of the device to operate on.
    /// </param>
    /// <param name="action">
    /// A function that performs the device operation and returns a result.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel waiting for access to the device.
    /// </param>
    /// <returns>
    /// A task that completes with the result returned by <paramref name="action"/>.
    /// </returns>
    Task<TResult> ExecuteAsync<TResult>(
        GpibAddress address,
        Func<IGpibDevice, Task<TResult>> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous operation on the specified GPIB device  
    /// without returning a result.  
    /// 
    /// This method provides exclusive access to the device for the  
    /// duration of the operation. Concurrent callers targeting the  
    /// same device are serialized, while callers targeting different  
    /// devices may run in parallel.
    /// </summary>
    /// <param name="address">
    /// The GPIB address of the device to operate on.
    /// </param>
    /// <param name="action">
    /// A function that performs the device operation.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel waiting for access to the device.
    /// </param>
    /// <returns>
    /// A task that completes once the operation has finished.
    /// </returns>
    Task ExecuteAsync(
        GpibAddress address,
        Func<IGpibDevice, Task> action,
        CancellationToken cancellationToken = default);
}
