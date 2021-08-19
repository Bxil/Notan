namespace Notan
{
    public interface ISystem<TEntity> where TEntity : struct, IEntity
    {
        void Work(Storage<TEntity> storage, ref TEntity entity);
    }

    public interface IViewSystem<TEntity> where TEntity : struct, IEntity
    {
        void Work(StorageView<TEntity> storage, ref TEntity entity);
    }
}
