using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{
    public class DhcpOptionSubOptions
    {
        public DhcpOptionRelayAgentSubOptionType Type { get; set; }
        public byte[] Value { get; set; }
        public DhcpOptionSubOptions(DhcpOptionRelayAgentSubOptionType type, byte[] data)
        {
            Type = type;
            Value = data;
        }
    }
}

