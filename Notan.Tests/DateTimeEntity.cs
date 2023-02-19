using Notan.Serialization;
using System;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct DateTimeEntity : IEntity<DateTimeEntity>
{
    [Serialize("Timestamp")]
    public DateTime DateTime;
}

public static class DateTimeSerialization
{
    public static void Serialize<T>(this T serializer, in DateTime dateTime) where T : ISerializer<T>
    {
        serializer.Write(dateTime.Ticks);
    }

    public static void Deserialize<T>(this T deserializer, ref DateTime dateTime) where T : IDeserializer<T>
    {
        dateTime = new DateTime(deserializer.GetInt64());
    }
}