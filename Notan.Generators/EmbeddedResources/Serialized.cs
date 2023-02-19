using Notan;
using Notan.Serialization;
using System.IO;

__NAMESPACE__

partial __STRUCTTYPE__ __TYPENAME__
{__PROPERTIES__
    void ISerializable.Serialize<T>(T serializer)
    {
        __SERIALIZE__
    }

    void ISerializable.Deserialize<T>(T deserializer)
    {
        __DESERIALIZE__
    }
}