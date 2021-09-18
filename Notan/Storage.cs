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

        internal abstract void Serialize<TEntry, TArray, TObject>(TEntry serializer)
            where TEntry : ISerializerEntry<TEntry, TArray, TObject>
            where TArray : ISerializerArray<TEntry, TArray, TObject>
            where TObject : ISerializerObject<TEntry, TArray, TObject>;

        internal abstract void Deserialize<TEntry, TArray, TObject>(TEntry deserializer)
            where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
            where TArray : IDeserializerArray<TEntry, TArray, TObject>
            where TObject : IDeserializerObject<TEntry, TArray, TObject>;

        internal abstract void LateDeserialize();

        internal abstract void HandleMessage(Client client, MessageType type, int index, int generation);

        internal abstract void FinalizeFrame();
    }

    //Common
    public abstract class StorageBase<T> : Storage where T : struct, IEntity<T>
    {
        private protected FastList<T> entities = new();
        private protected FastList<int> entityToIndex = new();
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

        [Conditional("DEBUG")]
        private protected static void Log(string log) => Console.WriteLine($"<{typeof(T)}> {log}");
    }

    //For servers
    public sealed class Storage<T> : StorageBase<T> where T : struct, IEntity<T>
    {
        private FastList<int> destroyedEntityIndices = new();

        private FastList<bool> entityIsDead = new();
        private FastList<FastList<Client>> entityToObservers = new();
        private FastList<Client?> entityToAuthority = new();

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

            var handle = new StrongHandle<T>(this, hndind, generations[hndind]);
            Get(hndind, generations[hndind]).LateCreate(handle);
            return handle;
        }

        internal void Destroy(int index, int generation)
        {
            Get(index, generation).OnDestroy(new(this, index, generation));
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

        private void Recycle(int index)
        {
            if (remaniningHandles > 0)
            {
                indexToEntity[index] = nextIndex;
            }
            nextIndex = index;
            remaniningHandles++;
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

        public TSystem Run<TSystem>(TSystem system) where TSystem : ISystem<T>
        {
            Run(ref system);
            return system;
        }

        internal override void Serialize<TEntry, TArray, TObject>(TEntry serializer)
        {
            var arr = serializer.WriteArray();
            int i = 0;
            foreach (var index in indexToEntity.AsSpan())
            {
                var obj = arr.Next().WriteObject();
                obj.Next("$gen").Write(generations[i]);
                if (entityToIndex.Count > index && entityToIndex[index] == i)
                {
                    entities[index].Serialize<TEntry, TArray, TObject>(obj);
                }
                else
                {
                    obj.Next("$dead").Write("");
                }
                obj.End();
                i++;
            }
            arr.End();
        }

        internal override void Deserialize<TEntry, TArray, TObject>(TEntry deserializer)
        {
            destroyedEntityIndices.Clear();
            remaniningHandles = 0;

            entities.Clear();
            entityToIndex.Clear();
            entityIsDead.Clear();
            entityToObservers.Clear();
            entityToAuthority.Clear();
            indexToEntity.Clear();
            generations.Clear();

            var arr = deserializer.GetArray();
            int i = 0;
            while (arr.NextEntry(out var entry))
            {
                var obj = entry.GetObject();

                bool dead = false;
                T t = default;
                int gen = -1;
                while (obj.NextEntry(out var key, out var value))
                {
                    switch (key)
                    {
                        case "$gen":
                            gen = value.GetInt32();
                            break;
                        case "$dead":
                            value.GetString();
                            dead = true;
                            break;
                        default:
                            t.Deserialize<TEntry, TArray, TObject>(key, value);
                            break;
                    }
                }

                generations.Add(gen);

                if (!dead)
                {
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
                i++;
            }
        }

        internal override void LateDeserialize()
        {
            int i = 0;
            foreach (ref var entity in entities.AsSpan())
            {
                entity.LateDeserialize(new(this, entityToIndex[i], generations[entityToIndex[i]]));
                i++;
            }
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
    public sealed class StorageView<T> : StorageBase<T> where T : struct, IEntity<T>
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

        internal override void FinalizeFrame() => throw new NotImplementedException();

        internal override void LateDeserialize() => throw new NotImplementedException();

        internal override void Serialize<TEntry, TArray, TObject>(TEntry serializer) => throw new NotImplementedException();

        internal override void Deserialize<TEntry, TArray, TObject>(TEntry deserializer) => throw new NotImplementedException();
    }
}
