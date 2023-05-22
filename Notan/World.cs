using Notan.Reflection;
using Notan.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Notan;

public abstract class World
{
    private protected readonly Dictionary<string, Storage> TypeNameToStorage = new();
    internal FastList<Storage> IdToStorage = new(); //Element 0 is always null.

    public IPEndPoint EndPoint { get; protected set; } = null!;

    private protected World()
    {
        IdToStorage.Add(null!);
    }

    public Storage<T> GetStorage<T>() where T : struct, IEntity<T>
    {
        return Unsafe.As<Storage<T>>(TypeNameToStorage[typeof(T).ToString()]);
    }

    private protected volatile bool exit = false;
    public void Exit() => exit = true;

    public abstract void AddStorage<T>(StorageOptionsAttribute? options = default) where T : struct, IEntity<T>;
}

public sealed class ServerWorld : World
{
    private readonly TcpListener listener;

    private FastList<Client> clients = new();
    public Span<Client> Clients => clients.AsSpan();

    private int nextClientId = 0;
    private readonly Stack<int> clientIds = new();

    public ServerWorld(int port)
    {
        listener = TcpListener.Create(port);
        listener.Start();
        EndPoint = (IPEndPoint)listener.LocalEndpoint;
    }

    public override void AddStorage<T>(StorageOptionsAttribute? options = default)
    {
        var newstorage = new ServerStorage<T>(IdToStorage.Count, options);
        TypeNameToStorage.Add(typeof(T).ToString(), newstorage);
        IdToStorage.Add(newstorage);
    }

    public new ServerStorage<T> GetStorage<T>() where T : struct, IEntity<T>
    {
        return Unsafe.As<ServerStorage<T>>(base.GetStorage<T>());
    }

    public bool Tick()
    {
        foreach (var storage in IdToStorage.AsSpan()[1..])
        {
            storage.FinalizeFrame();
        }

        if (exit)
        {
            listener.Stop();
            return false;
        }

        while (listener.Pending())
        {
            if (!clientIds.TryPop(out var id))
            {
                id = nextClientId;
                nextClientId++;
            }
            clients.Add(new(this, listener.AcceptTcpClient(), id));
        }

        var i = clients.Count;
        while (i > 0)
        {
            i--;
            var client = clients[i];
            try
            {
                const int messageReadMaximum = 10;
                var messagesRead = 0;
                while (messagesRead < messageReadMaximum && client.CanRead())
                {
                    var id = client.ReadHeader(out var type, out var index, out var generation);
                    if (id <= 0 || id >= IdToStorage.Count)
                    {
                        throw new IOException();
                    }
                    IdToStorage[id].HandleMessage(client, type, index, generation);

                    messagesRead++;
                }
            }
            catch (IOException)
            {
                DeleteClient(client);
            }
        }

        i = clients.Count;
        while (i > 0)
        {
            i--;
            var client = clients[i];
            var delete = !client.Connected;
            if (!delete)
            {
                try
                {
                    client.Flush();
                }
                catch (IOException)
                {
                    delete = true;
                }
            }

            if (delete)
            {
                DeleteClient(client);
            }
        }

        return true;
    }

    private void DeleteClient(Client client)
    {
        clientIds.Push(client.Id);
        client.Disconnect();
        _ = clients.Remove(client);
    }

    public void Serialize<T>(T serializer) where T : ISerializer<T>
    {
        serializer.ObjectBegin();
        serializer.ObjectNext("HandleIdentifiers").ObjectBegin();
        foreach (var pair in TypeNameToStorage)
        {
            if (!pair.Value.Impermanent)
            {
                serializer.ObjectNext(pair.Key).Serialize(pair.Value.Id);
            }
        }
        serializer.ObjectEnd();
        serializer.ObjectNext("Entities").ObjectBegin();
        foreach (var pair in TypeNameToStorage)
        {
            if (!pair.Value.Impermanent)
            {
                pair.Value.Serialize(serializer.ObjectNext(pair.Key));
            }
        }
        serializer.ObjectEnd();
        serializer.ObjectEnd();
    }

    public void Deserialize<T>(T deserializer) where T : IDeserializer<T>
    {
        var idToStorageSaved = IdToStorage;

        IdToStorage = new();

        deserializer.ObjectBegin();
        while (deserializer.ObjectTryNext(out var key))
        {
            if (key == "HandleIdentifiers")
            {
                deserializer.ObjectBegin();
                while (deserializer.ObjectTryNext(out key))
                {
                    var storage = TypeNameToStorage[key.ToString()];
                    Unsafe.SkipInit(out int id);
                    deserializer.Deserialize(ref id);
                    IdToStorage.EnsureSize(id + 1);
                    IdToStorage[id] = storage;
                }
            }
            else if (key == "Entities")
            {
                deserializer.ObjectBegin();
                while (deserializer.ObjectTryNext(out key))
                {
                    TypeNameToStorage[key.ToString()!].Deserialize(deserializer);
                }
                foreach (var pair in TypeNameToStorage)
                {
                    pair.Value.LateDeserialize();
                }
            }
            else
            {
                throw new IOException();
            }
        }

        IdToStorage = idToStorageSaved;
    }
}

public sealed class ClientWorld : World
{
    private readonly Client server;

    private ClientWorld(TcpClient server)
    {
        this.server = new Client(this, server, 0);
        EndPoint = (IPEndPoint)server.Client.LocalEndPoint!;
    }

    public static async Task<ClientWorld> StartAsync(string host, int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port);
        return new ClientWorld(client);
    }

    public override void AddStorage<T>(StorageOptionsAttribute? options = default)
    {
        Storage newstorage = new ClientStorage<T>(IdToStorage.Count, options, server);
        TypeNameToStorage.Add(typeof(T).ToString(), newstorage);
        IdToStorage.Add(newstorage);
    }

    public new ClientStorage<T> GetStorage<T>() where T : struct, IEntity<T>
    {
        return Unsafe.As<ClientStorage<T>>(base.GetStorage<T>());
    }

    public bool Tick()
    {
        foreach (var storage in IdToStorage.AsSpan()[1..])
        {
            storage.FinalizeFrame();
        }

        if (exit)
        {
            server.Disconnect();
            return false;
        }

        try
        {
            server.Flush();
        }
        catch (IOException)
        {
            return false;
        }

        while (server.CanRead())
        {
            IdToStorage[server.ReadHeader(out var type, out var index, out var generation)].HandleMessage(server, type, index, generation);
        }

        return true;
    }
}
