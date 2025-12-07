using LinuxGPIB.Net;
using LinuxGPIB.Net.Management;
try
{
   Console.WriteLine(GpibBus.Version);
   var bus = new GpibBus();
   var addresses = bus.DiscoverDevices();
   var gpibManager = new GpibDeviceManager();

   foreach (var address in addresses)
   {
      await gpibManager.ExecuteAsync(address, async dev =>
      {
         dev.Clear();
         var idn = dev.Query("*IDN?");
         Console.WriteLine($"{address.Primary}: {idn}");
      });
   }
}
catch (Exception ex)
{
   Console.WriteLine($"CRASH: {ex}");
}