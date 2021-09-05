using Notan.Reflection;
using Notan.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Notan
{
    public abstract class World
    {
        private protected readonly Dictionary<string, Storage> TypeNameToStorage = new();
        internal FastList<Storage> IdToStorage = new();

        public IPEndPoint EndPoint { get; protected set; }

        private protected World()
        {
            EndPoint = null!;
        }

        public StorageBase<T> GetStorageBase<T>() where T : struct, IEntity<T>
        {
            return Unsafe.As<StorageBase<T>>(TypeNameToStorage[typeof(T).ToString()]);
        }

        private protected volatile bool exit = false;
        public void Exit() => exit = true;

        public abstract void AddStorage<T>(StorageOptionsAttribute? options = default) where T : struct, IEntity<T>;

        public void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>
        {
            serializer.BeginObject();
            foreach (var pair in TypeNameToStorage.Where(x => !x.Value.NoPersistence).OrderBy(x => x.Key))
            {
                serializer.Entry(pair.Key);
                pair.Value.Serialize(serializer);
            }
            serializer.EndObject();
        }

        public void Deserialize<TDeserializer>(TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>
        {
            foreach (var pair in TypeNameToStorage.OrderBy(x => x.Key))
            {
                if (deserializer.TryGetEntry(pair.Key, out var entry))
                {
                    pair.Value.Deserialize(entry);
                }
            }
            foreach (var pair in TypeNameToStorage.OrderBy(x => x.Key))
            {
                pair.Value.LateDeserialize();
            }
        }
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
            Storage newstorage = new Storage<T>(IdToStorage.Count, options);
            TypeNameToStorage.Add(typeof(T).ToString(), newstorage);
            IdToStorage.Add(newstorage);
        }

        public Storage<T> GetStorage<T>() where T : struct, IEntity<T>
        {
            return Unsafe.As<Storage<T>>(GetStorageBase<T>());
        }

        public bool Tick()
        {
            foreach (var storage in IdToStorage.AsSpan())
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
                if (!clientIds.TryPop(out int id))
                {
                    id = nextClientId;
                    nextClientId++;
                }
                clients.Add(new(this, listener.AcceptTcpClient(), id));
            }

            int i = clients.Count;
            while (i > 0)
            {
                i--;
                var client = clients[i];
                try
                {
                    const int messageReadMaximum = 10;
                    int messagesRead = 0;
                    while (messagesRead < messageReadMaximum && client.CanRead())
                    {
                        int id = client.ReadHeader(out var type, out var index, out var generation);
                        if (id < 0 || id >= IdToStorage.Count)
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
                try
                {
                    client.Flush();
                }
                catch
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
            clients.Remove(client);
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
            Storage newstorage = new StorageView<T>(IdToStorage.Count, options, server);
            TypeNameToStorage.Add(typeof(T).ToString(), newstorage);
            IdToStorage.Add(newstorage);
        }

        public StorageView<T> GetStorageView<T>() where T : struct, IEntity<T>
        {
            return Unsafe.As<StorageView<T>>(GetStorageBase<T>());
        }
        public bool Tick()
        {
            if (exit)
            {
                server.Disconnect();
                return false;
            }

            server.Flush(); //TODO: make this not crash the client
            while (server.CanRead())
            {
                IdToStorage[server.ReadHeader(out var type, out int index, out var generation)].HandleMessage(server, type, index, generation);
            }

            return true;
        }
    }
}
