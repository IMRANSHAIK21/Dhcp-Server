using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{
    public enum DhcpMessageType : byte
    {
        Discover = 1,
        Offer = 2,
        Request = 3,
        Decline = 4,
        Acknowledge = 5,
        NoAcknowledge = 6,
        Release = 7,
        Inform = 8
    }
}
