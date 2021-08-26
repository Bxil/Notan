using Notan.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Notan
{
    public abstract class World
    {
        private protected readonly Dictionary<string, Storage> TypeNameToStorage = new();
        internal FastList<Storage> IdToStorage = new();

        public TimeSpan Timestep { get; set; } = TimeSpan.FromSeconds(1.0 / 60.0);

        public IPEndPoint EndPoint { get; protected set; }

        private protected World()
        {
            EndPoint = null!;
        }

        public StorageBase<T> GetStorageBase<T>() where T : struct, IEntity
        {
            return Unsafe.As<StorageBase<T>>(TypeNameToStorage[typeof(T).FullName!]);
        }

        private TimeSpan lastEnded = TimeSpan.Zero;
        private readonly Stopwatch watch = Stopwatch.StartNew();
        private protected void Sleep()
        {
            var ended = watch.Elapsed;
            var sleepfor = lastEnded - ended + Timestep;
            lastEnded = ended + sleepfor;
            if (sleepfor > TimeSpan.Zero)
            {
                Thread.Sleep(sleepfor);
            }
        }

        private protected volatile bool exit = false;
        public void Exit() => exit = true;

        public abstract void AddStorage<T>(StorageOptions options = default) where T : struct, IEntity;

        public void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer
        {
            serializer.BeginObject();
            foreach (var pair in TypeNameToStorage.Where(x => !x.Value.NoSerialization).OrderBy(x => x.Key))
            {
                serializer.WriteEntry(pair.Key);
                pair.Value.Serialize(serializer);
            }
            serializer.EndObject();
        }

        public void Deserialize<TDeserializer>(TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>
        {
            foreach (var pair in TypeNameToStorage.OrderBy(x => x.Key))
            {
                pair.Value.Deserialize(deserializer.GetEntry(pair.Key));
            }
        }
    }

    public sealed class ServerWorld : World
    {
        private readonly TcpListener listener;

        private FastList<Client> clients = new();

        private int nextClientId = 0;
        private readonly Stack<int> clientIds = new();

        public ServerWorld(int port)
        {
            listener = TcpListener.Create(port);
            listener.Start();
            EndPoint = (IPEndPoint)listener.LocalEndpoint;
        }

        public override void AddStorage<T>(StorageOptions options = default)
        {
            Storage newstorage = new Storage<T>(IdToStorage.Count, options);
            TypeNameToStorage.Add(typeof(T).FullName!, newstorage);
            IdToStorage.Add(newstorage);
        }

        public bool TryDequeueClient(out Client client)
        {
            client = null!;
            if (listener.Pending())
            {
                if (!clientIds.TryPop(out int id))
                {
                    id = nextClientId;
                    nextClientId++;
                }

                client = new(this, listener.AcceptTcpClient(), id);
                clients.Add(client);
                return true;
            }
            return false;
        }

        public Storage<T> GetStorage<T>() where T : struct, IEntity
        {
            return Unsafe.As<Storage<T>>(GetStorageBase<T>());
        }

        public bool Loop()
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

            int i = clients.Count;
            while (i > 0)
            {
                i--;
                var client = clients[i];
                try
                {
                    client.Flush();

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
                    clientIds.Push(client.Id);
                    client.Disconnect();
                    clients.Remove(client);
                }
            }

            Sleep();

            return true;
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
            var world = new ClientWorld(client);
            return world;
        }

        public override void AddStorage<T>(StorageOptions options = default)
        {
            Storage newstorage = new StorageView<T>(IdToStorage.Count, options, server);
            TypeNameToStorage.Add(typeof(T).FullName!, newstorage);
            IdToStorage.Add(newstorage);
        }

        public StorageView<T> GetStorageView<T>() where T : struct, IEntity
        {
            return Unsafe.As<StorageView<T>>(GetStorageBase<T>());
        }
        public bool Loop()
        {
            if (exit)
            {
                return false;
            }

            server.Flush();
            while (server.CanRead())
            {
                IdToStorage[server.ReadHeader(out var type, out int index, out var generation)].HandleMessage(server, type, index, generation);
            }

            Sleep();

            return true;
        }
    }
}
