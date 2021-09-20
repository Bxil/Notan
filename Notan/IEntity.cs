using Notan.Serialization;

namespace Notan
{
    public interface IEntity<T> where T : struct, IEntity<T>
    {
        void Serialize<TEntry, TArray, TObject>(TObject obj)
            where TEntry : ISerializerEntry<TEntry, TArray, TObject>
            where TArray : ISerializerArray<TEntry, TArray, TObject>
            where TObject : ISerializerObject<TEntry, TArray, TObject>;
        void Deserialize<TEntry, TArray, TObject>(KeyComparison comparison, TEntry entry)
            where TEntry : IDeserializerEntry<TEntry, TArray, TObject>
            where TArray : IDeserializerArray<TEntry, TArray, TObject>
            where TObject : IDeserializerObject<TEntry, TArray, TObject>;
        void LateDeserialize(StrongHandle<T> handle) { }
        void LateCreate(StrongHandle<T> handle) { }
        void OnDestroy(StrongHandle<T> handle) { }
    }
}
