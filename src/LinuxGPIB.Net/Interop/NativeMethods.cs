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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace LinuxGPIB.Net.Interop;

[ExcludeFromCodeCoverage]
internal static partial class NativeMethods
{
    private const string LibName = "libgpib.so.0";

    /// <summary>
    /// Critical check: Ensures this library is only used on supported platforms (Linux).
    /// Prevents confusing DllNotFoundExceptions on Windows/macOS.
    /// </summary>
    static NativeMethods()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new PlatformNotSupportedException($"This library only supports Linux (requires {LibName}).");
        }
    }

    // The simplest function to check if the library loads
    // ibdev opens a device and returns a descriptor (int)
    [LibraryImport(LibName, EntryPoint = "ibdev")]
    internal static partial int IbDev(int board_index, int pad, int sad, int tmo, int eot, int eos);

    [LibraryImport(LibName, EntryPoint = "ibonl")]
    internal static partial int IbOnl(int ud, int v);

    // --- IO Operations ---

    // Write: We use ReadOnlySpan<byte> (mapped to 'ref byte') for zero-copy writes
    [LibraryImport(LibName, EntryPoint = "ibwrt")]
    internal static partial int IbWrt(int ud, ref byte buf, nint count);

    // Read: We use 'ref byte' to write directly into stack/array memory
    [LibraryImport(LibName, EntryPoint = "ibrd")]
    internal static partial int IbRd(int ud, ref byte buf, nint count);

    [LibraryImport(LibName, EntryPoint = "ibclr")]
    internal static partial int IbClr(int ud);

    [LibraryImport(LibName, EntryPoint = "ibrsp")]
    internal static partial int IbRsp(int ud, out byte result);

    // --- Status / Error Handling (Thread Safe) ---
    // Crucial: strictly use these instead of trying to read global variables

    [LibraryImport(LibName, EntryPoint = "ThreadIbsta")]
    internal static partial int ThreadIbsta();

    [LibraryImport(LibName, EntryPoint = "ThreadIberr")]
    internal static partial int ThreadIberr();

    [LibraryImport(LibName, EntryPoint = "ThreadIbcnt")]
    internal static partial int ThreadIbcnt();

    [LibraryImport(LibName, EntryPoint = "gpib_error_string", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial string GpibErrorString(int error);

    // --- Bus Operations ---
    [LibraryImport(LibName, EntryPoint = "FindLstn")]
    internal static partial void FindLstn(int boardDescriptor, [In] ushort[] padList, [Out] ushort[] resultList, int maxNumResults);

    // --- Library functions ---
    [LibraryImport(LibName, EntryPoint = "ibvers")]
    internal static partial void IbVers(out IntPtr version);
}