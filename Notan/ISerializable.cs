using Notan.Serialization;

namespace Notan;

public interface ISerializable
{
    void Serialize<TSer>(TSer serializer) where TSer : ISerializer<TSer>;
    void Deserialize<TDeser>(TDeser deserializer) where TDeser : IDeserializer<TDeser>;
}
