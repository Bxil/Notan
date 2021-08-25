﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Notan.Testing
{
    [TestClass]
    public class List
    {

        private ServerWorld world;

        private Storage<ByteEntity> bytestorage;
        private Storage<ListEntity> liststorage;

        private readonly Handle[] bytehandles = new Handle[5];

        private Handle head;

        [TestInitialize]
        public void Init()
        {
            world = new ServerWorld(0)
            {
                Timestep = TimeSpan.Zero
            };
            world.AddStorage<ByteEntity>();
            world.AddStorage<ListEntity>();
            bytestorage = world.GetStorage<ByteEntity>();
            liststorage = world.GetStorage<ListEntity>();

            for (int i = 0; i < bytehandles.Length; i++)
            {
                ref var byteent = ref bytestorage.Create();
                byteent.Value = (byte)i;
                bytehandles[i] = byteent.Handle;
            }


            ref var head = ref liststorage.Create();
            head.Add(bytehandles[0]);
            head.Add(bytehandles[1]);
            head.Add(bytehandles[2]);
            this.head = head.Handle;

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
            ref var head = ref this.head.Strong<ListEntity>().Get();
            int sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(3, sum);

            head.Add(bytehandles[3]);

            sum = 0;
            foreach (var item in head)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(6, sum);

            head.Add(bytehandles[4]);

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
            ref var head = ref this.head.Strong<ListEntity>().Get();
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
            ref var head = ref this.head.Strong<ListEntity>().Get();
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
            ref var head = ref this.head.Strong<ListEntity>().Get();
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
            ref var newlist = ref liststorage.Create();
            newlist.Add(bytehandles[4]);
            newlist.Add(bytehandles[1]);

            head.Strong<ListEntity>().Destroy();

            int sum = 0;
            foreach (var item in newlist)
            {
                sum += item.Item.Strong<ByteEntity>().Get().Value;
            }
            Assert.AreEqual(5, sum);

            var system = new ListSystem();
            liststorage.Run(ref system);
            Assert.AreEqual(5, system.Count);
        }


        private struct ListSystem : ISystem<ListEntity>
        {
            public int Count;

            public void Work(ref ListEntity entity)
            {
                Count += 1;
            }
        }
    }
}
