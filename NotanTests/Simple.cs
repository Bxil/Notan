using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Testing
{
    [TestClass]
    public class Simple
    {
        private ServerWorld world;

        private ServerStorage<ByteEntity> bytestorage;

        private ByteSystem system;

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
            world = new ServerWorld(0);
            world.AddStorages(Assembly.GetExecutingAssembly());
            bytestorage = world.GetStorage<ByteEntity>();

            for (byte i = 0; i < byte.MaxValue; i++)
            {
                bytehandles[i] = bytestorage.Create(new ByteEntity { Value = i });
            }

            system = new();
        }

        [TestCleanup]
        public void End()
        {
            world.Exit();
            world.Tick();
        }

        [TestMethod]
        public void CheckInit()
        {
            bytestorage.Run(ref system);
            Assert.AreEqual(SumBytes(), system.Sum);
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

            bytestorage.Run(ref system);

            Assert.AreEqual(sumBeforeDelete - 50, system.Sum);
            Assert.AreEqual(system.Count, bytehandles.Length - 1);
        }

        [TestMethod]
        public void Forget()
        {
            const int forindex = 50;

            int sumBeforeDelete = SumBytes();

            Assert.IsTrue(bytehandles[forindex].Server<ByteEntity>().Alive());

            ref var entity = ref bytehandles[forindex].Server<ByteEntity>().Get();

            bytehandles[forindex].Server<ByteEntity>().Forget();

            Assert.AreEqual(50, entity.Value);

            Assert.IsFalse(bytehandles[forindex].Server<ByteEntity>().Alive());

            bytestorage.Run(ref system);

            Assert.AreEqual(sumBeforeDelete - 50, system.Sum);
            Assert.AreEqual(system.Count, bytehandles.Length - 1);
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

            bytestorage.Run(ref system);
            Assert.AreEqual(bytehandles.Length / 2 + 1, system.Count);

            world.Tick();

            for (byte i = 0; i < bytehandles.Length / 2; i++)
            {
                bytehandles[i * 2 + 1] = bytestorage.Create(new ByteEntity { Value = i });
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

            system = new();
            bytestorage.Run(ref system);
            Assert.AreEqual(bytehandles.Length, system.Count);
        }

        struct ByteSystem : IServerSystem<ByteEntity>
        {
            public int Sum;

            public int Count;

            public void Work(ServerHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                Sum += entity.Value;
                Count += 1;
            }
        }
    }
}
