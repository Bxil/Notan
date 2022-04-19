namespace Notan.Serialization.Binary;

internal enum BinaryTag : byte
{
    Null,
    Boolean,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
    String,
    ArrayBegin,
    ArrayNext,
    ArrayEnd,
    ObjectBegin,
    ObjectNext,
    ObjectEnd,
}
