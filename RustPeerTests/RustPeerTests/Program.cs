using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Facepunch;
using Network;
using Network.Implementation.Raknet;

namespace RustPeerTests
{
    public abstract class TestImpl
    {
        private string m_name;
        private int m_count;

        protected TestImpl(string name, int count)
        {
            if (count <= 0)
                throw new ArgumentException("m_count out of range");

            m_name = name;
            m_count = count;
        }

        public void RunTest()
        {
            Console.WriteLine("Run test {0} {1} times", m_name, m_count);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < m_count; ++i)
            {
                Test();
            }

            sw.Stop();
            Console.WriteLine("Test {0} complete, it takes {1} ms to execute", m_name, sw.ElapsedMilliseconds);
        }

        protected abstract void Test();
    }

    public sealed class RustPeer : TestImpl
    {
        public static byte[] s_SomeBytes = Encoding.UTF8.GetBytes("RustPeerTest");
        public static int s_Size = s_SomeBytes.Length;

        private Peer m_peer;
        public RustPeer(int count)
            : base(typeof(RustPeer).Name, count)
        {
            m_peer = new Peer();
            m_peer.ptr = Native.NET_Create();
        }

        protected override void Test()
        {
            m_peer.WriteBool(false);
            m_peer.WriteUInt8(42);
            m_peer.WriteUInt16(1339);
            m_peer.WriteInt32(42);
            m_peer.WriteUInt64(655353535);
            m_peer.WriteFloat(42.042f);
            m_peer.WriteDouble(8899.0);
            m_peer.WriteBytes(s_SomeBytes, 0, s_Size);
        }
    }

    public sealed class OptimizedRustPeer : TestImpl
    {
        public static byte[] s_SomeBytes = Encoding.UTF8.GetBytes("RustPeerTest");
        public static int s_Size = s_SomeBytes.Length;

        private PeerV2 m_peer;
        public OptimizedRustPeer(int count)
            : base(typeof(OptimizedRustPeer).Name, count)
        {
            m_peer = new PeerV2();
            m_peer.ptr = Native.NET_Create();
        }

        protected override void Test()
        {
            m_peer.WriteBool(false);
            m_peer.WriteUInt8(42);
            m_peer.WriteUInt16(1339);
            m_peer.WriteInt32(42);
            m_peer.WriteUInt64(655353535);
            m_peer.WriteFloat(42.042f);
            m_peer.WriteDouble(8899.0);
            m_peer.WriteBytes(s_SomeBytes, 0, s_Size);
        }
    }

    public sealed class ReadCStringTest : TestImpl
    {
        public static byte[] s_SomeBytes = Encoding.UTF8.GetBytes("Somenullterminatedstringfortestpurpose" + '\0');

        private BinaryReader reader;

        public ReadCStringTest(int count)
            : base(typeof(ReadCStringTest).Name, count)
        {
            reader = new BinaryReader(new MemoryStream(s_SomeBytes, false));
        }
        protected override void Test()
        {
            var pos = reader.BaseStream.Position;
            string str = ReadNullTerminatedString(reader);
            reader.BaseStream.Position = pos;
        }

        internal string ReadNullTerminatedString(BinaryReader read)
        {
            string text = string.Empty;
            while (read.BaseStream.Position != read.BaseStream.Length)
            {
                char c = (char)read.ReadSByte();
                if (c == '\0')
                {
                    return text;
                }
                text += c;
            }
            return string.Empty;
        }
    }

    public sealed class OptimizedReadCStringTest : TestImpl
    {
        public static byte[] s_SomeBytes = Encoding.UTF8.GetBytes("Somenullterminatedstringfortestpurpose" + '\0');

        private BinaryReader reader;

        public OptimizedReadCStringTest(int count)
            : base(typeof(OptimizedReadCStringTest).Name, count)
        {
            reader = new BinaryReader(new MemoryStream(s_SomeBytes));
        }
        protected override void Test()
        {
            var pos = reader.BaseStream.Position;
            string str = ReadNullTerminatedString(reader);
            reader.BaseStream.Position = pos;
        }

        internal string ReadNullTerminatedString(BinaryReader read)
        {
            // End of stream case
            if (read.BaseStream.Position == read.BaseStream.Length)
                return string.Empty;

            List<byte> list = new List<byte>();
            while (read.BaseStream.Position != read.BaseStream.Length)
            {
                int nc = read.ReadSByte();
                if (((Char)nc) == '\0')
                    break;

                list.Add((byte)nc);
            }
            return Encoding.UTF8.GetString(list.ToArray());
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ReadCStringTest test0 = new ReadCStringTest(10000);
            test0.RunTest();

            Console.WriteLine();

            OptimizedReadCStringTest test1 = new OptimizedReadCStringTest(10000);
            test1.RunTest();
        }
    }
}
