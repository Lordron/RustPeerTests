using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    class Program
    {
        static void Main(string[] args)
        {
            RustPeer test0 = new RustPeer(10000);
            test0.RunTest();

            Console.WriteLine();

            OptimizedRustPeer test1 = new OptimizedRustPeer(10000);
            test1.RunTest();
        }
    }
}
