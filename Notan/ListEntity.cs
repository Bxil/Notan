using Notan.Serialization;

namespace Notan
{
    public struct ListEntity<T> : IEntity where T : struct, IEntity
    {
        private Handle? next;
        private Handle item; //The head has no item!

        internal void Add(Storage<ListEntity<T>> storage, StrongHandle<T> item)
        {
            next = storage.Create(new ListEntity<T> { item = item, next = next });
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

        internal StrongEnumerator GetEnumerator(StrongHandle<ListEntity<T>> handle) => new(handle, next);

        internal ViewEnumerator GetEnumerator(ViewHandle<ListEntity<T>> handle) => new(handle, next);

        public struct StrongEnumerator
        {
            private Handle current;
            private Handle? next;

            public Holder Current { get; private set; }

            internal StrongEnumerator(Handle head, Handle? next)
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

        public struct ViewEnumerator
        {
            private Handle current;
            private Handle? next;

            public Handle Current { get; private set; }

            internal ViewEnumerator(Handle head, Handle? next)
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
                this.current = next.Value;
                ref var current = ref this.current.Strong<ListEntity<T>>().Get();
                next = current.next;

                Current = current.item;

                return true;
            }
        }
    }

    public static class ListEntityExtensions
    {
        public static ListEntity<T>.StrongEnumerator GetEnumerator<T>(this StrongHandle<ListEntity<T>> handle) where T : struct, IEntity
        {
            return handle.Get().GetEnumerator(handle);
        }

        public static ListEntity<T>.ViewEnumerator GetEnumerator<T>(this ViewHandle<ListEntity<T>> handle) where T : struct, IEntity
        {
            return handle.Get().GetEnumerator(handle);
        }

        public static void Add<T>(this StrongHandle<ListEntity<T>> handle, StrongHandle<T> item) where T : struct, IEntity
        {
            handle.Get().Add(handle.Storage, item);
        }
    }
}
