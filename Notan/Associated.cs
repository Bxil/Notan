namespace Notan;

public abstract class Associated<T> where T : struct, IEntity<T>
{
    public abstract void PreUpdate(Handle<T> handle, ref T entity);
    public abstract void PostUpdate(Handle<T> handle, ref T entity);
    public abstract void OnDestroy(Handle<T> handle, ref T entity);
}