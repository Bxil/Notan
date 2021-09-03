using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System;
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

            clientWorld1 = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).Result;
            clientWorld1.AddStorages(Assembly.GetExecutingAssembly());

            clientWorld2 = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).Result;
            clientWorld2.AddStorages(Assembly.GetExecutingAssembly());

            serverWorld.Tick();
        }

        [TestCleanup]
        public void End()
        {
            serverWorld.Exit();
            serverWorld.Tick();
            clientWorld1.Exit();
            clientWorld1.Tick();
            clientWorld2.Exit();
            clientWorld2.Tick();
        }

        [TestMethod]
        public void Updates()
        {
            var system1 = new IncSystem { Storage = clientWorld1.GetStorageView<ByteEntity>() };
            var system2 = new IncSystem { Storage = clientWorld2.GetStorageView<ByteEntity>() };

            system1.Storage.RequestCreate(new ByteEntity { Value = 1 });
            system2.Storage.RequestCreate(new ByteEntity { Value = 3 });

            clientWorld1.Tick();
            clientWorld2.Tick();

            serverWorld.Tick();

            clientWorld1.Tick();
            clientWorld2.Tick();

            system1.Storage.Run(ref system1);
            system2.Storage.Run(ref system2);

            clientWorld1.Tick();
            clientWorld2.Tick();

            serverWorld.Tick();

            var sumSystem = new SumSystem();
            serverWorld.GetStorage<ByteEntity>().Run(ref sumSystem);
            Assert.AreEqual(6, sumSystem.Sum);
        }

        struct IncSystem : IViewSystem<ByteEntity>
        {
            public StorageView<ByteEntity> Storage;

            void IViewSystem<ByteEntity>.Work(ViewHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                handle.RequestUpdate(new ByteEntity { Value = (byte)(entity.Value + 1) });
            }
        }

        struct SumSystem : ISystem<ByteEntity>
        {
            public int Sum;

            void ISystem<ByteEntity>.Work(StrongHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                Sum += entity.Value;
            }
        }
    }
}
