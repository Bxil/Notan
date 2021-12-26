using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Tests
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

            _ = serverWorld.Tick();
        }

        [TestCleanup]
        public void End()
        {
            serverWorld.Exit();
            _ = serverWorld.Tick();
        }

        //TODO: make this test a lot more precise
        [TestMethod]
        public void AddAndDisconnect()
        {
            clientWorld.GetStorage<ByteEntity>().RequestCreate(new ByteEntity());
            _ = clientWorld.Tick();
            _ = serverWorld.Tick();
            var system = new ByteSystem();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
            clientWorld.Exit();
            _ = clientWorld.Tick();
            _ = serverWorld.Tick();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
            _ = serverWorld.Tick();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
        }

        struct ByteSystem : IServerSystem<ByteEntity>
        {
            public void Work(ServerHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                handle.UpdateObservers();
            }
        }
    }
}
