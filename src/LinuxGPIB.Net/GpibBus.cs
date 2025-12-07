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
using System.Buffers;
using LinuxGPIB.Net.Abstractions;
using LinuxGPIB.Net.Interop;

namespace LinuxGPIB.Net;

public sealed class GpibBus : IGpibBus
{
    private static readonly Lazy<string> _version =
        new(static () => NativeGpibLowLevel.IbVers());
    private readonly IGpibLowLevel _lowLevel;
    private const ushort NOADDR = 0xFFFF;
    private int _boardIndex;

    /// <summary>
    /// Gets the version string reported by the underlying linux-gpib library.
    /// </summary>
    public static string Version => _version.Value;

    public int BoardIndex => _boardIndex;


    /// <summary>
    /// Initializes a new instance of the <see cref="GpibBus"/> class
    /// for the specified controller board.
    /// </summary>
    /// <param name="boardIndex">
    /// The index of the GPIB controller board to use (typically 0 for the first board).
    /// </param>
    public GpibBus(int boardIndex = 0)
        : this(boardIndex, new NativeGpibLowLevel()) { }
    internal GpibBus(int boardIndex, IGpibLowLevel lowLevel)
    {
        _lowLevel = lowLevel ?? throw new ArgumentNullException(nameof(lowLevel));
        _boardIndex = boardIndex;
    }

    public IReadOnlyList<GpibAddress> DiscoverDevices(int minPrimary = 1, int maxPrimary = 30)
    {
        if (minPrimary < 1 || minPrimary > 30)
            throw new ArgumentOutOfRangeException(nameof(minPrimary));

        if (maxPrimary < 1 || maxPrimary > 30)
            throw new ArgumentOutOfRangeException(nameof(maxPrimary));

        if (minPrimary > maxPrimary)
            throw new ArgumentException("minPrimary must be less than or equal to maxPrimary.");

        int countPads = maxPrimary - minPrimary + 1;

        var padList = new ushort[countPads + 1];
        for (int i = 0; i < countPads; i++)
            padList[i] = (ushort)(minPrimary + i);

        padList[countPads] = NOADDR; 

        int maxResults = countPads * 32;

        ushort[]? rentedResult = null;
        try
        {
            rentedResult = ArrayPool<ushort>.Shared.Rent(maxResults);

            _lowLevel.FindLstn(BoardIndex, padList, rentedResult, maxResults);

            int deviceCount = _lowLevel.ThreadIbcnt();

            deviceCount = Math.Min(deviceCount, maxResults);

            var devices = new List<GpibAddress>(deviceCount);

            const int bitMask = 0xFF;
            for (int i = 0; i < deviceCount; i++)
            {
                ushort raw = rentedResult[i];

                int pad = raw & bitMask;
                int sad = (raw >> 8) & bitMask;

                devices.Add(new GpibAddress(pad, sad));
            }

            return devices;
        }
        finally
        {
            if (rentedResult is not null)
                ArrayPool<ushort>.Shared.Return(rentedResult);
        }
    }
}