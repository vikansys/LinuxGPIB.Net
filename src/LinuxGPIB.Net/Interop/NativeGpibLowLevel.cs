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
using LinuxGPIB.Net.Abstractions;

namespace LinuxGPIB.Net.Interop;

[ExcludeFromCodeCoverage]
internal sealed class NativeGpibLowLevel : IGpibLowLevel
{
    public int IbDev(int boardIndex, int pad, int sad, int tmo, int eot, int eos)
        => NativeMethods.IbDev(boardIndex, pad, sad, tmo, eot, eos);

    public int IbOnl(int ud, int v)
        => NativeMethods.IbOnl(ud, v);

    public int IbWrt(int ud, ReadOnlySpan<byte> data)
        => NativeMethods.IbWrt(ud, ref MemoryMarshal.GetReference(data), data.Length);

    public int IbRd(int ud, Span<byte> buffer)
    {
        int status = NativeMethods.IbRd(ud, ref MemoryMarshal.GetReference(buffer), buffer.Length);
        return status;
    }

    public int IbClr(int ud) => NativeMethods.IbClr(ud);
    public int IbRsp(int ud)
    {
        _ = NativeMethods.IbRsp(ud, out byte stb);
        return stb;
    }
    public int ThreadIbsta() => NativeMethods.ThreadIbsta();
    public int ThreadIberr() => NativeMethods.ThreadIberr();
    public int ThreadIbcnt() => NativeMethods.ThreadIbcnt();
    public string GpibErrorString(int error) => NativeMethods.GpibErrorString(error);
    public void FindLstn(int boardIndex, ushort[] padList, ushort[] resultList, int maxNumResults) 
    => NativeMethods.FindLstn(boardIndex, padList, resultList, maxNumResults);
    public static string IbVers()
    {
        NativeMethods.IbVers(out var versionPtr);
        return Marshal.PtrToStringAnsi(versionPtr) ?? string.Empty;
    }
}
