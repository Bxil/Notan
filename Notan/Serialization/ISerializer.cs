namespace Notan.Serialization
{
    public interface ISerializer
    {
        void BeginArray(int length);
        void EndArray();

        void BeginObject();
        void EndObject();

        void WriteEntry(string name);

        void Write(string name, bool value);
        void Write(string name, byte value);
        void Write(string name, int value);
        void Write(string name, string value);
    }
}
