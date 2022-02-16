using Notan.Serialization;
using System;

namespace Notan.Tests;

[GenerateSerialization]
public partial struct DateTimeEntity : IEntity<DateTimeEntity>
{
    [Serialize("Timestamp")]
    public DateTime DateTime;
}

[Serializes(typeof(DateTime))]
public static class DateTimeSerialization
{
    public static void Serialize<T>(in DateTime dateTime, T serializer) where T : ISerializer<T>
    {
        serializer.Write(dateTime.Ticks);
    }

    public static void Deserialize<T>(ref DateTime dateTime, T deserializer) where T : IDeserializer<T>
    {
        dateTime = new DateTime(deserializer.GetInt64());
    }
}