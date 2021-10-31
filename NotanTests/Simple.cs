using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Testing
{
    [TestClass]
    public class Simple
    {
        private ServerWorld serverWorld;
        private ClientWorld clientWorld;

        private ServerStorage<ByteEntity> serverStorage;
        private ClientStorage<ByteEntity> clientStorage;

        private ServerSystem serverSystem;

        private readonly Handle[] bytehandles = new Handle[byte.MaxValue];

        private int SumBytes()
        {
            int sum = 0;
            for (int i = 0; i < bytehandles.Length; i++)
            {
                var strong = bytehandles[i].Server<ByteEntity>();
                if (strong.Alive())
                {
                    sum += strong.Get().Value;
                }
            }
            return sum;
        }

        [TestInitialize]
        public void Init()
        {
            serverWorld = new ServerWorld(0);
            serverWorld.AddStorages(Assembly.GetExecutingAssembly());
            serverStorage = serverWorld.GetStorage<ByteEntity>();

            clientWorld = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).AsTask().Result;
            clientWorld.AddStorages(Assembly.GetExecutingAssembly());
            clientStorage = clientWorld.GetStorage<ByteEntity>();

            serverWorld.Tick();

            for (byte i = 0; i < byte.MaxValue; i++)
            {
                bytehandles[i] = serverStorage.Create(new ByteEntity { Value = i });
                bytehandles[i].Server<ByteEntity>().AddObserver(serverWorld.Clients[0]);
                bytehandles[i].Server<ByteEntity>().UpdateObservers();
            }

            serverWorld.Tick();
            clientWorld.Tick();

            serverSystem = new();
        }

        [TestCleanup]
        public void End()
        {
            serverWorld.Exit();
            serverWorld.Tick();
        }

        [TestMethod]
        public void CheckInit()
        {
            serverStorage.Run(ref serverSystem);
            Assert.AreEqual(SumBytes(), serverSystem.Sum);
        }

        [TestMethod]
        public void Destroy()
        {
            const int delindex = 50;

            int sumBeforeDelete = SumBytes();

            Assert.IsTrue(bytehandles[delindex].Server<ByteEntity>().Alive());

            ref var entity = ref bytehandles[delindex].Server<ByteEntity>().Get();

            bytehandles[delindex].Server<ByteEntity>().Destroy();

            Assert.AreEqual(49, entity.Value);

            Assert.IsFalse(bytehandles[delindex].Server<ByteEntity>().Alive());

            serverStorage.Run(ref serverSystem);

            Assert.AreEqual(sumBeforeDelete - 50, serverSystem.Sum);
            Assert.AreEqual(serverSystem.Count, bytehandles.Length - 1);
        }

        [TestMethod]
        public void DestroyMany()
        {
            for (int i = 0; i < bytehandles.Length; i++)
            {
                if (i % 2 == 1)
                {
                    bytehandles[i].Server<ByteEntity>().Destroy();
                }
            }

            serverStorage.Run(ref serverSystem);
            Assert.AreEqual(bytehandles.Length / 2 + 1, serverSystem.Count);

            serverWorld.Tick();

            for (byte i = 0; i < bytehandles.Length / 2; i++)
            {
                bytehandles[i * 2 + 1] = serverStorage.Create(new ByteEntity { Value = i });
            }

            for (int i = 0; i < bytehandles.Length; i++)
            {
                var bytehandle = bytehandles[i].Server<ByteEntity>();
                if (i % 2 == 1)
                {
                    Assert.AreEqual(1, bytehandle.Generation);
                }
                else
                {
                    Assert.AreEqual(0, bytehandle.Generation);
                }
            }

            int expected = 0;
            for (int i = 0; i < bytehandles.Length; i++)
            {
                if (i % 2 == 1)
                {
                    expected += i;
                }
                else
                {
                    expected += i / 2;
                }
            }

            Assert.AreEqual(expected, SumBytes());

            serverSystem = new();
            serverStorage.Run(ref serverSystem);
            Assert.AreEqual(bytehandles.Length, serverSystem.Count);
        }

        [TestMethod]
        public void Linger()
        {
            var serverHandle50 = bytehandles[50].Server<ByteEntity>();
            var clientHandle50 = clientStorage.Run(new FindSystem(50)).Handle.Client<ByteEntity>();

            var serverHandle100 = bytehandles[100].Server<ByteEntity>();
            var clientHandle100 = clientStorage.Run(new FindSystem(100)).Handle.Client<ByteEntity>();

            Assert.IsTrue(serverHandle50.Alive());
            Assert.IsTrue(clientHandle50.Alive());
            Assert.IsFalse(clientHandle50.Lingering());

            Assert.IsTrue(serverHandle100.Alive());
            Assert.IsTrue(clientHandle100.Alive());
            Assert.IsFalse(clientHandle100.Lingering());

            serverHandle50.Destroy();
            serverHandle100.ClearObservers();
            serverWorld.Tick();
            clientWorld.Tick();

            Assert.IsFalse(serverHandle50.Alive());
            Assert.IsTrue(clientHandle50.Alive());
            Assert.IsTrue(clientHandle50.Lingering());
            Assert.AreEqual(50, clientHandle50.Get().Value);

            Assert.IsTrue(serverHandle100.Alive());
            Assert.IsTrue(clientHandle100.Alive());
            Assert.IsTrue(clientHandle100.Lingering());
            Assert.AreEqual(100, clientHandle100.Get().Value);
        }

        struct ServerSystem : IServerSystem<ByteEntity>
        {
            public int Sum;

            public int Count;

            public void Work(ServerHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                Sum += entity.Value;
                Count += 1;
            }
        }

        struct FindSystem : IClientSystem<ByteEntity>
        {
            private readonly int num;

            public Handle Handle { get; private set; }

            public FindSystem(int num)
            {
                Handle = default;
                this.num = num;
            }

            public void Work(ClientHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                if (handle.Get().Value == num)
                {
                    Handle = handle;
                }
            }
        }
    }
}
