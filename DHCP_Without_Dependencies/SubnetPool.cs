using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{ 
    public class SubnetPool
    {
        public IPAddress BaseNetWorkAddress { get; set; }
        public IPAddress StartIp { get; }
        public IPAddress EndIp { get; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress Gateway { get; set; }
        public uint PoolID { get; set; }

        public ConcurrentDictionary<PhysicalAddress, IPAddress>? AssignedIpAddress { get; } = new();
        public ConcurrentDictionary<PhysicalAddress, IPAddress>? StaticIpReservation { get; } = new();

        public SubnetPool(uint poolID,
                          IPAddress startIpAddress,
                          IPAddress endIpAddress,
                          IPAddress subnetMask,
                          IPAddress baseNetWorkAddress,
                          ConcurrentDictionary<PhysicalAddress, IPAddress>? staticReservation = null)
        {
            PoolID = poolID;
            BaseNetWorkAddress = baseNetWorkAddress;
            StartIp = startIpAddress;
            EndIp = endIpAddress;
            SubnetMask = subnetMask;
            StaticIpReservation = staticReservation;
        }

        //public void SetStaticReservations(string macAddress, IPAddress ipAddressToMakeStatic)
        //{
        //    if (IsValidIpAddress(ipAddressToMakeStatic) && IsValidMacAddress(macAddress))
        //    {

        //    }
        //}
        //private bool IsValidMacAddress(string macAddress)
        //{
        //    return !string.IsNullOrEmpty(macAddress) && macAddress.Length == 12 && macAddress.All(char.IsLetterOrDigit);
        //}

        //private bool IsValidIpAddress(IPAddress ipAddress)
        //{
        //    return ipAddress != IPAddress.Any && ipAddress != IPAddress.Loopback && ipAddress != IPAddress.Broadcast;
        //}

        //public void ClearStaticReservations()
        //{
        //    Console.WriteLine("Static Reservations are Cleared");
        //    StaticIpReservation.Clear();
        //}
        public bool IsInRange(IPAddress address)
        {
            var addressBytes = address.GetAddressBytes();
            var startBytes = StartIp.GetAddressBytes();
            var endBytes = EndIp.GetAddressBytes();

            for (int i = 0; i < 4; i++)
            {
                if (addressBytes[i] < startBytes[i] || addressBytes[i] > endBytes[i])
                    return false;
            }

            return true;
        }
        public IPAddress GenerateRandomIpAddress(Random random)
        {
            byte[] startBytes = StartIp.GetAddressBytes();
            byte[] endBytes = EndIp.GetAddressBytes();
            byte[] randomBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                randomBytes[i] = (byte)random.Next(startBytes[i], endBytes[i] + 1);
            }
            return new IPAddress(randomBytes);
        }

        public bool IsInSubnetRange(IPAddress address)
        {
            byte[] addressBytes = address.GetAddressBytes();
            byte[] subnetBytes = SubnetMask.GetAddressBytes();
            byte[] baseAddressBytes = BaseNetWorkAddress.GetAddressBytes();

            if (addressBytes.Length == 4)
            {
                if ((IPNumber(addressBytes) & IPNumber(subnetBytes)) != (IPNumber(baseAddressBytes) & IPNumber(subnetBytes)))
                {
                    return false;
                }
            }
            return true;
        }

        public int IPNumber(byte[] bytes)
        {
            if (bytes is not null || bytes.Length == 4)
            {
                return (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            }
            return 0;
        }

        //public bool IsInSubnetRange(IPAddress ipAddress)
        //{
        //    byte[] startBytes = ipAddress.GetAddressBytes();
        //    byte[] networkBytes = BaseNetWorkAddress.GetAddressBytes();
        //    byte[] subnetMaskBytes = SubnetMask.GetAddressBytes();

        //    for (int i = 0; i < subnetMaskBytes.Length; i++) {
        //        if ((startBytes[i] & subnetMaskBytes[i]) != (networkBytes[i] & subnetMaskBytes[i])) {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        public bool IsIpAvailableInReservation(IPAddress ipAddress)
        {
            if (StaticIpReservation is { Count: > 0 })
            {
                return StaticIpReservation.Values.Contains(ipAddress);
            }
            return false;
        }

    }
}
