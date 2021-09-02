using Notan.Serialization;

namespace Notan
{
    public interface IEntity
    {
        void Serialize<T>(T serializer) where T : ISerializer;
        void Deserialize<T>(T deserializer) where T : IDeserializer<T>;
        void LateDeserialize() { }
        void OnDestroy() { }
    }
}
