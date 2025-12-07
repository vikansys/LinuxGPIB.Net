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

public interface IGpibBus
{
    /// <summary>
    /// Gets the board descriptor (unit descriptor) used to identify
    /// the underlying GPIB controller.
    /// </summary>
    int BoardIndex { get; }

    /// <summary>
    /// Discovers GPIB devices that are currently listening on the bus
    /// within the specified range of primary addresses.
    /// </summary>
    /// <param name="minPrimary">
    /// The lowest primary address to probe (inclusive). Must be between 0 and 30.
    /// </param>
    /// <param name="maxPrimary">
    /// The highest primary address to probe (inclusive). Must be between 0 and 30
    /// and greater than or equal to <paramref name="minPrimary"/>.
    /// </param>
    /// <returns>
    /// A list of <see cref="GpibAddress"/> instances representing devices that
    /// responded on the bus.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the address range is invalid (less than 0, greater than 30,
    /// or <paramref name="minPrimary"/> is greater than <paramref name="maxPrimary"/>).
    /// </exception>
    IReadOnlyList<GpibAddress> DiscoverDevices(int minPrimary = 0, int maxPrimary = 30);
}
