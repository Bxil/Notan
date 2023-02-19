using Notan.Serialization;
using System;
using System.Runtime.CompilerServices;

namespace Notan;

public readonly record struct Handle
{
    public readonly Storage? Storage;

    public readonly int Index;
    public readonly int Generation;

    internal Handle(Storage? storage, int index, int generation)
    {
        Storage = storage;
        Index = index;
        Generation = generation;
    }

    public ServerHandle<T> Server<T>() where T : struct, IEntity<T> => Strong<T>().Server();

    public ClientHandle<T> Client<T>() where T : struct, IEntity<T> => Strong<T>().Client();

    public Handle<T> Strong<T>() where T : struct, IEntity<T>
    {
        if (Storage is not null and not Storage<T>)
        {
            NotanException.Throw($"Expected backing storage to be {typeof(T)} but it is {Storage.GetType()}");
        }
        return new(Unsafe.As<Storage<T>>(Storage), Index, Generation);
    }

    public bool TryStrong<T>(out Handle<T> handle) where T : struct, IEntity<T>
    {
        if (Storage is Storage<T> storage)
        {
            handle = new(storage, Index, Generation);
            return true;
        }
        handle = default;
        return false;
    }
}

//Beware of https://github.com/dotnet/runtime/issues/6924
public readonly record struct Handle<T> where T : struct, IEntity<T>
{
    public readonly Storage<T>? Storage;

    public readonly int Index;
    public readonly int Generation;

    public bool IsServer => Storage is ServerStorage<T>;

    internal Handle(Storage<T>? storage, int index, int generation)
    {
        Storage = storage;
        Index = index;
        Generation = generation;
    }

    public ref T Get() => ref Storage!.Get(Index, Generation);

    public ServerHandle<T> Server()
    {
        if (Storage is not null and not ServerStorage<T>)
        {
            NotanException.Throw($"Expected backing storage to be {typeof(T)} but it is {Storage.GetType()}");
        }
        return new(Unsafe.As<ServerStorage<T>>(Storage), Index, Generation);
    }

    public ClientHandle<T> Client()
    {
        if (Storage is not null and not ClientStorage<T>)
        {
            NotanException.Throw($"Expected backing storage to be {typeof(T)} but it is {Storage.GetType()}");
        }
        return new(Unsafe.As<ClientStorage<T>>(Storage), Index, Generation);
    }

    public static implicit operator Handle(Handle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);
}

