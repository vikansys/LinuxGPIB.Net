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
namespace LinuxGPIB.Net;

public enum GpibLineEnding
{
    None,
    /// <summary>\n</summary>
    Lf,
    /// <summary>\r</summary>
    Cr,
    /// <summary>\r\n</summary>
    CrLf
}

public enum GpibTimeout
{
    /// <summary>Timeout disabled.</summary>
    None = 0,

    /// <summary>10 Microseconds (us).</summary>
    T10us = 1,

    /// <summary>30 Microseconds (us).</summary>
    T30us = 2,

    /// <summary>100 Microseconds (us).</summary>
    T100us = 3,

    /// <summary>300 Microseconds (us).</summary>
    T300us = 4,

    /// <summary>1 Millisecond (ms).</summary>
    T1ms = 5,

    /// <summary>3 Milliseconds (ms).</summary>
    T3ms = 6,

    /// <summary>10 Milliseconds (ms).</summary>
    T10ms = 7,

    /// <summary>30 Milliseconds (ms).</summary>
    T30ms = 8,

    /// <summary>100 Milliseconds (ms).</summary>
    T100ms = 9,

    /// <summary>300 Milliseconds (ms).</summary>
    T300ms = 10,

    /// <summary>1 Second (s).</summary>
    T1s = 11,

    /// <summary>3 Seconds (s).</summary>
    T3s = 12,

    /// <summary>10 Seconds (s). (Default used by many drivers)</summary>
    T10s = 13,

    /// <summary>30 Seconds (s).</summary>
    T30s = 14,

    /// <summary>100 Seconds (s).</summary>
    T100s = 15
}