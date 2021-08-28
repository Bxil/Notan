using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System;
using System.Reflection;
using System.Threading.Tasks;

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
            serverWorld = new ServerWorld(0)
            {
                Timestep = TimeSpan.Zero
            };
            serverWorld.AddStorages(Assembly.GetExecutingAssembly());

            clientWorld = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).Result;
            clientWorld.Timestep = TimeSpan.Zero;
            clientWorld.AddStorages(Assembly.GetExecutingAssembly());

            serverWorld.Loop();
        }

        [TestCleanup]
        public void End()
        {
            serverWorld.Exit();
            serverWorld.Loop();
        }

        //TODO: make this test a lot more precise
        [TestMethod]
        public void AddAndDisconnect()
        {
            clientWorld.GetStorageView<ByteEntity>().RequestCreate(new ByteEntity());
            clientWorld.Loop();
            serverWorld.Loop();
            var system = new ByteSystem();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
            clientWorld.Exit();
            clientWorld.Loop();
            serverWorld.Loop();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
            serverWorld.Loop();
            serverWorld.GetStorage<ByteEntity>().Run(ref system);
        }

        struct ByteSystem : ISystem<ByteEntity>
        {
            public void Work(ref ByteEntity entity)
            {
                entity.Handle.Strong<ByteEntity>().UpdateObservers();
            }
        }
    }
}
