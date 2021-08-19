using Notan.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Notan
{
    //For storing in collections
    public abstract class Storage
    {
        public int Id { get; }

        protected Storage(int id)
        {
            Id = id;
        }

        internal abstract void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer;

        internal abstract void Deserialize<TDeserializer>(TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>;

        internal abstract void HandleMessage(Client client, MessageType type, Handle handle);
    }

    //Common
    public abstract class StorageBase<T> : Storage where T : struct, IEntity
    {
        private protected FastList<int> entityToIndex = new();
        private protected FastList<T> entities = new();
        private protected FastList<int> indexToEntity = new();
        private protected FastList<int> generations = new();

        private protected int nextIndex;
        private protected int remaniningHandles = 0;

        internal StorageBase(int id) : base(id) { }

        private protected void Recycle(int index)
        {
            if (remaniningHandles > 0)
            {
                indexToEntity[index] = nextIndex;
            }
            nextIndex = index;
            remaniningHandles++;
        }

        internal override void Serialize<TSerializer>(TSerializer serializer)
        {
            serializer.BeginArray(indexToEntity.Count);
            int i = 0;
            foreach (var index in indexToEntity.AsSpan())
            {
                serializer.BeginObject();
                serializer.Write("$gen", generations[i]);
                if (entityToIndex[index] == i)
                {
                    serializer.Write("$alive", true);
                    entities[index].Serialize(serializer, true);
                }
                else
                {
                    serializer.Write("$alive", false);
                }
                serializer.EndObject();
                i++;
            }
            serializer.EndArray();
        }

        internal override void Deserialize<TDeserializer>(TDeserializer deserializer)
        {
            entityToIndex.Clear();
            entities.Clear();
            indexToEntity.Clear();
            generations.Clear();

            var count = deserializer.BeginArray();
            for (int i = 0; i < count; i++)
            {
                var element = deserializer.NextArrayElement();
                generations.Add(element.GetEntry("_gen").ReadInt32());
                if (element.GetEntry("_alive").ReadBool())
                {
                    T t = default;
                    t.Deserialize(element);
                    entities.Add(t);
                    entityToIndex.Add(i);
                    indexToEntity.Add(entities.Count - 1);
                }
                else
                {
                    indexToEntity.Add(0);
                    Recycle(i);
                }
            }
        }

        [Conditional("DEBUG")]
        private protected static void Log(string log) => Console.WriteLine($"<{typeof(T)}> {log}");
    }

    //For servers
    public sealed class Storage<T> : StorageBase<T> where T : struct, IEntity
    {
        private FastList<FastList<Client>> entityToObservers = new();
        private FastList<Client?> entityToAuthority = new();

        private readonly ClientAuthority authority;

        internal Storage(int id, ClientAuthority authority) : base(id)
        {
            this.authority = authority;
        }

        public ref T Create()
        {
            int entind = entities.Count;
            entityToObservers.Add(new());
            entityToAuthority.Add(null);
            int hndind;
            if (remaniningHandles > 0)
            {
                remaniningHandles--;
                hndind = nextIndex;
                nextIndex = indexToEntity[nextIndex];
                indexToEntity[hndind] = entind;
            }
            else
            {
                hndind = indexToEntity.Count;
                indexToEntity.Add(entind);
                generations.Add(0);
            }

            Log($"Creating {hndind}|{generations[hndind]}");
            entities.Add(new T { Handle = new(hndind, generations[hndind]) });
            entityToIndex.Add(hndind);

            return ref entities[entind];
        }

        public void Destroy(Handle handle)
        {
            Log($"Destroying {handle.Index}|{handle.Generation}");
            Debug.Assert(Alive(handle));

            foreach (var observer in entityToObservers[indexToEntity[handle.Index]].AsSpan())
            {
                observer.Send(Id, MessageType.Destroy, handle, ref Unsafe.NullRef<T>());
            }

            var index = indexToEntity[handle.Index];

            entityToObservers.RemoveAt(index);
            entityToAuthority.RemoveAt(index);
            entities.RemoveAt(index);
            indexToEntity[entityToIndex[^1]] = index;
            entityToIndex.RemoveAt(index);
            generations[handle.Index]++;
            Recycle(handle.Index);
        }

        public ref T Get(Handle handle)
        {
            Debug.Assert(Alive(handle));
            return ref entities[indexToEntity[handle.Index]];
        }

        public bool Alive(Handle handle)
        {
            return generations[handle.Index] == handle.Generation;
        }

        public void Run<TSystem>(ref TSystem system) where TSystem : ISystem<T>
        {
            int i = entities.Count;
            while (i > 0)
            {
                i--;
                system.Work(this, ref entities[i]);
            }
        }

        public void AddObserver(Handle handle, Client client)
        {
            entityToObservers[indexToEntity[handle.Index]].Add(client);
            client.Send(Id, MessageType.Create, handle, ref Get(handle));
        }

        public void RemoveObserver(Handle handle, Client client)
        {
            client.Send(Id, MessageType.Destroy, handle, ref Unsafe.NullRef<T>());
            entityToObservers[indexToEntity[handle.Index]].Remove(client);
        }

        public void UpdateObservers(Handle handle)
        {
            Debug.Assert(Alive(handle));
            ref var entity = ref Get(handle);
            foreach (var observer in entityToObservers[indexToEntity[handle.Index]].AsSpan())
            {
                observer.Send(Id, MessageType.Update, handle, ref entity);
            }
            entity.PostUpdate();
        }

        public void MakeAuthority(Handle handle, Client? client)
        {
            entityToAuthority[indexToEntity[handle.Index]] = client;
        }

        public Client? Authority(Handle handle)
        {
            Debug.Assert(Alive(handle));
            return entityToAuthority[indexToEntity[handle.Index]];
        }

        internal override void Deserialize<TDeserializer>(TDeserializer deserializer)
        {
            entityToObservers.Clear();
            entityToAuthority.Clear();
            remaniningHandles = 0;
            base.Deserialize(deserializer);
        }

        internal override void HandleMessage(Client client, MessageType type, Handle handle)
        {
            switch (type)
            {
                case MessageType.Create:
                    if (authority == ClientAuthority.Unauthenticated || (authority == ClientAuthority.Authenticated && client.Authenticated))
                    {
                        ref var entity = ref Create();
                        client.ReadIntoEntity(ref entity);
                        MakeAuthority(entity.Handle, client);
                    }
                    else
                    {
                        Unsafe.SkipInit(out T entity);
                        client.ReadIntoEntity(ref entity);
                    }
                    break;
                case MessageType.Update:
                    if (Alive(handle) && entityToAuthority[indexToEntity[handle.Index]] == client)
                    {
                        client.ReadIntoEntity(ref Get(handle));
                    }
                    break;
                case MessageType.Destroy:
                    if (Alive(handle) && entityToAuthority[indexToEntity[handle.Index]] == client)
                    {
                        Destroy(handle);
                    }
                    break;
            }
        }
    }

    //For clients
    public sealed class StorageView<T> : StorageBase<T> where T : struct, IEntity
    {
        private readonly Client server;

        internal StorageView(int id, Client server) : base(id)
        {
            this.server = server;
        }

        public void RequestCreate(T entity)
        {
            server.Send(Id, MessageType.Create, new(), ref entity);
        }

        public void RequestUpdate(Handle handle, T entity)
        {
            server.Send(Id, MessageType.Update, handle, ref entity);
        }

        public void RequestDestroy(Handle handle)
        {
            server.Send(Id, MessageType.Destroy, handle, ref Unsafe.NullRef<T>());
        }

        internal override void HandleMessage(Client client, MessageType type, Handle handle)
        {
            switch (type)
            {
                case MessageType.Create:
                    Log($"Creating {handle.Index}|{handle.Generation}");
                    int entid = entityToIndex.Count;
                    entityToIndex.Add(handle.Index);
                    T entity = default;
                    client.ReadIntoEntity(ref entity);
                    entities.Add(entity);
                    indexToEntity.EnsureSize(handle.Index + 1);
                    indexToEntity[handle.Index] = entid;
                    generations.EnsureSize(handle.Index + 1);
                    generations[handle.Index] = handle.Generation;
                    break;
                case MessageType.Update:
                    Log($"Updating {handle.Index}|{handle.Generation}");
                    client.ReadIntoEntity(ref entities[indexToEntity[handle.Index]]);
                    break;
                case MessageType.Destroy:
                    Log($"Destroying {handle.Index}|{handle.Generation}");
                    var index = indexToEntity[handle.Index];
                    entities.RemoveAt(index);
                    indexToEntity[entityToIndex[^1]] = index;
                    entityToIndex.RemoveAt(index);
                    break;
            }
        }

        public void Run<TSystem>(ref TSystem system) where TSystem : IViewSystem<T>
        {
            int i = entities.Count;
            while (i > 0)
            {
                i--;
                system.Work(this, ref entities[i]);
            }
        }
    }
}
