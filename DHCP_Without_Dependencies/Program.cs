using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net;

namespace DHCP_Without_Dependencies
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IPAddress listeningIpAddress = IPAddress.Parse("192.168.0.3");

            Random random = new Random();
            // supported fomats by PhysicalAddress.Parse is 
            // 12-34-56-78-90-12
            // 12:34:56:78:90:12
            // 123456789012
            // otherwise it will give the exception
            var staticReservation1 = new ConcurrentDictionary<PhysicalAddress, IPAddress>();
            staticReservation1.TryAdd(PhysicalAddress.Parse("123456789012"), IPAddress.Parse("192.168.0.190"));
            var staticReservation2 = new ConcurrentDictionary<PhysicalAddress, IPAddress>();
            staticReservation2.TryAdd(PhysicalAddress.Parse("2087561B8920"), IPAddress.Parse("192.168.2.156"));

            // Need to give the all subnets's details to create the pool
            List<SubnetPool> subnetPool = new List<SubnetPool>()
            {
                new SubnetPool(1, startIpAddress: IPAddress.Parse("192.168.0.190"),
                               endIpAddress: IPAddress.Parse("192.168.0.199"),
                               subnetMask: IPAddress.Parse("255.255.255.0"),
                               IPAddress.Parse("192.168.0.0")),
                new SubnetPool(2, startIpAddress: IPAddress.Parse("192.168.2.150"),
                               endIpAddress: IPAddress.Parse("192.168.2.160"),
                               subnetMask: IPAddress.Parse("255.255.255.0"),
                               IPAddress.Parse("192.168.2.0"),
                               staticReservation2),
            };

            DhcpServerImplementation dhcpServer = new DhcpServerImplementation(
                listeningIpAddress,
                subnetPool,
                random
            );

            Thread serverThread = new Thread(() => {
                try
                {
                    dhcpServer.Start();
                    Console.WriteLine("DHCP Server is running...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting DHCP server: {ex.Message}");
                }
            });

            serverThread.IsBackground = true;
            serverThread.Start();

            // Keep the main thread running to prevent the application from exiting
            Console.WriteLine("Press Enter to stop the server...");
            Console.ReadLine();

            // Clean up
            dhcpServer.Stop(); // Ensure the server is properly stopped
            serverThread.Join(); // Wait for the server thread to finish
        }
    }
}
 