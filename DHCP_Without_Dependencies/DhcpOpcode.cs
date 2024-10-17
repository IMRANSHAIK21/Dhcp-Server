using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{
    public enum DhcpOpcode : byte
    {
        Unknown = 0,
        BootRequest = 1,
        BootReply = 2
    }
} 
