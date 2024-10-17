using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

//// The DhcpDiscoverResult class is designed to handle the creation of a DHCP OFFER message in response to a DHCP DISCOVER message. 
//// It creates an offer with a specified IP address and lease time, copying necessary details from the original DISCOVER request. Some areas for improvement 
//// include enhanced validation, better option handling, error logging, and configurability.
namespace DHCP_Without_Dependencies
{
    /// This class contains the result of a DHCP Discover operation, including 
    /// the original DHCP Disvocer message, the offered IP address, and the lease time for the offer.
    public class DhcpDiscoverResult
    {
        /// <summary>
        /// Gets the source DHCP DISCOVER message that initiated the request.
        /// </summary>
        public DhcpMessage SourceMessage { get; }

        /// <summary>
        /// Gets the IP address being offered to the client.
        /// </summary>
        public IPAddress OfferedIPAddress { get; }

        /// <summary>
        /// Gets the lease time (in seconds) that the offer is valid.
        /// </summary>
        public uint LeaseSeconds { get; }

        /// this cannot be instantieate directly, it instatiate through Factory method patter which CreateOffer
        // need to check how this is essential
        private DhcpDiscoverResult(DhcpMessage sourceMessage, IPAddress offeredIPAddress, uint leaseSeconds)
        {
            if (sourceMessage.Options.MessageType != DhcpMessageType.Discover)
                throw new ArgumentException("Source message must be a Discover type.", nameof(sourceMessage));

            SourceMessage = sourceMessage;
            OfferedIPAddress = offeredIPAddress;
            LeaseSeconds = leaseSeconds;
        }

        /// <summary>
        /// Creates an offer based on a source request message.
        /// This will takes th original DHCp Discover message, OfferedIP and the lease time and pass it to private constructor.
        /// </summary>
        public static DhcpDiscoverResult CreateOffer(DhcpMessage sourceMessage, IPAddress offeredIPAddress, uint leaseSeconds)
        {
            // need to add the null checks if required
            return new DhcpDiscoverResult(sourceMessage, offeredIPAddress, leaseSeconds);
        }

        /// This create the new DHCP message with Replay and message type offer(indicating DHCP offer)
        public DhcpMessage CreateMessage()
        {
            // need to check the exceptions
            var response = new DhcpMessage(DhcpOpcode.BootReply, DhcpMessageType.Offer)
            {
                HardwareAddressType = SourceMessage.HardwareAddressType,
                HardwareAddressLength = SourceMessage.HardwareAddressLength,
                TransactionID = SourceMessage.TransactionID,
                YourClientIPAddress = OfferedIPAddress,
                Flags = SourceMessage.Flags,
                RelayAgentIPAddress = SourceMessage.RelayAgentIPAddress,
                ClientMacAddress = SourceMessage.ClientMacAddress,
            };
            response.Options.SetValue(DhcpOption.IPAddressLeaseTime, LeaseSeconds);

            // need to check the adding of subnet mask and the Router details also because these are available in the Options
            // need to check is this handled any where or not
            // response.Options.SetValue(DhcpOption.SubnetMask, subnetMask);                // Subnet mask option
            // response.Options.SetValue(DhcpOption.Router, gatewayIPAddress);              // Router (gateway) IP address option
            // response.Options.SetValue(DhcpOption.DomainNameServer, dnsServerAddresses);  // DNS servers option
            // response.Options.SetValue(DhcpOption.ServerIdentifier, serverIPAddress);     // Server identifier option
            // response.Options.SetValue(DhcpOption.RenewalTimeValue, renewalTimeSeconds);  // Renewal (T1) time option
            // response.Options.SetValue(DhcpOption.RebindingTimeValue, rebindingTimeSeconds); // Rebinding (T2) time option
            // response.Options.SetValue(DhcpOption.BroadcastAddress, broadcastAddress);    // Broadcast address option
            // response.Options.SetValue(DhcpOption.NetBIOSNameServer, netbiosNameServers); // NetBIOS name servers option

            return response;
        }
    }
}
