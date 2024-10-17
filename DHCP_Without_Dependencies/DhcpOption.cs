﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{
    public enum DhcpOption : byte
    {
        // RFC 1497 Vendor Extension
        Pad = 0,
        SubnetMask = 1,
        TimeOffset = 2,
        Router = 3,
        TimeServer = 4,
        NameServer = 5,
        DomainNameServer = 6,
        LogServer = 7,
        CookieServer = 8,
        LprServer = 9,
        ImpressServer = 10,
        ResourceLocationServer = 11,
        HostName = 12,
        BootFileSize = 13,
        MeritDump = 14,
        DomainName = 15,
        SwapServer = 16,
        RootPath = 17,
        ExtensionsPath = 18,

        // IP Layer Paremeters per Host
        IpForwarding = 19,
        NonLocalSourceRouting = 20,
        PolicyFilter = 21,
        MaximumDatagramReAssemblySize = 22,
        DefaultIPTimeToLive = 23,
        PathMtuAgingTimeout = 24,
        PathMtuPlateauTable = 25,

        // IP Layer Parameters per Interface
        InterfaceMtu = 26,
        AllSubnetsAreLocal = 27,
        BroadcastAddress = 28,
        PerformMaskDiscovery = 29,
        MaskSupplier = 30,
        PerformRouterDiscovery = 31,
        RouterSolicitationAddress = 32,
        StaticRoute = 33,

        // Link Layer Parameters per Interface
        TrailerEncapsulation = 34,
        ArpCacheTimeout = 35,
        EthernetEncapsulation = 36,

        // TCP parameters
        TcpDefaultTtl = 37,
        TcpKeepaliveInterval = 38,
        TcpKeepaliveGarbage = 39,

        // Application and Service parameters
        NetworkInformationServiceDomain = 40,
        NetworkInformationServers = 41,
        NetworkTimeProtocolServers = 42,
        VendorSpecificInformation = 43,
        NetBiosOverTcpIPNameServer = 44,
        NetBiosOverTcpIPDatagramDistributionServer = 45,
        NetBiosOverTcpIPNodeType = 46,
        NetBiosOverTcpIPScope = 47,
        XWindowSystemFontServer = 48,
        XWindowSystemDisplayManager = 49,        
        NetworkInformationServicePlusDomain = 64,
        NetworkInformationServicePlusServers = 65,
        MobileIPHomeAgent = 68,
        SmtpServer = 69,
        Pop3Server = 70,
        NntpServer = 71,
        DefaultWwwServer = 72,
        DefaultFingerServer = 73,
        DefaultIrcServer = 74,
        StreetTalkServer = 75,
        StdaServer = 76,

        // DHCP Extensions
        RequestedIPAddress = 50,// this option is used in a client request to allow the client to request a particular IP address to be assigned
        IPAddressLeaseTime = 51,
        OptionOverload = 52,
        DhcpMessageType = 53,
        ServerIdentifier = 54,
        ParameterRequestList = 55,
        Message = 56,
        MaximumDhcpMessageSize = 57,
        RenewalTimeValue_T1 = 58,
        RebindingTimeValue_T2 = 59,
        Vendorclassidentifier = 60,
        ClientIdentifier = 61,
        TftpServerName = 66,
        BootfileName = 67,


        FullyQualifiedDomainName = 81,              // RFC4702
        RelayAgentInformation = 82,                 // RFC3046, RFC6607

        ClientSystemArchitectureType = 93,          // RFC4578
        ClientNetworkInterfaceIdentifier = 94,      // RFC4578
        ClientMachineIdentifier = 97,               // RFC4578

        AutoConfigure = 116,                        // RFC2563
        ClasslessStaticRoutesA = 121,               // RFC3442

        /*
            128   TFPT Server IP address                        // RFC 4578 
            129   Call Server IP address                        // RFC 4578 
            130   Discrimination string                         // RFC 4578 
            131   Remote statistics server IP address           // RFC 4578 
            132   802.1P VLAN ID
            133   802.1Q L2 Priority
            134   Diffserv Code Point
            135   HTTP Proxy for phone-specific applications    
         */

        ClasslessStaticRoutesB = 249,

        End = 255,
    }
}
