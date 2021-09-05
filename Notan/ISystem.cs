namespace Notan
{
    public interface ISystem<TEntity> where TEntity : struct, IEntity<TEntity>
    {
        void Work(StrongHandle<TEntity> handle, ref TEntity entity);
    }

    public interface IViewSystem<TEntity> where TEntity : struct, IEntity<TEntity>
    {
        void Work(ViewHandle<TEntity> handle, ref TEntity entity);
    }
}
