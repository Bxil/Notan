namespace Notan;

public interface IEntity<T> : ISerializable where T : struct, IEntity<T>
{
    void PreUpdate(Handle<T> handle) { }
    void PostUpdate(Handle<T> handle) { }
    void OnDestroy() { }
}
