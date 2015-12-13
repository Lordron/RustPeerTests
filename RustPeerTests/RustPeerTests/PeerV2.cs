using System;
using System.Runtime.InteropServices;
using System.Security;
using Network;
using Network.Implementation.Raknet;
using UnityEngine;
namespace RustPeerTests
{
    [SuppressUnmanagedCodeSecurity]
    internal class PeerV2
    {
        public enum PacketReliability
        {
            UNRELIABLE,
            UNRELIABLE_SEQUENCED,
            RELIABLE,
            RELIABLE_ORDERED,
            RELIABLE_SEQUENCED,
            UNRELIABLE_WITH_ACK_RECEIPT,
            RELIABLE_WITH_ACK_RECEIPT,
            RELIABLE_ORDERED_WITH_ACK_RECEIPT
        }

        public IntPtr ptr;
        private static byte[] ByteBuffer = new byte[512];
        public uint sentSplitPackets;
        public ulong incomingGUID
        {
            get
            {
                this.Check();
                return Native.NETRCV_GUID(this.ptr);
            }
        }
        public uint incomingAddressInt
        {
            get
            {
                this.Check();
                return Native.NETRCV_Address(this.ptr);
            }
        }
        public uint incomingPort
        {
            get
            {
                this.Check();
                return Native.NETRCV_Port(this.ptr);
            }
        }
        public string incomingAddress
        {
            get
            {
                this.Check();
                return this.GetAddress(this.incomingGUID);
            }
        }
        public int incomingBits
        {
            get
            {
                this.Check();
                return Native.NETRCV_LengthBits(this.ptr);
            }
        }
        public int incomingBitsUnread
        {
            get
            {
                this.Check();
                return Native.NETRCV_UnreadBits(this.ptr);
            }
        }
        public int incomingBytes
        {
            get
            {
                this.Check();
                return this.incomingBits / 8;
            }
        }
        public int incomingBytesUnread
        {
            get
            {
                this.Check();
                return this.incomingBitsUnread / 8;
            }
        }
        public float incomingAge
        {
            get
            {
                this.Check();
                return Native.NETRCV_GetAge(this.ptr);
            }
        }
        public static Peer CreateServer(string ip, int port, int maxConnections)
        {
            Peer peer = new Peer();
            peer.ptr = Native.NET_Create();
            if (Native.NET_StartServer(peer.ptr, ip, port, maxConnections) == 0)
            {
                return peer;
            }
            peer.Close();
            string text = PeerV2.StringFromPointer(Native.NET_LastStartupError(peer.ptr));
            Debug.LogWarning(string.Concat(new object[]
            {
                "Couldn't create server on port ",
                port,
                " (",
                text,
                ")"
            }));
            return null;
        }
        public static Peer CreateConnection(string hostname, int port, int retries, int retryDelay, int timeout)
        {
            Peer peer = new Peer();
            peer.ptr = Native.NET_Create();
            if (Native.NET_StartClient(peer.ptr, hostname, port, retries, retryDelay, timeout) == 0)
            {
                return peer;
            }
            string text = PeerV2.StringFromPointer(Native.NET_LastStartupError(peer.ptr));
            Debug.LogWarning(string.Concat(new object[]
            {
                "Couldn't connect to server ",
                hostname,
                ":",
                port,
                " (",
                text,
                ")"
            }));
            peer.Close();
            return null;
        }
        public void Close()
        {
            if (this.ptr != IntPtr.Zero)
            {
                Native.NET_Close(this.ptr);
                this.ptr = IntPtr.Zero;
            }
        }
        public bool Receive()
        {
            return !(this.ptr == IntPtr.Zero) && Native.NET_Receive(this.ptr);
        }
        public unsafe int Read(byte[] buffer, int offset, int length)
        {
            if (offset != 0)
            {
                throw new NotImplementedException("Offset != 0");
            }
            length = Mathf.Min(this.incomingBytesUnread, length);
            fixed (byte* data = &buffer[0])
            {
                if (!Native.NETRCV_ReadBytes(this.ptr, data, length))
                {
                    Debug.LogError("NETRCV_ReadBytes returned false");
                    return 0;
                }
            }
            return length;
        }
        public bool ReadBit()
        {
            return this.ReadUInt8() != 0;
        }
        public unsafe long ReadInt64()
        {
            return *(byte*)_Read(sizeof(long));
        }
        public unsafe int ReadInt32()
        {
            return *(byte*)_Read(sizeof(int));
        }
        public unsafe short ReadInt16()
        {
            return *(byte*)_Read(sizeof(short));
        }
        public unsafe sbyte ReadInt8()
        {
            return *(sbyte*)_Read(sizeof(sbyte));
        }
        public unsafe ulong ReadUInt64()
        {
            return *(byte*)_Read(sizeof(ulong));
        }
        public unsafe uint ReadUInt32()
        {
            return *(byte*)_Read(sizeof(uint));
        }
        public unsafe ushort ReadUInt16()
        {
            return *(byte*)_Read(sizeof(ushort));
        }
        public byte ReadUInt8()
        {
            return this.ReadByte();
        }
        public void SetReadPos(int pos)
        {
            Native.NETRCV_SetReadPointer(this.ptr, pos);
        }
        public unsafe float ReadFloat()
        {
            return *(byte*)_Read(sizeof(float));
        }
        public unsafe double ReadDouble()
        {
            return *(byte*)_Read(sizeof(double));
        }

        private unsafe byte* _Read(int size)
        {
            this.Check();

            fixed (byte* data = &PeerV2.ByteBuffer[0])
            {
                if (!Native.NETRCV_ReadBytes(this.ptr, data, size))
                {
                    Debug.LogError("NETRCV_ReadBytes returned false");
                    return null;
                }

                return data;
            }
        }

