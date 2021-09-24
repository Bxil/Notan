using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Testing
{
    [TestClass]
    public class Simple
    {
        private ServerWorld world;

        private Storage<ByteEntity> bytestorage;

        private ByteSystem system;

        private readonly Handle[] bytehandles = new Handle[byte.MaxValue];

        private int SumBytes()
        {
            int sum = 0;
            for (int i = 0; i < bytehandles.Length; i++)
            {
                var strong = bytehandles[i].Strong<ByteEntity>();
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

            Assert.IsTrue(bytehandles[delindex].Strong<ByteEntity>().Alive());

            ref var entity = ref bytehandles[delindex].Strong<ByteEntity>().Get();

            bytehandles[delindex].Strong<ByteEntity>().Destroy();

            Assert.AreEqual(49, entity.Value);

            Assert.IsFalse(bytehandles[delindex].Strong<ByteEntity>().Alive());

            bytestorage.Run(ref system);

            Assert.AreEqual(sumBeforeDelete - 50, system.Sum);
            Assert.AreEqual(system.Count, bytehandles.Length - 1);
        }

        [TestMethod]
        public void Forget()
        {
            const int forindex = 50;

            int sumBeforeDelete = SumBytes();

            Assert.IsTrue(bytehandles[forindex].Strong<ByteEntity>().Alive());

            ref var entity = ref bytehandles[forindex].Strong<ByteEntity>().Get();

            bytehandles[forindex].Strong<ByteEntity>().Forget();

            Assert.AreEqual(50, entity.Value);

            Assert.IsFalse(bytehandles[forindex].Strong<ByteEntity>().Alive());

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
                    bytehandles[i].Strong<ByteEntity>().Destroy();
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
                var bytehandle = bytehandles[i].Strong<ByteEntity>();
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

        struct ByteSystem : ISystem<ByteEntity>
        {
            public int Sum;

            public int Count;

            public void Work(StrongHandle<ByteEntity> handle, ref ByteEntity entity)
            {
                Sum += entity.Value;
                Count += 1;
            }
        }
    }
}
