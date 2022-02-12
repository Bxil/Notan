using Notan.Serialization;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct GeneratedNonEntity
{
    [Serialize]
    public int X;
    [Serialize("Z")]
    public string Y;
}
