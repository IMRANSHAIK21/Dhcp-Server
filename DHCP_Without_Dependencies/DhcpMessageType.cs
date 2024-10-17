using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{
    public enum DhcpMessageType : byte
    {
        Discover = 1, // Dhcp Discover
        Offer = 2, // Dhcp Offer
        Request = 3, // Dhcp Request
        Decline = 4, // Dhcp Decline
        Acknowledge = 5, // Dhcp Acknowledgement
        NoAcknowledge = 6, // Dhcp Negative Acknowledgement
        Release = 7, // Dhcp Release
        Inform = 8 // Dhcp Inform
    }
}
