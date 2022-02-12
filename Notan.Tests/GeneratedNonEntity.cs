using Notan.Serialization;

namespace Notan.Tests;

[AutoSerialized]
public partial struct GeneratedNonEntity
{
    [AutoSerialize]
    public int X;
    [AutoSerialize("Z")]
    public string Y;
}
