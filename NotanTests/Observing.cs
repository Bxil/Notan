using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Testing
{
    [TestClass]
    public class Observing
    {
        private ServerWorld serverWorld;
        private ClientWorld clientWorld;

        [TestInitialize]
        public void Init()
        {
            serverWorld = new ServerWorld(0);
            serverWorld.AddStorages(Assembly.GetExecutingAssembly());

            clientWorld = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).AsTask().Result;
            clientWorld.AddStorages(Assembly.GetExecutingAssembly());

            serverWorld.Tick();
        }

        [TestCleanup]
        public void End()
        {
            serverWorld.Exit();
            serverWorld.Tick();
        }

        //TODO: make this test a lot more precise
        [TestMethod]
        public void AddAndDisconnect()
        {
            clientWorld.GetStorageView<ByteEntity>().RequestCreate(new ByteEntity());
            clientWorld.Tick();
            serverWorld.Tick();
            var system = new ByteSystem();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
            clientWorld.Exit();
            clientWorld.Tick();
            serverWorld.Tick();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
            serverWorld.Tick();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
        }

        struct ByteSystem : ISystem<ByteEntity>
        {
            public void Work(ServerHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                handle.UpdateObservers();
            }
        }
    }
}
