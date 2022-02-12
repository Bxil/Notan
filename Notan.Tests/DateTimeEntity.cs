using Notan.Serialization;
using System;

namespace Notan.Tests;

[AutoSerialize]
public partial struct DateTimeEntity : IEntity<DateTimeEntity>
{
    [AutoSerialize]
    public DateTime DateTime;
}

[Serializes(typeof(DateTime))]
public static class DateTimeSerialization
{
    public static void Serialize<T>(DateTime dateTime, T serializer) where T : ISerializer<T>
    {
        serializer.Write(dateTime.Ticks);
    }

    public static DateTime Deserialize<T>(T deserializer) where T : IDeserializer<T>
    {
        return new DateTime(deserializer.GetInt64());
    }
}