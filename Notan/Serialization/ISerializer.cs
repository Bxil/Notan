using System.Numerics;

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
        public void Write(Matrix4x4 matrix)
        {
            var arr = WriteArray();
            arr.Next().Write(matrix.M11);
            arr.Next().Write(matrix.M12);
            arr.Next().Write(matrix.M13);
            arr.Next().Write(matrix.M14);
            arr.Next().Write(matrix.M21);
            arr.Next().Write(matrix.M22);
            arr.Next().Write(matrix.M23);
            arr.Next().Write(matrix.M24);
            arr.Next().Write(matrix.M31);
            arr.Next().Write(matrix.M32);
            arr.Next().Write(matrix.M33);
            arr.Next().Write(matrix.M34);
            arr.Next().Write(matrix.M41);
            arr.Next().Write(matrix.M42);
            arr.Next().Write(matrix.M43);
            arr.Next().Write(matrix.M44);
            arr.End();
        }

        public void Write(Vector3 vector3)
        {
            var arr = WriteArray();
            arr.Next().Write(vector3.X);
            arr.Next().Write(vector3.Y);
            arr.Next().Write(vector3.Z);
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
