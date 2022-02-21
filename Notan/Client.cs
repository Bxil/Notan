using Notan.Serialization;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Notan;

public class Client
{
    private readonly TcpClient tcpClient;
    private readonly MemoryStream outgoing;
    private readonly NetworkStream stream;
    private readonly BinaryWriter writer;
    private readonly BinaryReader reader;

    private readonly BinarySerializer serializer;
    private readonly BinaryDeserializer deserializer;

    private static readonly UTF8Encoding encoding = new(false);

    public int Id { get; }
    public bool Authenticated { get; set; } = false;
    public bool Connected => tcpClient.Connected;
    public DateTimeOffset LastCommunicated { get; private set; }
    public DateTimeOffset LoginTime { get; }
    public IPEndPoint IPEndPoint { get; }

    internal Client(World world, TcpClient tcpClient, int id)
    {
        this.tcpClient = tcpClient;
        IPEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint!;
        Id = id;

        outgoing = new MemoryStream();

        LastCommunicated = DateTimeOffset.Now;
        LoginTime = LastCommunicated;

        stream = tcpClient.GetStream();
        tcpClient.Client.Blocking = false; //Blocking cannot be false before the acquisiton of a stream.

        writer = new BinaryWriter(outgoing, encoding, true);
        reader = new BinaryReader(stream, encoding, true);

        serializer = new(outgoing, encoding);
        deserializer = new(world, stream, encoding);

        lengthPrefix = 0;
    }

    public void Disconnect()
    {
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

    internal void Send<T>(int storageid, MessageType type, int index, int generation, ref T entity) where T : struct, IEntity<T>
    {
        //Leave space for the length prefix
        var prefixPosition = (int)outgoing.Position;
        outgoing.Position += sizeof(int);

        writer.Write(storageid);
        writer.Write((byte)type);
        writer.Write(index);
        writer.Write(generation);

        switch (type)
        {
            case MessageType.Create:
            case MessageType.Update:
                entity.Serialize(serializer);
                break;
            case MessageType.Destroy:
                break;
        }

        var endPosition = (int)outgoing.Position;
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

        var storageid = reader.ReadInt32();
        type = (MessageType)reader.ReadByte();
        index = reader.ReadInt32();
        generation = reader.ReadInt32();

        lengthPrefix = 0;
        return storageid;
    }

    internal void ReadIntoEntity<T>(ref T entity) where T : struct, IEntity<T>
    {
        entity.Deserialize(deserializer);
    }
}
