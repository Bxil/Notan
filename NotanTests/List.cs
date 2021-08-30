using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;

namespace Notan.Testing
{
    [TestClass]
    public class List
    {

        private ServerWorld world;

        private Storage<ByteEntity> bytestorage;
        private Storage<ListEntity<ByteEntity>> liststorage;

        private readonly Handle[] bytehandles = new Handle[5];

        private Handle head;

        [TestInitialize]
        public void Init()
        {
            world = new ServerWorld(0)
            {
                Timestep = TimeSpan.Zero
            };
            world.AddStorage<ListEntity<ByteEntity>>();
            world.AddStorages(Assembly.GetExecutingAssembly());
            bytestorage = world.GetStorage<ByteEntity>();
            liststorage = world.GetStorage<ListEntity<ByteEntity>>();

            for (byte i = 0; i < bytehandles.Length; i++)
            {
                bytehandles[i] = bytestorage.Create(new ByteEntity { Value = i });
            }


            var head = liststorage.Create();
            head.Add(bytehandles[0].Strong<ByteEntity>());
            head.Add(bytehandles[1].Strong<ByteEntity>());
            head.Add(bytehandles[2].Strong<ByteEntity>());
            this.head = head;

            int sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(3, sum);
        }

        [TestCleanup]
        public void End()
        {
            world.Exit();
            world.Loop();
        }

        [TestMethod]
        public void Append()
        {
            var head = this.head.Strong<ListEntity<ByteEntity>>();
            int sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(3, sum);

            head.Add(bytehandles[3].Strong<ByteEntity>());

            sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(6, sum);

            head.Add(bytehandles[4].Strong<ByteEntity>());

            sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(10, sum);
        }

        [TestMethod]
        public void RemoveFirst()
        {
            var head = this.head.Strong<ListEntity<ByteEntity>>();
            int i = 0;
            foreach (var item in head)
            {
                if (i == 0)
                {
                    item.Remove();
                }
                i++;
            }

            int sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(1, sum);
        }

        [TestMethod]
        public void RemoveMiddle()
        {
            var head = this.head.Strong<ListEntity<ByteEntity>>();
            int i = 0;
            foreach (var item in head)
            {
                if (i == 1)
                {
                    item.Remove();
                }
                i++;
            }

            int sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(2, sum);
        }

        [TestMethod]
        public void RemoveLast()
        {
            var head = this.head.Strong<ListEntity<ByteEntity>>();
            int i = 0;
            foreach (var item in head)
            {
                if (i == 2)
                {
                    item.Remove();
                }
                i++;
            }

            int sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(3, sum);
        }

        [TestMethod]
        public void DestroyHead()
        {
            //We create a new list to ensure destruction doesn't interfere with other lists.
            var newlist = liststorage.Create();
            newlist.Add(bytehandles[4].Strong<ByteEntity>());
            newlist.Add(bytehandles[1].Strong<ByteEntity>());

            head.Strong<ListEntity<ByteEntity>>().Destroy();

            int sum = 0;
            foreach (var item in newlist)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(5, sum);

            var system = new ListSystem();
            liststorage.Run(ref system);
            Assert.AreEqual(3, system.Count);
        }


        private struct ListSystem : ISystem<ListEntity<ByteEntity>>
        {
            public int Count;

            public void Work(StrongHandle<ListEntity<ByteEntity>> handle, ref ListEntity<ByteEntity> entity)
            {
                Count += 1;
            }
        }
    }
}