public readonly record struct ServerHandle<T> where T : struct, IEntity<T>
{
    public readonly ServerStorage<T>? Storage;

    public readonly int Index;
    public readonly int Generation;

    internal ServerHandle(ServerStorage<T>? storage, int index, int generation)
    {
        Storage = storage;
        Index = index;
        Generation = generation;
    }

    public ref T Get() => ref Storage!.Get(Index, Generation);

    public void Destroy() => Storage!.Destroy(Index, Generation);

    public void AddObserver(Client client) => Storage!.AddObserver(Index, Generation, client);

    public void AddObservers(ReadOnlySpan<Client> clients) => Storage!.AddObservers(Index, Generation, clients);

    public void RemoveObserver(Client client) => Storage!.RemoveObserver(Index, Generation, client);

    public void UpdateObservers() => Storage!.UpdateObservers(Index, Generation);

    public void ClearObservers() => Storage!.ClearObservers(Index, Generation);

    public ReadOnlySpan<Client> Observers => Storage!.GetObservers(Index, Generation);

    public Client? Authority
    {
        get => Storage!.GetAuthority(Index, Generation);
        set => Storage!.SetAuthority(Index, Generation, value);
    }

    public static implicit operator Handle(ServerHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public static implicit operator Handle<T>(ServerHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);
}

public readonly record struct ClientHandle<T> where T : struct, IEntity<T>
{
    public readonly ClientStorage<T>? Storage;

    public readonly int Index;
    public readonly int Generation;

    internal ClientHandle(ClientStorage<T>? storage, int index, int generation)
    {
        Storage = storage;
        Index = index;
        Generation = generation;
    }

    public ref T Get() => ref Storage!.Get(Index, Generation);

    public void Forget() => Storage!.Forget(Index, Generation);

    public void RequestDestroy() => Storage!.RequestDestroy(Index, Generation);

    public void RequestUpdate() => Storage!.RequestUpdate(Index, Generation, Get());

    public void RequestUpdate(T entity) => Storage!.RequestUpdate(Index, Generation, entity);

    public static implicit operator Handle(ClientHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public static implicit operator Handle<T>(ClientHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);
}

public readonly struct Maybe<T> where T : struct, IEntity<T>
{
    internal readonly Handle<T> Handle;

    public Maybe(Handle<T> handle) => Handle = handle;

    public bool Alive() => Handle.Storage?.Alive(Handle.Index, Handle.Generation) ?? false;

    public bool Alive(out Handle<T> handle)
    {
        handle = Handle;
        return Alive();
    }

    public bool AliveServer(out ServerHandle<T> handle)
    {
        handle = Handle.Server();
        return Alive();
    }

    public bool AliveClient(out ClientHandle<T> handle)
    {
        handle = Handle.Client();
        return Alive();
    }

    public static implicit operator Maybe<T>(Handle<T> handle) => new(handle);
    public static implicit operator Maybe<T>(ServerHandle<T> handle) => new(handle);
    public static implicit operator Maybe<T>(ClientHandle<T> handle) => new(handle);
}

public static class HandleSerializer
{
    public static void Serialize<T>(this T serializer, Handle handle) where T : ISerializer<T>
    {
        serializer.ArrayBegin();
        if (handle.Storage == null)
        {
            serializer.ArrayNext().Write(0);
            serializer.ArrayNext().Write(0);
            serializer.ArrayNext().Write(0);
        }
        else
        {
            serializer.ArrayNext().Write(handle.Storage.Id);
            serializer.ArrayNext().Write(handle.Index);
            serializer.ArrayNext().Write(handle.Generation);
        }
        serializer.ArrayEnd();
    }

    public static void Deserialize<T>(this T deserializer, ref Handle handle) where T : IDeserializer<T>
    {
        deserializer.ArrayBegin();
        var storageid = deserializer.ArrayNext().GetInt32();
        var storages = deserializer.World.IdToStorage.AsSpan();
        handle = new Handle(storageid > 0 && storageid < storages.Length ? storages[storageid] : null, deserializer.ArrayNext().GetInt32(), deserializer.ArrayNext().GetInt32());
        _ = deserializer.ArrayTryNext(); //consume the end marker
    }

    public static void Serialize<TSer, T>(this TSer serializer, Handle<T> handle)
        where TSer : ISerializer<TSer>
        where T : struct, IEntity<T>
    {
        serializer.Serialize((Handle)handle);
    }

    public static void Deserialize<TDeser, T>(this TDeser deserializer, ref Handle<T> handle)
        where TDeser : IDeserializer<TDeser>
        where T : struct, IEntity<T>
    {
        Unsafe.SkipInit(out Handle weak);
        deserializer.Deserialize(ref weak);
        handle = weak.Strong<T>();
    }

    public static void Serialize<TSer, T>(this TSer serializer, ServerHandle<T> handle)
    where TSer : ISerializer<TSer>
    where T : struct, IEntity<T>
    {
        serializer.Serialize((Handle)handle);
    }

    public static void Deserialize<TDeser, T>(this TDeser deserializer, ref ServerHandle<T> handle)
        where TDeser : IDeserializer<TDeser>
        where T : struct, IEntity<T>
    {
        Unsafe.SkipInit(out Handle weak);
        deserializer.Deserialize(ref weak);
        handle = weak.Server<T>();
    }

    public static void Serialize<TSer, T>(this TSer serializer, ClientHandle<T> handle)
    where TSer : ISerializer<TSer>
    where T : struct, IEntity<T>
    {
        serializer.Serialize((Handle)handle);
    }

    public static void Deserialize<TDeser, T>(this TDeser deserializer, ref ClientHandle<T> handle)
        where TDeser : IDeserializer<TDeser>
        where T : struct, IEntity<T>
    {
        Unsafe.SkipInit(out Handle weak);
        deserializer.Deserialize(ref weak);
        handle = weak.Client<T>();
    }

    public static void Serialize<TSer, T>(this TSer serializer, Maybe<T> maybe)
    where TSer : ISerializer<TSer>
    where T : struct, IEntity<T>
    {
        serializer.Serialize((Handle)maybe.Handle);
    }

    public static void Deserialize<TDeser, T>(this TDeser deserializer, ref Maybe<T> maybe)
        where TDeser : IDeserializer<TDeser>
        where T : struct, IEntity<T>
    {
        Unsafe.SkipInit(out Handle<T> weak);
        deserializer.Deserialize(ref weak);
        maybe = weak;
    }
}