using System;

#nullable enable

namespace Notan.Serialization;

[AttributeUsage(AttributeTargets.Struct)]
internal sealed class GenerateSerializationAttribute : Attribute {}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class SerializeAttribute : Attribute
{
    public string? Name { get; }

    public SerializeAttribute(string? name = null)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class HandleIsAttribute : Attribute
{
    public Type Type { get; }
    public bool MakeProperty { get; }

    public HandleIsAttribute(Type type, bool makeProperty = false)
    {
        Type = type;
        MakeProperty = makeProperty;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class SerializesAttribute : Attribute
{
    public Type Type { get; }

    public SerializesAttribute(Type type)
    {
        Type = type;
    }
}