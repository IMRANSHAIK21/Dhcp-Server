using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace DHCP_Without_Dependencies
{
    public class DhcpServerImplementation : DhcpServer
    {
        private readonly List<SubnetPool> _subnetPool;
        private readonly IPAddress listeningIpAddress;
        private readonly Random _random = new Random();

        private ConcurrentDictionary<string, uint> CircutIdDictionary = new ConcurrentDictionary<string, uint>()
        {
            ["Vlan1"] = 1,
            ["Vlan2"] = 2

        };
        private ConcurrentDictionary<string, uint> RemoteIdDictionary = new ConcurrentDictionary<string, uint>()
        {
            ["d4-f5-27-63-b8-b3"] = 2,
            ["Vlan2"] = 2

        };

        public DhcpServerImplementation(IPAddress listeningIpAddress, List<SubnetPool> subnetPool, Random random)
            : base(listeningIpAddress, subnetPool.FirstOrDefault().SubnetMask)
        {
            this.listeningIpAddress = listeningIpAddress;
            _subnetPool = subnetPool;
        }


        protected override DhcpDiscoverResult? OnDiscoverReceived(DhcpMessage message)
        {
            Console.WriteLine($"DHCP Discover received from {message.ClientMacAddress}");
            RelayAgentInformationData relayAgentInfo = null;

            // To Handle the Option 82 And to parse the data
            if (message is not null && message.Options is not null)
            {
                byte[] relayAgentInfoBytes = null;
                var realy = message.Options.ContainsKey(DhcpOption.RelayAgentInformation) ? message.Options.TryGetValue(DhcpOption.RelayAgentInformation, out relayAgentInfoBytes) : false;
                if (realy && relayAgentInfoBytes is not null)
                {
                    relayAgentInfo = ParseRealyAgentInformation(relayAgentInfoBytes);
                }

            }
            SubnetPool subnet = null;
            if (relayAgentInfo is not null)
            {
                subnet = GetSubnetOfMessageBasedOnRelayAgent(relayAgentInfo, message);
            }
            else
            {
                subnet = FetchSubnetPool(message);
            }

            if (subnet == null)
            {
                Console.WriteLine("Provided subnet is not found");
                return null;
            }
            if (subnet.StaticIpReservation is { Count: > 0 } && subnet.StaticIpReservation.TryGetValue(message.ClientMacAddress, out var staticIp))
            {
                return DhcpDiscoverResult.CreateOffer(message, staticIp, (uint)TimeSpan.FromSeconds(60).TotalSeconds);
            }
            // Check if the client previously has an assigned IP
            if (subnet.AssignedIpAddress is { Count: > 0 } && subnet.AssignedIpAddress is not null && subnet.AssignedIpAddress.TryGetValue(message.ClientMacAddress,
                                                                                                                                           out var assignedAddress))
            {
                return DhcpDiscoverResult.CreateOffer(message, assignedAddress, (uint)TimeSpan.FromSeconds(60).TotalSeconds);
            }

            IPAddress? newAddress = GetAvailableIpAddress(subnet);
            if (newAddress == null)
            {
                OnMessageError(new Exception(" IP addresses are not available."));
                return null;
            }

            subnet.AssignedIpAddress[message.ClientMacAddress] = newAddress;

            return DhcpDiscoverResult.CreateOffer(message, newAddress, (uint)TimeSpan.FromSeconds(60).TotalSeconds);
        }

        private RelayAgentInformationData ParseRealyAgentInformation(byte[] realyAgentOptionData)
        {
            var realyAgentInfo = new RelayAgentInformationData();
            var subOptions = ParseSubOptions(realyAgentOptionData);
            foreach (var subOption in subOptions)
            {
                switch (subOption.Type)
                {
                    case DhcpOptionRelayAgentSubOptionType.CircuitId:
                        realyAgentInfo.CircuitId = Encoding.ASCII.GetString(subOption.Value);
                        break;
                    case DhcpOptionRelayAgentSubOptionType.RemoteId:
                        realyAgentInfo.RemoteId = Encoding.ASCII.GetString(subOption.Value);
                        break;
                }
            }
            return realyAgentInfo;
        }
        private List<DhcpOptionSubOptions> ParseSubOptions(byte[] options82Data)
        {
            List<DhcpOptionSubOptions> dh = new();
            int i = 0;
            while (i < options82Data.Length)
            {
                var subOptionType = (DhcpOptionRelayAgentSubOptionType)options82Data[i];
                int length = options82Data[i + 1];
                byte[] value = options82Data.Skip(i + 2).Take(length).ToArray();
                dh.Add(new DhcpOptionSubOptions(subOptionType, value));
                i += 2 + length;

            }
            return dh;
        }


        protected override DhcpRequestResult? OnRequestReceived(DhcpMessage message)
        {
            Console.WriteLine($"DHCP Request received from {message.ClientMacAddress}");
            var subnet = FetchSubnetPool(message);
            if (subnet == null)
            {
                Console.WriteLine("Provided subnet is not found");
                return null;
            }
            if (subnet.StaticIpReservation is { Count: > 0 } && subnet.StaticIpReservation.TryGetValue(message.ClientMacAddress, out var staticIp))
            {
                return DhcpRequestResult.CreateAcknowledgement(message, staticIp, (uint)TimeSpan.FromSeconds(60).TotalSeconds);
            }
            if (subnet.AssignedIpAddress is not null && subnet.AssignedIpAddress.TryGetValue(message.ClientMacAddress, out var assignedAddress))
            {
                return DhcpRequestResult.CreateAcknowledgement(message, assignedAddress, (uint)TimeSpan.FromSeconds(60).TotalSeconds);
            }            // for static ip reservation

            else
            {
                /*return DhcpRequestResult.CreateNoAcknowledgement(message,
                                                "Not assined any IP address.");*/
                OnMessageError(new Exception("Client has not been assigned an IP address."));
            }

            return null;
        }

        private SubnetPool? GetSubnetOfMessageBasedOnRelayAgent(RelayAgentInformationData relay, DhcpMessage message)
        {
            uint poolID = 0;
            if (RemoteIdDictionary.TryGetValue(relay.RemoteId, out poolID))
            {
                foreach (var subnet in _subnetPool)
                {
                    if (subnet.PoolID.Equals(poolID))
                    {
                        return subnet;
                    }
                }
            }
            else if (CircutIdDictionary.TryGetValue(relay.CircuitId, out poolID))
            {
                foreach (var subnet in _subnetPool)
                {
                    if (subnet.PoolID.Equals(poolID))
                    {
                        return subnet;
                    }
                }
            }
            return null;

        }

        // to know which subnet the ip address belongs to.
        private SubnetPool? FetchSubnetPool(DhcpMessage message)
        {
            var addressToCheck = message.RelayAgentIPAddress.Equals(IPAddress.Any) ? listeningIpAddress : message.RelayAgentIPAddress;

            foreach (var subnet in _subnetPool)
            {
                if (subnet.IsInSubnetRange(addressToCheck))
                {
                    return subnet;
                }
            }

            //else {
            //    if (subnet.IsInSubnetRange(message.ClientIPAddress.Equals(IPAddress.Any) ? listeningIpAddress : message.ClientIPAddress)) {
            //        return subnet;
            //    }
            //}

            return null;
        }
        private IPAddress? GetAvailableIpAddress(SubnetPool subnet)
        {
            IPAddress randomAddress;
            do
            {
                randomAddress = subnet.GenerateRandomIpAddress(_random);
            } while (subnet.AssignedIpAddress.Values.Contains(randomAddress)
            || !subnet.IsInRange(randomAddress)
            || subnet.IsIpAvailableInReservation(randomAddress));

            return randomAddress;
        }
        private SubnetPool? FetchSubnetForIp(IPAddress ipaddress)
        {
            foreach (var subnet in _subnetPool)
            {
                if (subnet.IsInSubnetRange(ipaddress))
                {
                    return subnet;
                }
            }
            return null;
        }

        protected override void OnInformReceived(DhcpMessage message)
        {
            Console.WriteLine($"Dhcp comes to Inform function");
        }

        protected override void OnMessageError(Exception ex)
        {
            Console.WriteLine(" Message Error");
        }

        protected override void OnReleaseReceived(DhcpMessage message)
        {
            Console.WriteLine("Release Received");
        }

        protected override void OnResponseSent(DhcpMessage message)
        {
            Console.WriteLine($"DHCP Response sent to {message.ClientIPAddress}: {message.Options.MessageType}");
        }

        protected override void OnSocketError(SocketException ex)
        {
            Console.WriteLine("Soket Error");
        }

        protected override void OnDeclineReceived(DhcpMessage message)
        {
            Console.WriteLine($"DHCP decline message {message.ClientMacAddress}");
        }
    }
}
