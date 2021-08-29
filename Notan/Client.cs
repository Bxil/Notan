using Notan.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Notan
{
    public class Client
    {
        private readonly World world;

        private readonly TcpClient tcpClient;
        private readonly MemoryStream outgoing;
        private readonly NetworkStream stream;
        private readonly BinaryWriter writer;
        private readonly BinaryReader reader;

        private static readonly Encoding encoding = new UTF8Encoding(false);

        public int Id { get; }
        public bool Authenticated { get; set; } = false;
        public bool Connected => tcpClient.Connected;
        public DateTimeOffset LastCommunicated { get; private set; }
        public DateTimeOffset LoginTime { get; }
        public IPEndPoint IPEndPoint { get; }

        internal Client(World world, TcpClient tcpClient, int id)
        {
            this.world = world;
            this.tcpClient = tcpClient;
            IPEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint!;
            Id = id;

            outgoing = new MemoryStream();

            LastCommunicated = DateTimeOffset.Now;
            LoginTime = LastCommunicated;

            stream = tcpClient.GetStream();
            tcpClient.Client.Blocking = false; //Blocking cannot be false before the acquisiton of a stream.

            writer = new BinaryWriter(outgoing, encoding);
            reader = new BinaryReader(stream, encoding);

            lengthPrefix = 0;

            Log("Connected.");
        }

        public void Disconnect()
        {
            Log("Disconnecting.");
            tcpClient.Close();
        }

        internal void Flush()
        {
            if (outgoing.Position > 0)
            {
                outgoing.WriteTo(stream);
                outgoing.SetLength(0);
                LastCommunicated = DateTimeOffset.Now;
            }
        }

        internal void Send<T>(int storageid, MessageType type, int index, int generation, ref T entity) where T : struct, IEntity
        {
            //Leave space for the length prefix
            int prefixPosition = (int)outgoing.Position;
            outgoing.Position += sizeof(int);

            writer.Write(storageid);
            writer.Write((byte)type);
            writer.Write(index);
            writer.Write(generation);

            switch (type)
            {
                case MessageType.Create:
                    entity.Serialize(new BinarySerializer(writer));
                    break;
                case MessageType.Update:
                    entity.Serialize(new BinarySerializer(writer));
                    break;
                case MessageType.Destroy:
                    break;
            }

            int endPosition = (int)outgoing.Position;
            outgoing.Position = prefixPosition;
            writer.Write(endPosition - prefixPosition - sizeof(int));
            outgoing.Position = endPosition;
        }

        private int lengthPrefix;
        // After this function returns true and immediate read must follow.
        internal bool CanRead()
        {
            if (lengthPrefix == 0) //We are yet to read the prefix,
            {
                if (tcpClient.Available < sizeof(int)) //but it is unavailable.
                {
                    return false;
                }

                lengthPrefix = reader.ReadInt32();
            }
            return tcpClient.Available >= lengthPrefix;
        }

        internal int ReadHeader(out MessageType type, out int index, out int generation)
        {
            LastCommunicated = DateTimeOffset.Now;

            int storageid = reader.ReadInt32();
            type = (MessageType)reader.ReadByte();
            index = reader.ReadInt32();
            generation = reader.ReadInt32();

            lengthPrefix = 0;
            return storageid;
        }

        internal void ReadIntoEntity<T>(ref T entity) where T : struct, IEntity
        {
            entity.Deserialize(new BinaryDeserializer(world, reader));
        }

        [Conditional("DEBUG")]
        private protected void Log(string log) => Console.WriteLine($"{IPEndPoint} ({Id}) - {log}");
    }
}
