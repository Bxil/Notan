using Notan.Serialization;
using System.Runtime.CompilerServices;

namespace Notan
{
    public struct ListEntity<T> : IEntity where T : IEntity
    {
        public Handle Handle { get; set; }

        private Handle? next;
        private Handle item; //The head has no item!

        public void Add(Handle item)
        {
            ref var listEntity = ref Unsafe.As<Storage<ListEntity<T>>>(Handle.Storage).Create();
            listEntity.item = item;
            listEntity.next = next;

            next = listEntity.Handle;
        }

        public void Serialize<TSer>(TSer serializer) where TSer : ISerializer
        {
            serializer.Write("hasNext", next.HasValue);
            if (next.HasValue)
            {
                serializer.Write(nameof(next), next.Value);
            }
            serializer.Write(nameof(item), item);
        }

        public void Deserialize<TDeser>(TDeser deserializer) where TDeser : IDeserializer<TDeser>
        {
            if (deserializer.GetEntry("hasNext").ReadBool())
            {
                next = deserializer.GetEntry(nameof(next)).ReadHandle();
            }
            item = deserializer.GetEntry(nameof(item)).ReadHandle();
        }

        void IEntity.OnDestroy()
        {
            if (next.HasValue)
            {
                next.Value.Strong<ListEntity<T>>().Destroy();
            }
        }

        public Enumerator GetEnumerator() => new(Handle, next);

        public struct Enumerator
        {
            private Handle current;
            private Handle? next;

            public Holder Current { get; private set; }

            internal Enumerator(Handle head, Handle? next)
            {
                current = head;
                this.next = next;
                Current = new();
            }

            public bool MoveNext()
            {
                if (!next.HasValue)
                {
                    return false;
                }
                var last = this.current;
                this.current = next.Value;
                ref var current = ref this.current.Strong<ListEntity<T>>().Get();
                next = current.next;

                Current = new Holder(last, this.current, current.item);

                return true;
            }

            public struct Holder
            {
                private readonly Handle last;
                private readonly Handle current;
                public Handle Item { get; }

                internal Holder(Handle last, Handle current, Handle item)
                {
                    this.last = last;
                    this.current = current;
                    Item = item;
                }

                public void Remove()
                {
                    var currentStrong = this.current.Strong<ListEntity<T>>();
                    ref var last = ref this.last.Strong<ListEntity<T>>().Get();
                    ref var current = ref currentStrong.Get();
                    last.next = current.next;
                    current.next = null;
                    currentStrong.Destroy();
                }
            }
        }
    }
}
