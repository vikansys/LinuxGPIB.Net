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

public interface IGpibLowLevel
{
    int IbDev(int boardIndex, int pad, int sad, int tmo, int eot, int eos);
    int IbOnl(int ud, int v);
    int IbWrt(int ud, ReadOnlySpan<byte> data);
    int IbRd(int ud, Span<byte> buffer);
    int IbClr(int ud);
    int IbRsp(int ud);
    int ThreadIbsta();
    int ThreadIberr();
    int ThreadIbcnt();
    string GpibErrorString(int error);
    void FindLstn(int boardIndex, ushort[] padList, ushort[] resultList, int maxNumResults);
}
