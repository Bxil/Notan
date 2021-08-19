using Notan.Serialization;

namespace Notan
{
    public interface IEntity
    {
        Handle Handle { get; set; }

        void Serialize<T>(T serializer, bool nodelta) where T : ISerializer;
        void Deserialize<T>(T deserializer) where T : IDeserializer<T>;
        void PostUpdate() { }
    }
}
