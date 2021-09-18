namespace Notan.Serialization
{
    public interface ISerializerEntry<TEntry, TArray, TObject>
        where TEntry : ISerializerEntry<TEntry, TArray, TObject>
        where TArray : ISerializerArray<TEntry, TArray, TObject>
        where TObject : ISerializerObject<TEntry, TArray, TObject>
    {
        void Write(bool value);
        void Write(byte value);
        void Write(short value);
        void Write(int value);
        void Write(long value);
        void Write(float value);
        void Write(double value);
        void Write(string value);
        TArray WriteArray();
        TObject WriteObject();

        public void Write(Handle handle)
        {
            var arr = WriteArray();
            arr.Next().Write(handle.Index);
            arr.Next().Write(handle.Generation);
            arr.End();
        }
    }

    public interface ISerializerArray<TEntry, TArray, TObject>
        where TEntry : ISerializerEntry<TEntry, TArray, TObject>
        where TArray : ISerializerArray<TEntry, TArray, TObject>
        where TObject : ISerializerObject<TEntry, TArray, TObject>
    {
        TEntry Next();
        void End();
    }

    public interface ISerializerObject<TEntry, TArray, TObject>
        where TEntry : ISerializerEntry<TEntry, TArray, TObject>
        where TArray : ISerializerArray<TEntry, TArray, TObject>
        where TObject : ISerializerObject<TEntry, TArray, TObject>
    {
        TEntry Next(string key);
        void End();
    }
}
