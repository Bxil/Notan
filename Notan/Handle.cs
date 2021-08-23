using System.Runtime.CompilerServices;

namespace Notan
{
    //TODO: Make this a generic when https://github.com/dotnet/runtime/issues/6924 is finally fixed.
    public readonly struct Handle
    {
        private readonly Storage storage;

        public readonly int Index;
        public readonly int Generation;

        internal Handle(Storage storage, int index, int generation)
        {
            this.storage = storage;
            Index = index;
            Generation = generation;
        }

        public StrongHandle<T> Strong<T>() where T : struct, IEntity => new(Unsafe.As<StorageBase<T>>(storage), Index, Generation);
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

        public void Alive() => storage.Alive(Index, Generation);

        public void Destroy() => GetStorage().Destroy(Index, Generation);

        public void AddObserver(Client client) => GetStorage().AddObserver(Index, Generation, client);

        public void RemoveObserver(int index, int generation, Client client) => GetStorage().RemoveObserver(index, generation, client);

        public void UpdateObservers() => GetStorage().UpdateObservers(Index, Generation);

        public void MakeAuthority(Client? client) => GetStorage().MakeAuthority(Index, Generation, client);

        public Client? GetAuthority() => GetStorage().GetAuthority(Index, Generation);

        public Storage<T> GetStorage() => Unsafe.As<Storage<T>>(storage);
    }
}
