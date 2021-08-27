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

        public StrongHandle<T> Strong<T>() where T : struct, IEntity => new(Unsafe.As<StorageBase<T>>(Storage), Index, Generation);

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
        private readonly StorageBase<T> storage;

        public readonly int Index;
        public readonly int Generation;

        internal StrongHandle(StorageBase<T> storage, int index, int generation)
        {
            this.storage = storage;
            Index = index;
            Generation = generation;
        }

        public ref T Get() => ref storage.Get(Index, Generation);

        public bool Alive() => storage.Alive(Index, Generation);

        public void Destroy() => GetStorage().Destroy(Index, Generation);

        public void AddObserver(Client client) => GetStorage().AddObserver(Index, Generation, client);

        public void RemoveObserver(Client client) => GetStorage().RemoveObserver(Index, Generation, client);

        public void UpdateObservers() => GetStorage().UpdateObservers(Index, Generation);

        public Client? Authority
        {
            get => GetStorage().GetAuthority(Index, Generation);
            set => GetStorage().SetAuthority(Index, Generation, value);
        }

        public Storage<T> GetStorage() => Unsafe.As<Storage<T>>(storage);

        public Handle Weak() => new(storage, Index, Generation);

        public static bool operator ==(StrongHandle<T> a, StrongHandle<T> b)
        {
            return a.storage == b.storage && a.Index == b.Index && a.Generation == b.Generation;
        }

        public static bool operator !=(StrongHandle<T> a, StrongHandle<T> b)
        {
            return a.storage != b.storage || a.Index != b.Index || a.Generation != b.Generation;
        }
    }
}