using Notan.Serialization;

namespace Notan
{
    public interface IEntity<T> where T : struct, IEntity<T>
    {
        void Serialize<TSerializer>(TSerializer serializer) where TSerializer : ISerializer<TSerializer>;
        void Deserialize<TDeserializer>(TDeserializer deserializer) where TDeserializer : IDeserializer<TDeserializer>;
        void LateDeserialize(StrongHandle<T> handle) { }
        void LateCreate(StrongHandle<T> handle) { }
        void OnDestroy(StrongHandle<T> handle) { }
    }
}
