using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// This class  represents the result of handling a DHCP REQUEST message from a DHCP client. 
// The result could either be an ACKNOWLEDGE (ACK) or 
// a NO ACKNOWLEDGE (NAK) message, depending on whether the server can honor the request (like assigning the requested IP address).
namespace DHCP_Without_Dependencies
{
    // this class need to be checke why they are priventing from inheriting the class 
    public sealed class DhcpRequestResult
    {
        // This is the original DHCP Request Message from the client
        public DhcpMessage SourceMessage { get; }

        // The type of the message being sent as a result to the client
        public DhcpMessageType MessageType { get; }

        public IPAddress? AssignedIPAddress { get; }

        // This lease time is relevent only if the response wer are sending ACK message.
        public uint LeaseSeconds { get; }

        // Error message explaing why the request was not asknowledged. this is related to NAK message.
        public string? ErrorMessage { get; }

        // this is used when successfully acknowledges the DHCP request, and assings an IP, and specifies the lease time.

        private DhcpRequestResult(DhcpMessage sourceMessage, IPAddress assignedIPAddress, uint leaseSeconds)
        {
            //// these null checks i just added randomly, need to verify once
            if (sourceMessage == null)
                    throw new ArgumentNullException(nameof(sourceMessage));
            if (assignedIPAddress == null && MessageType == DhcpMessageType.Acknowledge)
                    throw new ArgumentNullException(nameof(assignedIPAddress));
            if(ValidateSourceMessage(sourceMessage)){
                Console.WriteLine("Framing Acknowledgement message");
            SourceMessage = sourceMessage;
            MessageType = DhcpMessageType.Acknowledge;

            AssignedIPAddress = assignedIPAddress;
            LeaseSeconds = leaseSeconds;
            }
        }

        // this is used when we are not acknowledge the DHCP request and provides the error message.
        private DhcpRequestResult(DhcpMessage sourceMessage, string errorMessage)
        {
            if(ValidateSourceMessage(sourceMessage)){
                Console.WriteLine("Framing No Acknowledgement message");
            SourceMessage = sourceMessage;
            MessageType = DhcpMessageType.NoAcknowledge;

            ErrorMessage = errorMessage;
            }
        }
        private bool ValidateSourceMessage(DhcpMessage sourceMessage){
            
            if (sourceMessage.Options.MessageType != DhcpMessageType.Request){
                Console.WriteLine("Source message must be a Request Type." + nameof(sourceMessage));
                return false;
            }
            return true;
        }
        //// creates an ACK result and invokes the private constructor that handles ACKs.
        public static DhcpRequestResult CreateAcknowledgement(DhcpMessage sourceMessage, IPAddress assignedIPAddress, uint leaseSeconds)
        {
            return new DhcpRequestResult(sourceMessage, assignedIPAddress, leaseSeconds);
        }

        ////  creates a NAK result and invokes the private constructor that handles NAKs.
        public static DhcpRequestResult CreateNoAcknowledgement(DhcpMessage sourceMessage, string errorMessage)
        {
            return new DhcpRequestResult(sourceMessage, errorMessage);
        }
        

        //// creates the actual DHCP message that will be sent in response to the client
        internal DhcpMessage CreateMessage()
        {
            //  [X] = handled here
            //  [S] = handled by DhcpServer

            //      Field      DHCPOFFER            DHCPACK              DHCPNAK
            //      -----      ---------            -------              -------

            //  [X] 'op'       BOOTREPLY            BOOTREPLY            BOOTREPLY
            //  [X] 'htype'    Copy                 Copy                 Copy
            //  [X] 'hlen'     Copy                 Copy                 Copy
            //  [ ] 'hops'     -                    -                    -
            //  [X] 'xid'      Copy                 Copy                 Copy
            //  [ ] 'secs'     -                    -                    -
            //  [X] 'ciaddr'   -                    Copy                 -
            //  [X] 'yiaddr'   IP address offered   IP address assigned  -
            //  [ ] 'siaddr'   -                    -                    -
            //  [X] 'flags'    Copy                 Copy                 Copy
            //  [X] 'giaddr'   Copy                 Copy                 Copy
            //  [X] 'chaddr'   Copy                 Copy                 Copy
            //  [ ] 'sname'    Server name/options  Server name/options  -
            //  [ ] 'file'     Boot file/options    Boot file/options     -
            //  [ ] 'options'  options              options

            //      Option                    DHCPOFFER    DHCPACK            DHCPNAK
            //      ------                    ---------    -------            -------
            //  [ ] Requested IP address      MUST NOT     MUST NOT           MUST NOT
            //  [X] IP address lease time     MUST         MUST (DHCPREQUEST) MUST NOT
            //                                             MUST NOT (DHCPINFORM)
            //  [ ] Use 'file'/'sname' fields MAY          MAY                MUST NOT
            //  [ ] DHCP message type         DHCPOFFER    DHCPACK            DHCPNAK
            //  [ ] Parameter request list    MUST NOT     MUST NOT           MUST NOT
            //  [ ] Message                   SHOULD       SHOULD             SHOULD
            //  [ ] Client identifier         MUST NOT     MUST NOT           MAY
            //  [ ] Vendor class identifier   MAY          MAY                MAY
            //  [S] Server identifier         MUST         MUST               MUST
            //  [ ] Maximum message size      MUST NOT     MUST NOT           MUST NOT
            //  [S] Subnet mask               MAY          MAY                MUST NOT
            //  [ ] All others                MAY          MAY                MUST NOT

            var response = new DhcpMessage(DhcpOpcode.BootReply, MessageType)
            {
                HardwareAddressType = SourceMessage.HardwareAddressType,
                HardwareAddressLength = SourceMessage.HardwareAddressLength,
                TransactionID = SourceMessage.TransactionID,
                Flags = SourceMessage.Flags,
                RelayAgentIPAddress = SourceMessage.RelayAgentIPAddress,
                ClientMacAddress = SourceMessage.ClientMacAddress
            };

            if (MessageType == DhcpMessageType.Acknowledge)
            {
                response.ClientIPAddress = SourceMessage.ClientIPAddress;
                response.YourClientIPAddress = AssignedIPAddress!;
                response.Options.SetValue(DhcpOption.IPAddressLeaseTime, LeaseSeconds);
                // Below are the additional options i need to check, whether is there any situation these get null or any other errors
                // response.Options.SetValue(DhcpOption.SubnetMask, subnetMask);                // Subnet mask option
                // response.Options.SetValue(DhcpOption.Router, gatewayIPAddress);              // Router (gateway) IP address option
                // response.Options.SetValue(DhcpOption.DomainNameServer, dnsServerAddresses);  // DNS servers option
                // response.Options.SetValue(DhcpOption.ServerIdentifier, serverIPAddress);     // Server identifier option
                // response.Options.SetValue(DhcpOption.RenewalTimeValue, renewalTimeSeconds);  // Renewal (T1) time option
                // response.Options.SetValue(DhcpOption.RebindingTimeValue, rebindingTimeSeconds); // Rebinding (T2) time option
                // response.Options.SetValue(DhcpOption.BroadcastAddress, broadcastAddress);    // Broadcast address option
                // response.Options.SetValue(DhcpOption.NetBIOSNameServer, netbiosNameServers); // NetBIOS name servers option
            }
            else if (!string.IsNullOrEmpty(ErrorMessage))
            {
                response.Options.SetValue(DhcpOption.Message, ErrorMessage);
            }

            return response;
        }
    }
}
