using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DHCP_Without_Dependencies
{
    // DHCP MESSAGE FORMAT (adapted from https://www.ietf.org/rfc/rfc2131.txt)
    /// <summary>
    /// Represents a DHCP message.
    /// </summary>
    public class DhcpMessage
    {
        // this is a magic cookie used in dhcp message to identifythe start of DHCP options.
        // 99.130.83.99 is a standard value defined in DHCP Protocol(RFC 2132)
        //   When used with BOOTP, the first four octets of the vendor informationfield have been assigned to the "magic cookie" (as suggested in RFC951). .
        // This field identifies the mode in which the succeeding data is to be interpreted.  The value of the magic cookie is the 
        // 4 octet dotted decimal 99.130.83.99 (or hexadecimal number 63.82.53.63) in network byte order.

        // Use: Validates that received DHCP message contains the correct magic cookie, ensuring well formed DHCP message
        private static readonly IPAddress BootPMagicCookieValue = IPAddress.Parse("99.130.83.99");

        // Represents the byte offset in the DHCP message where the options field begins. (RFC 2131 and RFC 2132)
        // This is essential for correctly parsing and serializing DHCP options.
        // this 240 is mandatory as per DHCP protocol spedification
        // In simple it is the byte position where Option Section begins.
        internal const int OptionFieldOffset = 240;

        // Direct Fields of DHCP Message Structure

        // Specifies the messge Type.(Ex. Request or Reply)
        public DhcpOpcode OpCode { get; }

        // Hardware address (Ex. ethernet etc.,)
        public DhcpHardwareType HardwareAddressType { get; internal set; }

        // Length of the hardware address (e.g., 6 for MAC addresses).
        public byte HardwareAddressLength { get; internal set; }

        //  Specifies options related to the hardware address.
        public byte HardwareOptions { get; internal set; }

        public uint TransactionID { get; internal set; }

        // The amount of time that has passed since a client started trying to obtain an IP address from a network.
        public ushort SecondsElapsed { get; internal set; }

        //  Flags that convey additional information, such as whether the client is requesting a broadcast.
        public ushort Flags { get; internal set; }

        // IP Address of a Client 
        public IPAddress ClientIPAddress { get; internal set; } = IPAddress.Any;

        // IP Address for a Client, which is offered by DHCP
        public IPAddress YourClientIPAddress { get; internal set; } = IPAddress.Any;

        // Server IP address need to rename this with ServerIPAddress
        public IPAddress NextServerIPAddress { get; internal set; } = IPAddress.Any;


        // to update the Relay agent IP address
        public IPAddress RelayAgentIPAddress { get; internal set; } = IPAddress.Any;

        public PhysicalAddress ClientMacAddress { get; internal set; } = new PhysicalAddress(new byte[6]);

        public string ServerHostName { get; internal set; } = string.Empty;

        // Name of the boot file (used in network booting scenarios)
        public string BootFileName { get; internal set; } = string.Empty;

        // Collection of DHCP options, which provide additional configuration parameters.
        public DhcpOptionCollection Options { get; }

        //  Initializes a new DHCP message with the specified opcode and message type, setting up the options collection accordingly.
        // This is to create the DHCP message from the scratch.
        // This is useful when our application need to Construct and Send a DHCP message.
        public DhcpMessage(DhcpOpcode opCode, DhcpMessageType messageType)
        {
            OpCode = opCode;
            Options = new DhcpOptionCollection(messageType);
        }

        // Constructs a Dhcp Message instance by parsing a raw byte array representing a DHCP message.
        public DhcpMessage(byte[] messageData)
        {
            // the minimum lenght of dhcp message is 244 bytes.
            if (messageData.Length < 244)
                throw new InvalidDataException("Message is not long enough to be a valid DHCP message.");

            OpCode = (DhcpOpcode)messageData[0];
            HardwareAddressType = (DhcpHardwareType)messageData[1];
            HardwareAddressLength = messageData[2];
            HardwareOptions = messageData[3];
            TransactionID = ReadUInt32(messageData, 4);
            SecondsElapsed = ReadUInt16(messageData, 8);
            Flags = ReadUInt16(messageData, 10);
            ClientIPAddress = ReadIPAddress(messageData, 12);
            YourClientIPAddress = ReadIPAddress(messageData, 16);
            NextServerIPAddress = ReadIPAddress(messageData, 20);
            RelayAgentIPAddress = ReadIPAddress(messageData, 24);
            ClientMacAddress = ReadPhysicalAddress(messageData, 28);
            ServerHostName = ReadNullTerminatedString(messageData, 44, 64);
            BootFileName = ReadNullTerminatedString(messageData, 108, 128);

            var magicCookie = ReadIPAddress(messageData, 236);

            if (!BootPMagicCookieValue.Equals(magicCookie))
                throw new InvalidDataException($"Wrong magic cookie value. Expected: {BootPMagicCookieValue}, Received: {magicCookie}");

            Options = new DhcpOptionCollection(messageData);
        }

        // This will Serializes the DHCP message object into a byte array received over the network.
        // returns the final byte arry.
        public byte[] GetBytes()
        {
            // Checking the Required DHCP message Type option is present.
            if (!Options.ContainsKey(DhcpOption.DhcpMessageType))
                throw new InvalidDataException($"Required option '{nameof(DhcpOption.DhcpMessageType)}' is missing.");

            using var stream = new MemoryStream(512);

            stream.WriteByte((byte)OpCode);
            stream.WriteByte((byte)HardwareAddressType);
            stream.WriteByte(HardwareAddressLength);
            stream.WriteByte(HardwareOptions);
            Write(TransactionID, stream);
            Write(SecondsElapsed, stream);
            Write(Flags, stream);
            Write(ClientIPAddress, stream);
            Write(YourClientIPAddress, stream);
            Write(NextServerIPAddress, stream);
            Write(RelayAgentIPAddress, stream);
            Write(ClientMacAddress, stream);
            WriteNullTerminatedString(ServerHostName, 64, stream);
            WriteNullTerminatedString(BootFileName, 128, stream);

            Write(BootPMagicCookieValue, stream);
            Options.Write(stream);

            return stream.ToArray();
        }
#region Helper Method for Parsing

        // Extracts a string from the byte array, stopping at the first null byte or after a specified count.
        // Parses ServerHostName and BootFileName.
        public static string ReadNullTerminatedString(byte[] data, int offset, int count)
        {
            int nullIndex = Array.IndexOf(data, (byte)0, offset, count);
            count = nullIndex >= 0 ? nullIndex - offset : count;

            return Encoding.ASCII.GetString(data, offset, count);
        }

        // Reads a MAC address (6 bytes/ 48 bits) from the specified offset in the byte array.
        public static PhysicalAddress ReadPhysicalAddress(byte[] data, int offset)
        {
            byte[] bytes = new byte[6];
            Buffer.BlockCopy(data, offset, bytes, 0, 6);
            return new PhysicalAddress(bytes);
        }
        
        //  Reads a 4-byte IPv4 address from the byte array.
        public static IPAddress ReadIPAddress(byte[] data, int offset)
        {
            byte[] bytes = new byte[4];
            Buffer.BlockCopy(data, offset, bytes, 0, 4);
            return new IPAddress(bytes);
        }

        //  Reads 16-bit unsigned integers from the byte array, handling byte ordering.
        public static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)(data[offset] << 8 | data[offset]);
        }

        //  //  Reads 32-bit unsigned integers from the byte array, handling byte ordering.
        public static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        }

