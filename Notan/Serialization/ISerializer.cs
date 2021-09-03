namespace Notan.Serialization
{
    public interface ISerializer<T> where T : ISerializer<T>
    {
        void BeginArray(int length);
        void EndArray();

        void BeginObject();
        void EndObject();

        T Entry(string name);

        void Write(bool value);
        void Write(byte value);
        void Write(short value);
        void Write(int value);
        void Write(long value);
        void Write(float value);
        void Write(double value);
        void Write(string value);
    }

    public static class SerializerExtensions
    {
        public static void Write<T>(this T serializer, Handle handle) where T : ISerializer<T>
        {
            serializer.BeginArray(2);
            serializer.Write(handle.Index);
            serializer.Write(handle.Generation);
            serializer.EndArray();
        }
    }
}
