namespace Notan.Serialization
{
    public static class WorldExtensions
    {
        public static void Serialize(this ServerWorld world, JsonSerializerEntry entry)
        {
            world.Serialize<JsonSerializerEntry, JsonSerializerArray, JsonSerializerObject>(entry);
        }

        public static void Deserialize(this ServerWorld world, JsonDeserializerEntry entry)
        {
            world.Deserialize<JsonDeserializerEntry, JsonDeserializerArray, JsonDeserializerObject>(entry);
        }

        public static void Serialize(this ServerWorld world, BinarySerializerEntry entry)
        {
            world.Serialize<BinarySerializerEntry, BinarySerializerArray, BinarySerializerObject>(entry);
        }

        public static void Deserialize(this ServerWorld world, BinaryDeserializerEntry entry)
        {
            world.Deserialize<BinaryDeserializerEntry, BinaryDeserializerArray, BinaryDeserializerObject>(entry);
        }
    }
}
