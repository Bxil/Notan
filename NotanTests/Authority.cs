using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Testing
{
    [TestClass]
    public class Authority
    {
        private ServerWorld serverWorld;
        private ClientWorld clientWorld1;
        private ClientWorld clientWorld2;

        [TestInitialize]
        public void Init()
        {
            serverWorld = new ServerWorld(0);
            serverWorld.AddStorages(Assembly.GetExecutingAssembly());

            clientWorld1 = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).AsTask().Result;
            clientWorld1.AddStorages(Assembly.GetExecutingAssembly());

            clientWorld2 = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).AsTask().Result;
            clientWorld2.AddStorages(Assembly.GetExecutingAssembly());

            _ = serverWorld.Tick();
        }

        [TestCleanup]
        public void End()
        {
            serverWorld.Exit();
            _ = serverWorld.Tick();
            clientWorld1.Exit();
            _ = clientWorld1.Tick();
            clientWorld2.Exit();
            _ = clientWorld2.Tick();
        }

        [TestMethod]
        public void Updates()
        {
            var storage1 = clientWorld1.GetStorage<ByteEntity>();
            var storage2 = clientWorld2.GetStorage<ByteEntity>();

            storage1.RequestCreate(new ByteEntity { Value = 1 });
            storage2.RequestCreate(new ByteEntity { Value = 3 });

            _ = clientWorld1.Tick();
            _ = clientWorld2.Tick();

            _ = serverWorld.Tick();

            _ = clientWorld1.Tick();
            _ = clientWorld2.Tick();

            _ = storage1.Run(new IncSystem());
            _ = storage2.Run(new IncSystem());

            _ = clientWorld1.Tick();
            _ = clientWorld2.Tick();

            _ = serverWorld.Tick();

            Assert.AreEqual(6, serverWorld.GetStorage<ByteEntity>().Run(new SumSystem()).Sum);

            _ = serverWorld.GetStorage<ByteEntity>().Run(new DestroySystem());

            _ = serverWorld.Tick();

            _ = clientWorld1.Tick();
            _ = clientWorld2.Tick();

            Assert.AreEqual(0, storage1.Run(new SumSystem()).Sum);
            Assert.AreEqual(0, storage2.Run(new SumSystem()).Sum);
        }

        struct IncSystem : IClientSystem<ByteEntity>
        {
            void IClientSystem<ByteEntity>.Work(ClientHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                handle.RequestUpdate(new ByteEntity { Value = (byte)(entity.Value + 1) });
            }
        }

        struct SumSystem : IServerSystem<ByteEntity>, IClientSystem<ByteEntity>
        {
            public int Sum;

            void IServerSystem<ByteEntity>.Work(ServerHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                Sum += entity.Value;
            }

            void IClientSystem<ByteEntity>.Work(ClientHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                Sum += entity.Value;
            }
        }

        struct DestroySystem : IServerSystem<ByteEntity>
        {
            void IServerSystem<ByteEntity>.Work(ServerHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                handle.Destroy();
            }
        }
    }
}
