using Notan.Serialization;
using System;
using System.Runtime.CompilerServices;

namespace Notan;

public record struct Handle : ISerializable
{
    public Storage? Storage { get; }

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

    void ISerializable.Serialize<T>(T serializer)
    {
        serializer.ArrayBegin();
        if (Storage == null)
        {
            serializer.ArrayNext().Serialize(0);
            serializer.ArrayNext().Serialize(0);
            serializer.ArrayNext().Serialize(0);
        }
        else
        {
            serializer.ArrayNext().Serialize(Storage.Id);
            serializer.ArrayNext().Serialize(Index);
            serializer.ArrayNext().Serialize(Generation);
        }
        serializer.ArrayEnd();
    }

    void ISerializable.Deserialize<T>(T deserializer)
    {
        deserializer.ArrayBegin();
        Unsafe.SkipInit(out int storageid);
        deserializer.ArrayNext().Deserialize(ref storageid);
        Unsafe.SkipInit(out int index);
        deserializer.ArrayNext().Deserialize(ref index);
        Unsafe.SkipInit(out int generation);
        deserializer.ArrayNext().Deserialize(ref generation);
        var storages = deserializer.World.IdToStorage.AsSpan();
        this = new Handle(storageid > 0 && storageid < storages.Length ? storages[storageid] : null, index, generation);
        _ = deserializer.ArrayTryNext(); //consume the end marker
    }
}

//Beware of https://github.com/dotnet/runtime/issues/6924
public record struct Handle<T> : ISerializable where T : struct, IEntity<T>
{
    public Storage<T>? Storage { get; }

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

    void ISerializable.Serialize<TSer>(TSer serializer)
    {
        serializer.Serialize((Handle)this);
    }

    void ISerializable.Deserialize<TDeser>(TDeser deserializer)
    {
        Unsafe.SkipInit(out Handle weak);
        deserializer.Deserialize(ref weak);
        this = weak.Strong<T>();
    }
}

public record struct ServerHandle<T> : ISerializable where T : struct, IEntity<T>
{
    public ServerStorage<T>? Storage { get; }

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

    void ISerializable.Serialize<TSer>(TSer serializer)
    {
        serializer.Serialize((Handle)this);
    }

    void ISerializable.Deserialize<TDeser>(TDeser deserializer)
    {
        Unsafe.SkipInit(out Handle weak);
        deserializer.Deserialize(ref weak);
        this = weak.Server<T>();
    }
}

public record struct ClientHandle<T> : ISerializable where T : struct, IEntity<T>
{
    public ClientStorage<T>? Storage { get; }

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

    public void RequestUpdate() => Storage!.RequestUpdate(Index, Generation, ref Get());

    public void RequestUpdate(T entity) => Storage!.RequestUpdate(Index, Generation, ref entity);

    public static implicit operator Handle(ClientHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public static implicit operator Handle<T>(ClientHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    void ISerializable.Serialize<TSer>(TSer serializer)
    {
        serializer.Serialize((Handle)this);
    }

    void ISerializable.Deserialize<TDeser>(TDeser deserializer)
    {
        Unsafe.SkipInit(out Handle weak);
        deserializer.Deserialize(ref weak);
        this = weak.Client<T>();
    }
}

public struct Maybe<T> : ISerializable where T : struct, IEntity<T>
{
    private Handle<T> handle;

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

    void ISerializable.Serialize<TSer>(TSer serializer)
    {
        serializer.Serialize((Handle)handle);
    }

    void ISerializable.Deserialize<TDeser>(TDeser deserializer)
    {
        Unsafe.SkipInit(out Handle<T> weak);
        deserializer.Deserialize(ref weak);
        this = weak;
    }
}