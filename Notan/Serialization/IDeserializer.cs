namespace Notan.Serialization
{
    public interface IDeserializer<out T> where T : IDeserializer<T>
    {
        int BeginArray();
        T NextArrayElement();
        T GetEntry(string name);
        bool ReadBool();
        byte ReadByte();
        int ReadInt32();
        string ReadString();
    }
}
