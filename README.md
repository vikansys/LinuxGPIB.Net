---

# LinuxGPIB.Net


[![NuGet Version](https://img.shields.io/nuget/v/LinuxGPIB.Net.svg)](https://www.nuget.org/packages/LinuxGPIB.Net/)
![Target Framework](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Linux-lightgrey)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3%2B-blue.svg)](LICENSE)


LinuxGPIB.Net is a modern .NET 8+ wrapper for the linux-gpib 4.3.7 C library, providing safe, high-level abstractions for communicating with GPIB instruments on Linux.

* High-level API (no P/Invoke required)
* Thread-safe device coordination via `GpibDeviceManager`
* Synchronous and asynchronous operations
* Tested with linux-gpib 4.3.7
* Licensed under GPLv3 or later

---

## Installation

### 1. Install the NuGet package

```bash
dotnet add package LinuxGPIB.Net
```

### 2. Install linux-gpib (version 4.3.7)

LinuxGPIB.Net requires the native linux-gpib library to be installed on the system.

A installation guide for linux-gpib 4.3.4 can be found here:
[http://elektronomikon.org/install.html](http://elektronomikon.org/install.html)

The guide is still applicable for linux-gpib 4.3.7. Replace the version-specific steps with the sequence below:

```bash
wget https://github.com/coolshou/linux-gpib/archive/refs/tags/4.3.7.tar.gz
tar -xzf 4.3.7.tar.gz

cd linux-gpib-4.3.7/linux-gpib-user/
./configure
make
make install
ldconfig

cd ../linux-gpib-kernel/
make
make install
```

Depending on your GPIB hardware, you may need to load the appropriate kernel module (e.g., `xyphro_ugc`, `gpib_bitbang`, etc.).

---

## Quick Start

Basic usage through the high-level `GpibDevice` class:

```csharp
using LinuxGPIB.Net;

var dev = new GpibDevice(primaryAddress: 5);

dev.Write("*IDN?");
string idn = dev.Read();

Console.WriteLine(idn);
```

### Query helper

```csharp
string idn = dev.Query("*IDN?");
```

---

## Configuration Example

```csharp
var dev = new GpibDevice(
    primaryAddress: 5,
    timeout: GpibTimeout.T3s,
    boardIndex: 0,
    eot: 1,
    eos: 0);
```

---

## Asynchronous Polling

```csharp
await dev.QueryAsync("*IDN?");              // Wait for MAV
await dev.WaitForMessageAsync();           // Wait for MAV
await dev.WaitForServiceRequestAsync();    // Wait for SRQ
```

---

## Thread-Safe Device Access

Use `GpibDeviceManager` to serialize calls per instrument and automatically dispose idle devices.

```csharp
using LinuxGPIB.Net;
using LinuxGPIB.Net.Management;

var manager = new GpibDeviceManager();
var address = new GpibAddress(5);

string idn = await manager.ExecuteAsync(address, async dev =>
{
    dev.Write("*IDN?");
    return dev.Read();
});
```

---

## Device Discovery

```csharp
var bus = new GpibBus();
var devices = bus.DiscoverDevices();
```

---

## Features

* High-level wrapper around linux-gpib
* Automatic cleanup of idle devices
* Read, write, and query support
* Serial polling (MAV and SRQ)
* Asynchronous wait operations
* Device discovery utilities

---

## Requirements

* Linux platform
* linux-gpib 4.3.7
* .NET 8.0+

---

## Tested hardware

This library has been tested on real hardware with:

- HP 54600B oscilloscope
- Raspberry Pi 3 Model B
- gpib4pi ([https://github.com/lightside-instruments/gpib4pi](https://github.com/lightside-instruments/gpib4pi))
- linux-gpib 4.3.7

Other GPIB instruments should work, but have not been explicitly tested.

---

## License

LinuxGPIB.Net is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

See the LICENSE file for details.

---
