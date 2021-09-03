namespace Notan.Serialization
{
    public interface IDeserializer<T> where T : IDeserializer<T>
    {
        public World World { get; }

        int BeginArray();
        T NextArrayElement();
        T Entry(string name);
        bool TryGetEntry(string name, out T entry);
        bool ReadBool();
        byte ReadByte();
        short ReadInt16();
        int ReadInt32();
        long ReadInt64();
        float ReadSingle();
        double ReadDouble();
        string ReadString();
    }

    public static class DeserializerExtensions
    {
        public static Handle ReadHandle<TDeser, TEntity>(this TDeser deserializer) where TDeser : IDeserializer<TDeser> where TEntity : struct, IEntity
        {
            deserializer.BeginArray();
            return new Handle(deserializer.World.GetStorageBase<TEntity>(), deserializer.NextArrayElement().ReadInt32(), deserializer.NextArrayElement().ReadInt32());
        }
    }
}
