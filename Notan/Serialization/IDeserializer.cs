using System.Diagnostics;

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

        public Handle ReadHandle<TEntity>() where TEntity : struct, IEntity
        {
            Debug.Assert(2 == BeginArray());
            return new Handle(World.GetStorageBase<TEntity>(), NextArrayElement().ReadInt32(), NextArrayElement().ReadInt32());
        }
    }
}
