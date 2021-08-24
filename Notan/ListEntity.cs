using Notan.Serialization;
using System.Runtime.CompilerServices;

namespace Notan
{
    struct ListEntity : IEntity
    {
        public Handle Handle { get; set; }

        private Handle previous;
        private Handle next;

        public Handle Item { get; private set; }

        public void Create(Handle item)
        {
            previous = next = Handle;
            Item = item;
        }

        public void Append(Handle item)
        {
            var listEntity = Unsafe.As<Storage<ListEntity>>(Handle.Storage).Create();
            listEntity.Create(item);

            next.Strong<ListEntity>().Get().previous = listEntity.Handle;
            listEntity.previous = Handle;
            listEntity.next = next;
            next = listEntity.Handle;
        }

        public void Serialize<TSer>(TSer serializer) where TSer : ISerializer
        {
            serializer.Write(nameof(previous), previous);
            serializer.Write(nameof(next), next);
            serializer.Write(nameof(Item), Item);
        }

        public void Deserialize<TDeser>(TDeser deserializer) where TDeser : IDeserializer<TDeser>
        {
            previous = deserializer.GetEntry(nameof(previous)).ReadHandle();
            next = deserializer.GetEntry(nameof(next)).ReadHandle();
            Item = deserializer.GetEntry(nameof(Item)).ReadHandle();
        }

        public void OnDestroy()
        {
            if (this.next == Item)
            {
                return;
            }

            var prev = previous.Strong<ListEntity>().Get();
            var next = this.next.Strong<ListEntity>().Get();

            prev.next = this.next;
            next.previous = previous;
        }

        public Enumerator<T> GetEnumerator<T>() where T : struct, IEntity => new(Handle.Strong<ListEntity>());

        public ref struct Enumerator<T> where T : struct, IEntity
        {
            private readonly StrongHandle<ListEntity> first;
            private StrongHandle<ListEntity> current;
            private StrongHandle<ListEntity> next;

            public StrongHandle<T> Current { get; private set; }

            public Enumerator(StrongHandle<ListEntity> first)
            {
                this.first = first;
                next = first;
                current = new();
                Current = new();
            }

            public bool MoveNext()
            {
                if (next == first)
                {
                    return false;
                }
                current = next;
                Current = current.Get().Item.Strong<T>();
                next = next.Get().next.Strong<ListEntity>();
                return true;
            }
        }
    }
}
