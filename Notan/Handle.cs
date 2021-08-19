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

        public ref T Get<T>() where T : struct, IEntity => ref Unsafe.As<StorageBase<T>>(storage).Get(Index, Generation);

        public void Alive<T>() where T : struct, IEntity => Unsafe.As<StorageBase<T>>(storage).Alive(Index, Generation);

        public void Destroy<T>() where T : struct, IEntity => GetStorage<T>().Destroy(Index, Generation);

        public void AddObserver<T>(Client client) where T : struct, IEntity => GetStorage<T>().AddObserver(Index, Generation, client);

        public void RemoveObserver<T>(int index, int generation, Client client) where T : struct, IEntity => GetStorage<T>().RemoveObserver(index, generation, client);

        public void UpdateObservers<T>() where T : struct, IEntity => GetStorage<T>().UpdateObservers(Index, Generation);

        public void MakeAuthority<T>(Client? client) where T : struct, IEntity => GetStorage<T>().MakeAuthority(Index, Generation, client);

        public Client? GetAuthority<T>() where T : struct, IEntity => GetStorage<T>().GetAuthority(Index, Generation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Storage<T> GetStorage<T>() where T : struct, IEntity => Unsafe.As<Storage<T>>(storage);
    }
}
