using System.Runtime.CompilerServices;

namespace Notan
{
    //TODO: Make this a generic when https://github.com/dotnet/runtime/issues/6924 is finally fixed.
    public readonly struct Handle
    {
        internal readonly Storage Storage;

        public readonly int Index;
        public readonly int Generation;

        internal Handle(Storage storage, int index, int generation)
        {
            Storage = storage;
            Index = index;
            Generation = generation;
        }

        public StrongHandle<T> Strong<T>() where T : struct, IEntity => new((Storage<T>)Storage, Index, Generation);
        public StrongHandle<T> StrongUnsafe<T>() where T : struct, IEntity => new(Unsafe.As<Storage<T>>(Storage), Index, Generation);

        public ViewHandle<T> View<T>() where T : struct, IEntity => new((StorageView<T>)Storage, Index, Generation);
        public ViewHandle<T> ViewUnsafe<T>() where T : struct, IEntity => new(Unsafe.As<StorageView<T>>(Storage), Index, Generation);

        public static bool operator ==(Handle a, Handle b)
        {
            return a.Storage == b.Storage && a.Index == b.Index && a.Generation == b.Generation;
        }

        public static bool operator !=(Handle a, Handle b)
        {
            return a.Storage != b.Storage || a.Index != b.Index || a.Generation != b.Generation;
        }
    }

    public readonly ref struct StrongHandle<T> where T : struct, IEntity
    {
        public readonly Storage<T> Storage;

        public readonly int Index;
        public readonly int Generation;

        internal StrongHandle(Storage<T> storage, int index, int generation)
        {
            Storage = storage;
            Index = index;
            Generation = generation;
        }

        public ref T Get() => ref Storage.Get(Index, Generation);

        public bool Alive() => Storage.Alive(Index, Generation);

        public void Destroy() => Storage.Destroy(Index, Generation);

        public void AddObserver(Client client) => Storage.AddObserver(Index, Generation, client);

        public void RemoveObserver(Client client) => Storage.RemoveObserver(Index, Generation, client);

        public void UpdateObservers() => Storage.UpdateObservers(Index, Generation);

        public void WipeObservers() => Storage.WipeObservers(Index, Generation);

        public Client? Authority
        {
            get => Storage.GetAuthority(Index, Generation);
            set => Storage.SetAuthority(Index, Generation, value);
        }

        public static implicit operator Handle(StrongHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

        public static bool operator ==(StrongHandle<T> a, StrongHandle<T> b) => (Handle)a == b;

        public static bool operator !=(StrongHandle<T> a, StrongHandle<T> b) => (Handle)a != b;
    }

    public readonly ref struct ViewHandle<T> where T : struct, IEntity
    {
        public readonly StorageView<T> Storage;

        public readonly int Index;
        public readonly int Generation;

        internal ViewHandle(StorageView<T> storage, int index, int generation)
        {
            Storage = storage;
            Index = index;
            Generation = generation;
        }

        public ref T Get() => ref Storage.Get(Index, Generation);

        public bool Alive() => Storage.Alive(Index, Generation);

        public void RequestDestroy() => Storage.RequestDestroy(this);

        public void RequestUpdate() => Storage.RequestUpdate(this, Get());

        public void RequestUpdate(T entity) => Storage.RequestUpdate(this, entity);

        public static implicit operator Handle(ViewHandle<T> handle) => new(handle.Storage, handle.Index, handle.Generation);

        public static bool operator ==(ViewHandle<T> a, ViewHandle<T> b) => (Handle)a == b;

        public static bool operator !=(ViewHandle<T> a, ViewHandle<T> b) => (Handle)a != b;
    }
}