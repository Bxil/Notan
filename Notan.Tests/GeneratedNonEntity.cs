using Notan.Serialization;
using System;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct GeneratedNonEntity
{
    [Serialize]
    public int X;
    [Serialize("Z")]
    public string Y;
    [Serialize]
    public DateTime W;
}
