namespace Notan
{
    public interface ISystem<TEntity> where TEntity : struct, IEntity
    {
        void Work(ref TEntity entity);
    }

    public interface IViewSystem<TEntity> where TEntity : struct, IEntity
    {
        void Work(ref TEntity entity);
    }
}
