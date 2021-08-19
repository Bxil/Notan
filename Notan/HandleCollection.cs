using System;
using System.IO;

namespace Notan
{
    //TODO: Make this a generic when https://github.com/dotnet/runtime/issues/6924 is finally fixed.
    public struct HandleCollection
    {
        private readonly Storage storage;

        private FastList<Handle> handles;
        private FastList<Handle> addedDelta;
        private FastList<Handle> removedDelta;

        public HandleCollection(Storage storage)
        {
            this.storage = storage;
            handles = new();
            addedDelta = new();
            removedDelta = new();
        }

        /// <returns>true if a handle was added.</returns>
        public bool Add(Handle handle)
        {
            var index = handles.IndexOf(handle);
            if (index == -1)
            {
                handles.Add(handle);
                addedDelta.Add(handle);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <returns>true if a handle was removed.</returns>
        public bool Remove(Handle handle)
        {
            if (handles.Remove(handle))
            {
                removedDelta.Add(handle);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ResetDeltas()
        {
            addedDelta.Clear();
            removedDelta.Clear();
        }

        public Span<Handle> AsSpan() => handles.AsSpan();

        public void Encode(BinaryWriter writer, bool newobserver)
        {
            if (newobserver)
            {
                writer.Write(0);
                writer.Write(handles.Count);
                foreach (var handle in handles.AsSpan())
                {
                    writer.Write(handle.Index);
                    writer.Write(handle.Generation);
                }
            }
            else
            {
                writer.Write(removedDelta.Count);
                foreach (var handle in removedDelta.AsSpan())
                {
                    writer.Write(handle.Index);
                    writer.Write(handle.Generation);
                }
                writer.Write(addedDelta.Count);
                foreach (var handle in addedDelta.AsSpan())
                {
                    writer.Write(handle.Index);
                    writer.Write(handle.Generation);
                }
            }
        }

        public void Decode(BinaryReader reader)
        {
            int removedDeltaCount = reader.ReadInt32();
            for (int i = 0; i < removedDeltaCount; i++)
            {
                handles.Remove(new Handle(storage, reader.ReadInt32(), reader.ReadInt32()));
            }
            int addedDeltaCount = reader.ReadInt32();
            handles.EnsureCapacity(handles.Count + addedDeltaCount);
            for (int i = 0; i < addedDeltaCount; i++)
            {
                handles.Add(new Handle(storage, reader.ReadInt32(), reader.ReadInt32()));
            }
        }
    }
}