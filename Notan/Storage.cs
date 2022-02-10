using Notan.Reflection;
using Notan.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Notan;

//For storing in collections
public abstract class Storage
{
    private protected FastList<int> generations = new();

    internal readonly bool Impermanent;

    public int Id { get; }

    private protected Storage(int id, bool impermanent)
    {
        Id = id;
        Impermanent = impermanent;
    }

    internal bool Alive(int index, int generation)
    {
        return generations.Count > index && generations[index] == generation;
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

    private protected int nextIndex;
    private protected int remaniningHandles = 0;

    internal Storage(int id, bool impermanent) : base(id, impermanent) { }

    internal ref T Get(int index, int generation)
    {
        Debug.Assert(Alive(index, generation));
        return ref entities[indexToEntity[index]];
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

    internal ServerStorage(int id, StorageOptionsAttribute? options) : base(id, options != null && options.Impermanent)
    {
        authority = options == null ? ClientAuthority.None : options.ClientAuthority;
    }

    public ServerHandle<T> Create(T entity)
    {
        var entind = entities.Count;
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
            generations.Add(1);
            //We start from 1 so generation 0 is always invalid.
            //This is necessary to make [0,0] invalid:
            //Even if Storage was null it might become non-null when deserializing.
        }

        entities.Add(entity);
        entityToIndex.Add(hndind);

        var handle = new ServerHandle<T>(this, hndind, generations[hndind]);
        Get(hndind, generations[hndind]).PostUpdate(handle);
        return handle;
    }

    internal void Destroy(int index, int generation)
    {
        Get(index, generation).PreUpdate(new(this, index, generation));
        Get(index, generation).OnDestroy(new(this, index, generation));

        foreach (var observer in entityToObservers[indexToEntity[index]].AsSpan())
        {
            observer.Send(Id, MessageType.Destroy, index, generations[index], ref Unsafe.NullRef<T>());
        }

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

    internal void AddObservers(int index, int generation, ReadOnlySpan<Client> clients)
    {
        Debug.Assert(Alive(index, generation));
        ref var list = ref entityToObservers[indexToEntity[index]];
        list.EnsureCapacity(list.Count + clients.Length);
        foreach (var client in clients)
        {
            if (list.IndexOf(client) == -1)
            {
                list.Add(client);
                client.Send(Id, MessageType.Create, index, generation, ref Get(index, generation));
            }
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
        var i = span.Length;
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

    internal void ClearObservers(int index, int generation)
    {
        Debug.Assert(Alive(index, generation));
        ref var list = ref entityToObservers[indexToEntity[index]];
        var i = list.Count;
        while (i > 0)
        {
            i--;
            list[i].Send(Id, MessageType.Destroy, index, generation, ref Unsafe.NullRef<T>());
            list.RemoveAt(i);
        }
    }

    internal ReadOnlySpan<Client> GetObservers(int index, int generation)
    {
        Debug.Assert(Alive(index, generation));
        return entityToObservers[indexToEntity[index]].AsSpan();
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
        var i = entities.Count;
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
        var i = 0;
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
        var i = 0;
        while (deserializer.ArrayTryNext())
        {
            deserializer.ObjectBegin();

            var dead = false;
            T t = default;
            var gen = -1;
            while (deserializer.ObjectTryNext(out var key))
            {
                if (key == "$gen")
                {
                    gen = deserializer.GetInt32();
                }
                else if (key == "$dead")
                {
                    _ = deserializer.GetBoolean();
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
        var i = 0;
        foreach (ref var entity in entities.AsSpan())
        {
            entity.PostUpdate(new(this, entityToIndex[i], generations[entityToIndex[i]]));
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
                    ref var entity = ref Get(index, generation);
                    entity.PreUpdate(new(this, index, generation));
                    client.ReadIntoEntity(ref entity);
                    entity.PostUpdate(new(this, index, generation));
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

    private FastList<int> forgottenEntityIndices = new();
    private FastList<bool> entityIsForgotten = new();

    internal ClientStorage(int id, StorageOptionsAttribute? options, Client server) : base(id, options != null && options.Impermanent)
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
        generations[index] = -1;
        entityIsForgotten[indexToEntity[index]] = true;
        forgottenEntityIndices.Add(index);
    }

    internal override void HandleMessage(Client client, MessageType type, int index, int generation)
    {
        switch (type)
        {
            case MessageType.Create:
                {
                    var entid = entityToIndex.Count;
                    entityToIndex.Add(index);
                    T entity = default;
                    client.ReadIntoEntity(ref entity);
                    entities.Add(entity);
                    entityIsForgotten.Add(false);
                    indexToEntity.EnsureSize(index + 1);
                    indexToEntity[index] = entid;
                    generations.EnsureSize(index + 1);
                    generations[index] = generation;
                    Get(index, generation).PostUpdate(new(this, index, generation));
                }
                break;
            case MessageType.Update:
                if (Alive(index, generation))
                {
                    ref var entity = ref Get(index, generation);
                    entity.PreUpdate(new(this, index, generation));
                    client.ReadIntoEntity(ref entity);
                    entity.PostUpdate(new(this, index, generation));
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
                    Get(index, generation).PreUpdate(new(this, index, generation));
                    generations[index] = -1;
                    DestroyInternal(index);
                }
                break;
        }
    }

    private void DestroyInternal(int index)
    {
        var entityIndex = indexToEntity[index];
        entityIsForgotten.RemoveAt(entityIndex);
        entities.RemoveAt(entityIndex);
        indexToEntity[entityToIndex[^1]] = entityIndex;
        entityToIndex.RemoveAt(entityIndex);
    }

    public void Run<TSystem>(ref TSystem system) where TSystem : IClientSystem<T>
    {
        var i = entities.Count;
        while (i > 0)
        {
            i--;
            if (!entityIsForgotten[i])
            {
                var index = entityToIndex[i];
                system.Work(new(this, index, generations[index]), ref entities[i]);
            }
        }
    }

    public TSystem Run<TSystem>(TSystem system) where TSystem : IClientSystem<T>
    {
        Run(ref system);
        return system;
    }

    internal override void LateDeserialize() => throw new NotImplementedException();

    internal override void Serialize<TSer>(TSer serializer) => throw new NotImplementedException();

    internal override void Deserialize<TDeser>(TDeser deserializer) => throw new NotImplementedException();

    internal override void FinalizeFrame()
    {
        foreach (var index in forgottenEntityIndices.AsSpan())
        {
            entityIsForgotten[indexToEntity[index]] = false;
            DestroyInternal(index);
        }
        forgottenEntityIndices.Clear();
    }
}
