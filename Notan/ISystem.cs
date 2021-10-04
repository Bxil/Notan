namespace Notan
{
    public interface ISystem<TEntity> where TEntity : struct, IEntity<TEntity>
    {
        void Work(ServerHandle<TEntity> handle, ref TEntity entity);
    }

    public interface IViewSystem<TEntity> where TEntity : struct, IEntity<TEntity>
    {
        void Work(ClientHandle<TEntity> handle, ref TEntity entity);
    }
}
