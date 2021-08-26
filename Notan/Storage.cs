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
        internal Type InnerType { get; }

        public int Id { get; }

        private protected Storage(int id, Type innerType)
        {
            Id = id;
            InnerType = innerType;
        }

        internal abstract void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer;

        internal abstract void Deserialize<TDeserializer>(TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>;

        internal abstract void HandleMessage(Client client, MessageType type, int index, int generation);

        internal abstract void FinalizeFrame();
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

        internal StorageBase(int id) : base(id, typeof(T)) { }

        internal ref T Get(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            return ref entities[indexToEntity[index]];
        }

        internal bool Alive(int index, int generation)
        {
            return generations[index] == generation;
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
                serializer.Write("$gen", generations[i]);
                if (entityToIndex[index] == i)
                {
                    serializer.Write("$alive", true);
                    entities[index].Serialize(serializer);
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

        private FastList<bool> entityIsDead = new();

        private FastList<int> destroyedEntityIndices = new();

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
            entities.Add(new T { Handle = new(this, hndind, generations[hndind]) });
            entityToIndex.Add(hndind);

            return ref entities[entind];
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
            entityToObservers[indexToEntity[index]].Add(client);
            client.Send(Id, MessageType.Create, index, generation, ref Get(index, generation));
        }

        internal void RemoveObserver(int index, int generation, Client client)
        {
            Debug.Assert(Alive(index, generation));
            client.Send(Id, MessageType.Destroy, index, generation, ref Unsafe.NullRef<T>());
            entityToObservers[indexToEntity[index]].Remove(client);
        }

        internal void UpdateObservers(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            ref var entity = ref Get(index, generation);
            foreach (var observer in entityToObservers[indexToEntity[index]].AsSpan())
            {
                observer.Send(Id, MessageType.Update, index, generation, ref entity);
            }
        }

        internal void MakeAuthority(int index, int generation, Client? client)
        {
            Debug.Assert(Alive(index, generation));
            entityToAuthority[indexToEntity[index]] = client;
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
                ref var entity = ref entities[i];
                if (!entityIsDead[i])
                {
                    system.Work(ref entity);
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
                        ref var entity = ref Create();
                        client.ReadIntoEntity(ref entity);
                        MakeAuthority(entity.Handle.Index, entity.Handle.Generation, client);
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

        internal StorageView(int id, Client server) : base(id)
        {
            this.server = server;
        }

        public void RequestCreate(T entity)
        {
            server.Send(Id, MessageType.Create, 0, 0, ref entity);
        }

        public void RequestUpdate(Handle handle, T entity)
        {
            server.Send(Id, MessageType.Update, handle.Index, handle.Generation, ref entity);
        }

        public void RequestDestroy(Handle handle)
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
                system.Work(ref entities[i]);
            }
        }

        internal override void FinalizeFrame() { }
    }
}
