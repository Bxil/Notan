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
        internal readonly bool NoPersistence;

        public int Id { get; }

        private protected Storage(int id, bool noPersistence)
        {
            Id = id;
            NoPersistence = noPersistence;
        }

        internal abstract void Serialize<T>(T serializer) where T : ISerializer<T>;

        internal abstract void Deserialize<T>(T deserializer) where T : IDeserializer<T>;

        internal abstract void LateDeserialize();

        internal abstract void HandleMessage(Client client, MessageType type, int index, int generation);

        internal abstract void FinalizeFrame();
    }

    //Common
    public abstract class Storage<T> : Storage where T : struct, IEntity<T>
    {
        private protected FastList<T> entities = new();
        private protected FastList<int> entityToIndex = new();
        private protected FastList<int> indexToEntity = new();
        private protected FastList<int> generations = new();

        private protected int nextIndex;
        private protected int remaniningHandles = 0;

        internal Storage(int id, bool noPersistence) : base(id, noPersistence) { }

        internal ref T Get(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            return ref entities[indexToEntity[index]];
        }

        internal bool Alive(int index, int generation)
        {
            return generations.Count > index && generations[index] == generation;
        }
    }

    //For servers
    public sealed class ServerStorage<T> : Storage<T> where T : struct, IEntity<T>
    {
        private FastList<int> destroyedEntityIndices = new();

        private FastList<bool> entityIsDead = new();
        private FastList<FastList<Client>> entityToObservers = new();
        private FastList<Client?> entityToAuthority = new();

        private readonly ClientAuthority authority;

        internal ServerStorage(int id, StorageOptionsAttribute? options) : base(id, options != null && options.NoPersistence)
        {
            authority = options == null ? ClientAuthority.None : options.ClientAuthority;
        }

        public ServerHandle<T> Create(T entity)
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

            entities.Add(entity);
            entityToIndex.Add(hndind);

            var handle = new ServerHandle<T>(this, hndind, generations[hndind]);
            Get(hndind, generations[hndind]).LateCreate(handle);
            return handle;
        }

        internal void Destroy(int index, int generation)
        {
            Get(index, generation).OnDestroy(new(this, index, generation));

            DestroyInternal(index);

            foreach (var observer in entityToObservers[indexToEntity[index]].AsSpan())
            {
                observer.Send(Id, MessageType.Destroy, index, generations[index], ref Unsafe.NullRef<T>());
            }
        }

        //Destroy an entity without notifying anyone and running its OnDestroy.
        internal void Forget(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));

            DestroyInternal(index);
        }

        private void DestroyInternal(int index)
        {
            entityIsDead[indexToEntity[index]] = true;
            generations[index]++;
            destroyedEntityIndices.Add(index);
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

        public void Run<TSystem>(ref TSystem system) where TSystem : IServerSystem<T>
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

        public TSystem Run<TSystem>(TSystem system) where TSystem : IServerSystem<T>
        {
            Run(ref system);
            return system;
        }

        internal override void Serialize<TSer>(TSer serializer)
        {
            serializer.ArrayBegin();
            int i = 0;
            foreach (var index in indexToEntity.AsSpan())
            {
                serializer.ArrayNext().ObjectBegin();
                serializer.ObjectNext("$gen").Write(generations[i]);
                if (entityToIndex.Count > index && entityToIndex[index] == i)
                {
                    entities[index].Serialize(serializer);
                }
                else
                {
                    serializer.ObjectNext("$dead").Write(true);
                }
                serializer.ObjectEnd();
                i++;
            }
            serializer.ArrayEnd();
        }

        internal override void Deserialize<TDeser>(TDeser deserializer)
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

            deserializer.ArrayBegin();
            int i = 0;
            while (deserializer.ArrayTryNext())
            {
                deserializer.ObjectBegin();

                bool dead = false;
                T t = default;
                int gen = -1;
                while (deserializer.ObjectTryNext(out var key))
                {
                    if (key == "$gen")
                    {
                        gen = deserializer.GetInt32();
                    }
                    else if (key == "$dead")
                    {
                        deserializer.GetBool();
                        dead = true;
                    }
                    else
                    {
                        t.Deserialize(key, deserializer);
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
                        Destroy(index, generation);
                    }
                    break;
            }
        }

        internal override void FinalizeFrame()
        {
            foreach (var index in destroyedEntityIndices.AsSpan())
            {
                var entityIndex = indexToEntity[index];
                entityToObservers.RemoveAt(entityIndex);
                entityToAuthority.RemoveAt(entityIndex);
                entityIsDead.RemoveAt(entityIndex);
                entities.RemoveAt(entityIndex);
                indexToEntity[entityToIndex[^1]] = entityIndex;
                entityToIndex.RemoveAt(entityIndex);
                Recycle(index);
            }
            destroyedEntityIndices.Clear();
        }
    }

    //For clients
    public sealed class ClientStorage<T> : Storage<T> where T : struct, IEntity<T>
    {
        private readonly Client server;

        internal ClientStorage(int id, StorageOptionsAttribute? options, Client server) : base(id, options != null && options.NoPersistence)
        {
            this.server = server;
        }

        public void RequestCreate(T entity)
        {
            server.Send(Id, MessageType.Create, 0, 0, ref entity);
        }

        internal void RequestUpdate(int index, int generation, T entity)
        {
            server.Send(Id, MessageType.Update, index, generation, ref entity);
        }

        internal void RequestDestroy(int index, int generation)
        {
            server.Send(Id, MessageType.Destroy, index, generation, ref Unsafe.NullRef<T>());
        }

        internal void Forget(int index, int generation)
        {
            Debug.Assert(Alive(index, generation));
            DestroyInternal(index);
        }

        internal override void HandleMessage(Client client, MessageType type, int index, int generation)
        {
            switch (type)
            {
                case MessageType.Create:
                    {
                        int entid = entityToIndex.Count;
                        entityToIndex.Add(index);
                        T entity = default;
                        client.ReadIntoEntity(ref entity);
                        entities.Add(entity);
                        indexToEntity.EnsureSize(index + 1);
                        indexToEntity[index] = entid;
                        generations.EnsureSize(index + 1);
                        generations[index] = generation;
                    }
                    break;
                case MessageType.Update:
                    if (Alive(index, generation))
                    {
                        client.ReadIntoEntity(ref entities[indexToEntity[index]]);
                    }
                    else
                    {
                        Unsafe.SkipInit(out T entity);
                        client.ReadIntoEntity(ref entity);
                    }
                    break;
                case MessageType.Destroy:
                    if (Alive(index, generation))
                    {
                        DestroyInternal(index);
                    }
                    break;
            }
        }

        private void DestroyInternal(int index)
        {
            var entityIndex = indexToEntity[index];
            entities.RemoveAt(entityIndex);
            indexToEntity[entityToIndex[^1]] = entityIndex;
            entityToIndex.RemoveAt(entityIndex);
        }

        public void Run<TSystem>(ref TSystem system) where TSystem : IClientSystem<T>
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

        internal override void Serialize<TSer>(TSer serializer) => throw new NotImplementedException();

        internal override void Deserialize<TDeser>(TDeser deserializer) => throw new NotImplementedException();
    }
}
