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

/// <summary>
/// Represents errors that occur during GPIB bus operations (hardware/protocol errors).
/// Inherits from IOException for compatibility with common I/O error handling.
/// </summary>
public class GpibException : IOException
{
    public readonly int ErrorCode;
    public readonly int Status;
    public readonly string? Operation;
    public readonly string? Command;

    public GpibException(string message, string operation, string command, int status, int errorCode)
        : base(message, errorCode)
    {
        Status = status;
        ErrorCode = errorCode;
        Operation = operation;
        Command = command;
    }

    public GpibException(string message) : base(message)
    {
        Status = -1;
        ErrorCode = -1; 
    }
}