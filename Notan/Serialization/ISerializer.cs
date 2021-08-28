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
        void Write(string name, long value);
        void Write(string name, string value);
    }

    public static class SerializerExtensions
    {
        public static void Write<T>(this T serializer, string name, Handle handle) where T : ISerializer
        {
            serializer.WriteEntry(name);
            serializer.BeginObject();
            serializer.Write("storage", handle.Storage.Id);
            serializer.Write("index", handle.Index);
            serializer.Write("gen", handle.Generation);
            serializer.EndObject();
        }
    }
}
