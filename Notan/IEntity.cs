namespace Notan;

public interface IEntity<T> : ISerializable where T : struct, IEntity<T> { }