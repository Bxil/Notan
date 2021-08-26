using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
            world = new ServerWorld(0)
            {
                Timestep = TimeSpan.Zero
            };
            world.AddStorage<ListEntity>();
            world.AddStorages(Assembly.GetExecutingAssembly());
            bytestorage = world.GetStorage<ByteEntity>();

            for (int i = 0; i < byte.MaxValue; i++)
            {
                ref var byteent = ref bytestorage.Create();
                byteent.Value = (byte)i;
                bytehandles[i] = byteent.Handle;
            }

            system = new();
        }

        [TestCleanup]
        public void End()
        {
            world.Exit();
            world.Loop();
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
            int sumBeforeDelete = SumBytes();

            int delindex = 50;
            bytehandles[delindex].Strong<ByteEntity>().Destroy();

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

            world.Loop();

            for (int i = 0; i < bytehandles.Length / 2; i++)
            {
                ref var byteent = ref bytestorage.Create();
                byteent.Value = (byte)i;
                bytehandles[i * 2 + 1] = byteent.Handle;
            }

            for (int i = 0; i < bytehandles.Length; i++)
            {
                ref var byteent = ref bytehandles[i].Strong<ByteEntity>().Get();
                if (i % 2 == 1)
                {
                    Assert.AreEqual(1, byteent.Handle.Generation);
                }
                else
                {
                    Assert.AreEqual(0, byteent.Handle.Generation);
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

            public void Work(ref ByteEntity entity)
            {
                Sum += entity.Value;
                Count += 1;
            }
        }
    }
}