        public unsafe byte ReadByte()
        {
            return *(byte*)_Read(sizeof(byte));
        }

        public unsafe byte[] ReadBytes(int length)
        {
            this.Check();
            if (length == -1)
            {
                length = this.incomingBytesUnread;
            }
            byte[] array = new byte[length];
            fixed (byte* data = &array[0])
            {
                if (!Native.NETRCV_ReadBytes(this.ptr, data, length))
                {
                    Debug.LogError("NETRCV_ReadBytes returned false");
                    return null;
                }
            }
            return array;
        }
        public void SendStart()
        {
            this.Check();
            Native.NETSND_Start(this.ptr);
        }

        private unsafe void _Write(byte* data, int size)
        {
            this.Check();
            Native.NETSND_WriteBytes(this.ptr, data, size);
        }

        public void WriteBool(bool val)
        {
            this.WriteUInt8((byte)((!val) ? 0 : 1));
        }
        public unsafe void WriteUInt8(byte val)
        {
            _Write(&val, sizeof(byte));
        }

        public unsafe void WriteUInt16(ushort val)
        {
            _Write((byte*)&val, sizeof(ushort));
        }
        public unsafe void WriteUInt32(uint val)
        {
            _Write((byte*)&val, sizeof(uint));
        }
        public unsafe void WriteUInt64(ulong val)
        {
            _Write((byte*)&val, sizeof(ulong));
        }
        public unsafe void WriteInt8(sbyte val)
        {
            _Write((byte*)&val, sizeof(sbyte));
        }
        public unsafe void WriteInt16(short val)
        {
            _Write((byte*)&val, sizeof(short));
        }
        public unsafe void WriteInt32(int val)
        {
            _Write((byte*)&val, sizeof(int));
        }
        public unsafe void WriteInt64(long val)
        {
            _Write((byte*)&val, sizeof(long));
        }
        public unsafe void WriteFloat(float val)
        {
            _Write((byte*)&val, sizeof(float));
        }
        public unsafe void WriteDouble(double val)
        {
            _Write((byte*)&val, sizeof(double));
        }
        public unsafe void WriteBytes(byte[] val)
        {
            fixed (byte* data = &val[0])
            {
                _Write(data, val.Length);
            }
        }
        public unsafe void WriteBytes(byte[] val, int offset, int length)
        {
            if (offset != 0)
            {
                throw new NotSupportedException("offset != 0");
            }
            fixed (byte* data = &val[0])
            {
                _Write(data, length);
            }
        }
        public uint SendBroadcast(Priority priority, SendMethod reliability, sbyte channel)
        {
            this.Check();
            return Native.NETSND_Broadcast(this.ptr, this.ToRaknetPriority(priority), this.ToRaknetPacketReliability(reliability), (int)channel);
        }
        public uint SendTo(ulong guid, Priority priority, SendMethod reliability, sbyte channel)
        {
            this.Check();
            return Native.NETSND_Send(this.ptr, guid, this.ToRaknetPriority(priority), this.ToRaknetPacketReliability(reliability), (int)channel);
        }
        public unsafe void SendUnconnectedMessage(byte* data, int length, uint adr, ushort port)
        {
            this.Check();
            Native.NET_SendMessage(this.ptr, data, length, adr, port);
        }
        public string GetAddress(ulong guid)
        {
            this.Check();
            return PeerV2.StringFromPointer(Native.NET_GetAddress(this.ptr, guid));
        }
        private static string StringFromPointer(IntPtr p)
        {
            if (p == IntPtr.Zero)
            {
                return string.Empty;
            }
            return Marshal.PtrToStringAnsi(p);
        }
        public int ToRaknetPriority(Priority priority)
        {
            switch (priority)
            {
                case Priority.Immediate:
                    return 0;
                case Priority.High:
                    return 1;
                case Priority.Medium:
                    return 2;
                default:
                    return 3;
            }
        }
        public int ToRaknetPacketReliability(SendMethod priority)
        {
            switch (priority)
            {
                case SendMethod.Reliable:
                    return 3;
                case SendMethod.ReliableUnordered:
                    return 2;
                case SendMethod.ReliableSequenced:
                    return 4;
                case SendMethod.Unreliable:
                    return 0;
                case SendMethod.UnreliableSequenced:
                    return 1;
                default:
                    return 3;
            }
        }
        public void Kick(Connection connection)
        {
            this.Check();
            Native.NET_CloseConnection(this.ptr, connection.guid);
        }
        private void Check()
        {
            if (this.ptr == IntPtr.Zero)
            {
                throw new NullReferenceException("Peer has already shut down!");
            }
        }
        public string GetStatisticsString(ulong guid)
        {
            return string.Format("Average Ping:\t\t{0}\nLast Ping:\t\t{1}\nLowest Ping:\t\t{2}\n{3}", new object[]
            {
                this.GetPingAverage(guid),
                this.GetPingLast(guid),
                this.GetPingLowest(guid),
                PeerV2.StringFromPointer(Native.NET_GetStatisticsString(this.ptr, guid))
            });
        }
        public int GetPingAverage(ulong guid)
        {
            return Native.NET_GetAveragePing(this.ptr, guid);
        }
        public int GetPingLast(ulong guid)
        {
            return Native.NET_GetLastPing(this.ptr, guid);
        }
        public int GetPingLowest(ulong guid)
        {
            return Native.NET_GetLowestPing(this.ptr, guid);
        }
        public void StartLogging(string logName)
        {
            Native.NET_StartLogging(this.ptr, logName);
        }
        public void StopLogging()
        {
            Native.NET_StopLogging(this.ptr);
        }
    }
}