#endregion
#region  Helper Methods for Serialization: Means writing specific data types to the Memory Stream
#region Write Methods are for Serializing data types to stream in the correct order
        public static void Write(ushort value, Stream stream)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        public static void Write(uint value, Stream stream)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        public static void Write(IPAddress value, Stream stream)
        {
            if (value.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 addresses are supported.");

            byte[] bytes = value.GetAddressBytes();
            stream.Write(bytes, 0, 4);
        }

        public static void Write(PhysicalAddress value, Stream stream)
        {
            byte[] bytes = value.GetAddressBytes();
            byte[] padding = new byte[10];

            if (bytes.Length != 6)
                throw new ArgumentException("Only MAC-48 physical addresses are supported.", nameof(value));

            stream.Write(bytes, 0, bytes.Length);
            stream.Write(padding, 0, padding.Length);
        }

        // Writes a string to the stream, ensuring it is null-terminated and padded to fit the specified field length.
        public static void WriteNullTerminatedString(string value, int fieldLength, Stream stream)
        {
            if (value.Length >= fieldLength)
                throw new ArgumentException("The value was too long for the field length.");

            byte[] bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);

            for (int i = bytes.Length; i < fieldLength; i++)
                stream.WriteByte(0);
        }
#endregion
        // if we use this overriden ToString, to get human readable DHCP message. for Debugging and also will be useful incase of logging
        public override string ToString()
        {
            string value = $"DHCP Message: [{Options.MessageType}] {ClientMacAddress} / {ClientIPAddress}";

            var address = Options.RequestedIPAddress ?? YourClientIPAddress;

            if (address?.Equals(IPAddress.Any) == false)
                value += $" => {address}";

            if (Options.ContainsKey(DhcpOption.Message))
                value += $" - {Options.GetString(DhcpOption.Message)}";

            return value;
        }
    }
}
