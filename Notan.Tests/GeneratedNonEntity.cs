using Notan.Serialization;
using System;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct GeneratedNonEntity : ISerializable
{
    [Serialize]
    public int X;
    [Serialize("Z")]
    public string Y;
    [Serialize]
    public DateTime W;
    [Serialize]
    public Inner SomeSerializable;
}

[GenerateSerialization]
public partial struct Inner : ISerializable { }