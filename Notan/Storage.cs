using Notan.Reflection;
using Notan.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Notan
{
    //For storing in collections
    public abstract class Storage
    {
        //TODO: delete me once Handle became generic
        internal readonly Type InnerType;

        internal readonly bool NoPersistence;

        public int Id { get; }

        private protected Storage(int id, bool noPersistence, Type innerType)
        {
            Id = id;
            NoPersistence = noPersistence;
            InnerType = innerType;
        }

        internal abstract void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>;

        internal abstract void Deserialize<TDeserializer>(TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>;

        internal abstract void LateDeserialize();

        internal abstract void HandleMessage(Client client, MessageType type, int index, int generation);

        internal abstract void FinalizeFrame();
    }

    //Common
    public abstract class StorageBase<T> : Storage where T : struct, IEntity
    {
        private protected FastList<T> entities = new();
        private protected FastList<int> entityToIndex = new();
        private protected FastList<bool> entityIsDead = new();
        private protected FastList<FastList<Client>> entityToObservers = new();
        private protected FastList<Client?> entityToAuthority = new();
        private protected FastList<int> indexToEntity = new();
        private protected FastList<int> generations = new();

        private protected int nextIndex;
        private protected int remaniningHandles = 0;

        internal StorageBase(int id, bool noPersistence) : base(id, noPersistence, typeof(T)) { }

        internal ref T Get(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            return ref entities[indexToEntity[index]];
        }

        internal bool Alive(int index, int generation)
        {
            return generations.Count > index && generations[index] == generation;
        }

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
                serializer.Entry("$gen").Write(generations[i]);
                if (entityToIndex.Count > index && entityToIndex[index] == i)
                {
                    entities[index].Serialize(serializer);
                }
                else
                {
                    serializer.Entry("$dead").Write("");
                }
                serializer.EndObject();
                i++;
            }
            serializer.EndArray();
        }

        internal override void Deserialize<TDeserializer>(TDeserializer deserializer)
        {
            entities.Clear();
            entityToIndex.Clear();
            entityIsDead.Clear();
            entityToObservers.Clear();
            entityToAuthority.Clear();
            indexToEntity.Clear();
            generations.Clear();

            var count = deserializer.BeginArray();
            for (int i = 0; i < count; i++)
            {
                var element = deserializer.NextArrayElement();
                generations.Add(element.Entry("$gen").ReadInt32());
                if (!element.TryGetEntry("$dead", out _))
                {
                    T t = default;
                    t.Deserialize(element);
                    entities.Add(t);
                    entityToIndex.Add(i);
                    entityIsDead.Add(false);
                    entityToObservers.Add(new());
                    entityToAuthority.Add(null);
                    indexToEntity.Add(entities.Count - 1);
                }
                else
                {
                    indexToEntity.Add(0);
                    Recycle(i);
                }
            }
        }

        internal override void LateDeserialize()
        {
            foreach (ref var entity in entities.AsSpan())
            {
                entity.LateDeserialize();
            }
        }

        [Conditional("DEBUG")]
        private protected static void Log(string log) => Console.WriteLine($"<{typeof(T)}> {log}");
    }

    //For servers
    public sealed class Storage<T> : StorageBase<T> where T : struct, IEntity
    {
        private FastList<int> destroyedEntityIndices = new();

        private readonly ClientAuthority authority;

        internal Storage(int id, StorageOptionsAttribute? options) : base(id, options != null && options.NoPersistence)
        {
            authority = options == null ? ClientAuthority.None : options.ClientAuthority;
        }

        public StrongHandle<T> Create(T entity)
        {
            int entind = entities.Count;
            entityToObservers.Add(new());
            entityToAuthority.Add(null);
            entityIsDead.Add(false);
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
            entities.Add(entity);
            entityToIndex.Add(hndind);

            return new(this, hndind, generations[hndind]);
        }

        internal void Destroy(int index, int generation)
        {
            Get(index, generation).OnDestroy();
            entityIsDead[indexToEntity[index]] = true;
            generations[index]++;
            destroyedEntityIndices.Add(index);
        }

        private void DestroyImmediate(int index)
        {
            foreach (var observer in entityToObservers[indexToEntity[index]].AsSpan())
            {
                observer.Send(Id, MessageType.Destroy, index, generations[index], ref Unsafe.NullRef<T>());
            }

            var entityIndex = indexToEntity[index];

            entityToObservers.RemoveAt(entityIndex);
            entityToAuthority.RemoveAt(entityIndex);
            entityIsDead.RemoveAt(entityIndex);
            entities.RemoveAt(entityIndex);
            indexToEntity[entityToIndex[^1]] = entityIndex;
            entityToIndex.RemoveAt(entityIndex);
            Recycle(index);
        }

        internal void AddObserver(int index, int generation, Client client)
        {
            Debug.Assert(Alive(index, generation));
            ref var list = ref entityToObservers[indexToEntity[index]];
            if (list.IndexOf(client) == -1)
            {
                list.Add(client);
                client.Send(Id, MessageType.Create, index, generation, ref Get(index, generation));
            }
        }

        internal void RemoveObserver(int index, int generation, Client client)
        {
            Debug.Assert(Alive(index, generation));
            if (entityToObservers[indexToEntity[index]].Remove(client))
            {
                client.Send(Id, MessageType.Destroy, index, generation, ref Unsafe.NullRef<T>());
            }
        }

        internal void UpdateObservers(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            ref var entity = ref Get(index, generation);
            ref var list = ref entityToObservers[indexToEntity[index]];
            var span = list.AsSpan();
            int i = span.Length;
            while (i > 0)
            {
                i--;
                var observer = span[i];
                if (observer.Connected)
                {
                    observer.Send(Id, MessageType.Update, index, generation, ref entity);
                }
                else
                {
                    list.RemoveAt(i);
                }
            }
        }

        internal void WipeObservers(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            ref var list = ref entityToObservers[indexToEntity[index]];
            int i = list.Count;
            while (i > 0)
            {
                i--;
                list[i].Send(Id, MessageType.Destroy, index, generation, ref Unsafe.NullRef<T>());
                list.RemoveAt(i);
            }
        }

        internal void SetAuthority(int index, int generation, Client? client)
        {
            Debug.Assert(Alive(index, generation));
            entityToAuthority[indexToEntity[index]] = client;
            if (client != null)
            {
                AddObserver(index, generation, client);
            }
        }

        internal Client? GetAuthority(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            return entityToAuthority[indexToEntity[index]];
        }

        public void Run<TSystem>(ref TSystem system) where TSystem : ISystem<T>
        {
            int i = entities.Count;
            while (i > 0)
            {
                i--;
                if (!entityIsDead[i])
                {
                    var index = entityToIndex[i];
                    system.Work(new(this, index, generations[index]), ref entities[i]);
                }
            }
        }

        internal sealed override void Deserialize<TDeserializer>(TDeserializer deserializer)
        {
            entityToObservers.Clear();
            entityToAuthority.Clear();
            entityIsDead.Clear();
            destroyedEntityIndices.Clear();
            remaniningHandles = 0;
            base.Deserialize(deserializer);
        }

        internal sealed override void HandleMessage(Client client, MessageType type, int index, int generation)
        {
            switch (type)
            {
                case MessageType.Create:
                    if (authority == ClientAuthority.Unauthenticated || (authority == ClientAuthority.Authenticated && client.Authenticated))
                    {
                        T entity = default;
                        client.ReadIntoEntity(ref entity);
                        var handle = Create(entity);
                        SetAuthority(handle.Index, handle.Generation, client);
                    }
                    else
                    {
                        Unsafe.SkipInit(out T entity);
                        client.ReadIntoEntity(ref entity);
                    }
                    break;
                case MessageType.Update:
                    if (Alive(index, generation) && entityToAuthority[indexToEntity[index]] == client)
                    {
                        client.ReadIntoEntity(ref Get(index, generation));
                    }
                    else
                    {
                        Unsafe.SkipInit(out T entity);
                        client.ReadIntoEntity(ref entity);
                    }
                    break;
                case MessageType.Destroy:
                    if (Alive(index, generation) && entityToAuthority[indexToEntity[index]] == client)
                    {
                        DestroyImmediate(index);
                    }
                    break;
            }
        }

        internal override void FinalizeFrame()
        {
            foreach (var index in destroyedEntityIndices.AsSpan())
            {
                DestroyImmediate(index);
            }
            destroyedEntityIndices.Clear();
        }
    }

    //For clients
    public sealed class StorageView<T> : StorageBase<T> where T : struct, IEntity
    {
        private readonly Client server;

        internal StorageView(int id, StorageOptionsAttribute? options, Client server) : base(id, options != null && options.NoPersistence)
        {
            this.server = server;
        }

        public void RequestCreate(T entity)
        {
            server.Send(Id, MessageType.Create, 0, 0, ref entity);
        }

        internal void RequestUpdate(ViewHandle<T> handle, T entity)
        {
            server.Send(Id, MessageType.Update, handle.Index, handle.Generation, ref entity);
        }

        internal void RequestDestroy(ViewHandle<T> handle)
        {
            server.Send(Id, MessageType.Destroy, handle.Index, handle.Generation, ref Unsafe.NullRef<T>());
        }

        internal override void HandleMessage(Client client, MessageType type, int index, int generation)
        {
            switch (type)
            {
                case MessageType.Create:
                    Log($"Creating {index}|{generation}");
                    int entid = entityToIndex.Count;
                    entityToIndex.Add(index);
                    T entity = default;
                    client.ReadIntoEntity(ref entity);
                    entities.Add(entity);
                    indexToEntity.EnsureSize(index + 1);
                    indexToEntity[index] = entid;
                    generations.EnsureSize(index + 1);
                    generations[index] = generation;
                    break;
                case MessageType.Update:
                    Log($"Updating {index}|{generation}");
                    client.ReadIntoEntity(ref entities[indexToEntity[index]]);
                    break;
                case MessageType.Destroy:
                    Log($"Destroying {index}|{generation}");
                    var entityIndex = indexToEntity[index];
                    entities.RemoveAt(entityIndex);
                    indexToEntity[entityToIndex[^1]] = entityIndex;
                    entityToIndex.RemoveAt(entityIndex);
                    break;
            }
        }

        public void Run<TSystem>(ref TSystem system) where TSystem : IViewSystem<T>
        {
            int i = entities.Count;
            while (i > 0)
            {
                i--;
                var index = entityToIndex[i];
                system.Work(new(this, index, generations[index]), ref entities[i]);
            }
        }

        internal override void FinalizeFrame() { }
    }
}
