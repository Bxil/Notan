using Notan.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Notan;

public readonly record struct Handle
{
    internal readonly Storage? Storage;

    public int Index { get; }
    public int Generation { get; }

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
        Debug.Assert(Storage is null or Storage<T>);
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

    public void Serialize<T>(T serializer) where T : ISerializer<T>
    {
        serializer.ArrayBegin();
        if (Storage == null)
        {
            serializer.ArrayNext().Write(0);
            serializer.ArrayNext().Write(0);
            serializer.ArrayNext().Write(0);
        }
        else
        {
            serializer.ArrayNext().Write(Storage.Id);
            serializer.ArrayNext().Write(Index);
            serializer.ArrayNext().Write(Generation);
        }
        serializer.ArrayEnd();
    }

    public static void Deserialize<T>(ref Handle handle, T deserializer) where T : IDeserializer<T>
    {
        deserializer.ArrayBegin();
        handle = new Handle(deserializer.World.IdToStorage[deserializer.ArrayNext().GetInt32()], deserializer.ArrayNext().GetInt32(), deserializer.ArrayNext().GetInt32());
        _ = deserializer.ArrayTryNext(); //consume the end marker
    }
}

//Beware of https://github.com/dotnet/runtime/issues/6924
public readonly record struct Handle<T> where T : struct, IEntity<T>
{
    internal readonly Storage<T>? Storage;

    public int Index { get; }
    public int Generation { get; }

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
        Debug.Assert(Storage is null or ServerStorage<T>);
        return new(Unsafe.As<ServerStorage<T>>(Storage), Index, Generation);
    }

    public ClientHandle<T> Client()
    {
        Debug.Assert(Storage is null or ClientStorage<T>);
        return new(Unsafe.As<ClientStorage<T>>(Storage), Index, Generation);
    }

    public static implicit operator Handle(Handle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>
        => ((Handle)this).Serialize(serializer);

    public static void Deserialize<TDeserializer>(ref Handle<T> handle, TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>
    {
        Unsafe.SkipInit(out Handle weak);
        Handle.Deserialize(ref weak, deserializer);
        handle = weak.Strong<T>();
    }
}

public readonly record struct ServerHandle<T> where T : struct, IEntity<T>
{
    public readonly ServerStorage<T>? Storage;

    public int Index { get; }
    public int Generation { get; }

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

    public void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>
    => ((Handle)this).Serialize(serializer);

    public static void Deserialize<TDeserializer>(ref ServerHandle<T> handle, TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>
    {
        Unsafe.SkipInit(out Handle weak);
        Handle.Deserialize(ref weak, deserializer);
        handle = weak.Server<T>();
    }
}

public readonly record struct ClientHandle<T> where T : struct, IEntity<T>
{
    public readonly ClientStorage<T>? Storage;

    public int Index { get; }
    public int Generation { get; }

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

    public void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>
        => ((Handle)this).Serialize(serializer);

    public static void Deserialize<TDeserializer>(ref ClientHandle<T> handle, TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>
    {
        Unsafe.SkipInit(out Handle weak);
        Handle.Deserialize(ref weak, deserializer);
        handle = weak.Client<T>();
    }
}

public readonly struct Maybe<T> where T : struct, IEntity<T>
{
    private readonly Handle<T> handle;

    public Maybe(Handle<T> handle) => this.handle = handle;

    public bool Alive() => handle.Storage?.Alive(handle.Index, handle.Generation) ?? false;

    public bool Alive(out Handle<T> handle)
    {
        handle = this.handle;
        return Alive();
    }

    public bool AliveServer(out ServerHandle<T> handle)
    {
        handle = this.handle.Server();
        return Alive();
    }

    public bool AliveClient(out ClientHandle<T> handle)
    {
        handle = this.handle.Client();
        return Alive();
    }

    public static implicit operator Maybe<T>(Handle<T> handle) => new(handle);
    public static implicit operator Maybe<T>(ServerHandle<T> handle) => new(handle);
    public static implicit operator Maybe<T>(ClientHandle<T> handle) => new(handle);

    public void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>
        => handle.Serialize(serializer);

    public static void Deserialize<TDeserializer>(ref Maybe<T> maybe, TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>
    {
        Unsafe.SkipInit(out Handle<T> handle);
        Handle<T>.Deserialize(ref handle, deserializer);
        maybe = handle;
    }
}