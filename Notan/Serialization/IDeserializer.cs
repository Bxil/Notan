using System;

namespace Notan.Serialization
{
    public interface IDeserializer<out T> where T : IDeserializer<T>
    {
        public World World { get; }

        int BeginArray();
        T NextArrayElement();
        T GetEntry(string name);
        bool ReadBool();
        byte ReadByte();
        int ReadInt32();
        string ReadString();
    }

    public static class DeserializerExtensions
    {
        public static Handle ReadHandle<TDeser>(this TDeser deserializer) where TDeser : IDeserializer<TDeser>
        {
            int storageid = Math.Clamp(deserializer.ReadInt32(), 0, deserializer.World.IdToStorage.Count - 1);
            return new Handle(deserializer.World.IdToStorage[storageid], deserializer.GetEntry("index").ReadInt32(), deserializer.GetEntry("gen").ReadInt32());
        }
    }
}
