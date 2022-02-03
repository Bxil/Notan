using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Notan;

public readonly struct Handle : IEquatable<Handle>
{
    internal readonly Storage? Storage;

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

    public static bool operator ==(Handle a, Handle b)
    {
        return a.Storage == b.Storage && a.Index == b.Index && a.Generation == b.Generation;
    }

    public static bool operator !=(Handle a, Handle b)
    {
        return a.Storage != b.Storage || a.Index != b.Index || a.Generation != b.Generation;
    }

    public bool Equals(Handle other) => this == other;
    public override int GetHashCode() => Index;
}

//Beware of https://github.com/dotnet/runtime/issues/6924
public readonly struct Handle<T> : IEquatable<Handle<T>> where T : struct, IEntity<T>
{
    internal readonly Storage<T>? Storage;

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
        Debug.Assert(Storage is null or ServerStorage<T>);
        return new(Unsafe.As<ServerStorage<T>>(Storage), Index, Generation);
    }

    public ClientHandle<T> Client()
    {
        Debug.Assert(Storage is null or ClientStorage<T>);
        return new(Unsafe.As<ClientStorage<T>>(Storage), Index, Generation);
    }

    public static implicit operator Handle(Handle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public static bool operator ==(Handle<T> a, Handle<T> b)
    {
        return a.Storage == b.Storage && a.Index == b.Index && a.Generation == b.Generation;
    }

    public static bool operator !=(Handle<T> a, Handle<T> b)
    {
        return a.Storage != b.Storage || a.Index != b.Index || a.Generation != b.Generation;
    }

    public bool Equals(Handle<T> other) => this == other;
    public override int GetHashCode() => Index;
}

public readonly struct ServerHandle<T> : IEquatable<ServerHandle<T>> where T : struct, IEntity<T>
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

    public bool Alive() => Storage?.Alive(Index, Generation) ?? false;

    public void Destroy() => Storage!.Destroy(Index, Generation);

    public void AddObserver(Client client) => Storage!.AddObserver(Index, Generation, client);

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

    public static bool operator ==(ServerHandle<T> a, ServerHandle<T> b) => (Handle<T>)a == b;

    public static bool operator !=(ServerHandle<T> a, ServerHandle<T> b) => (Handle<T>)a != b;

    public bool Equals(ServerHandle<T> other) => this == other;
    public override int GetHashCode() => Index;
}

public readonly struct ClientHandle<T> : IEquatable<ClientHandle<T>> where T : struct, IEntity<T>
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

    public bool Alive() => Storage?.Alive(Index, Generation) ?? false;

    public void Forget() => Storage!.Forget(Index, Generation);

    public void RequestDestroy() => Storage!.RequestDestroy(Index, Generation);

    public void RequestUpdate() => Storage!.RequestUpdate(Index, Generation, Get());

    public void RequestUpdate(T entity) => Storage!.RequestUpdate(Index, Generation, entity);

    public static implicit operator Handle(ClientHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public static implicit operator Handle<T>(ClientHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

    public static bool operator ==(ClientHandle<T> a, ClientHandle<T> b) => (Handle<T>)a == b;

    public static bool operator !=(ClientHandle<T> a, ClientHandle<T> b) => (Handle<T>)a != b;

    public bool Equals(ClientHandle<T> other) => this == other;
    public override int GetHashCode() => Index;
}
