using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using Notan.Serialization;
using System;
using System.IO;
using System.Reflection;

namespace Notan.Tests;

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

        clientWorld = ClientWorld.StartAsync("localhost", serverWorld.EndPoint.Port).Result;
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
        clientWorld.GetStorage<ByteEntityOnDestroy>().RequestCreate(new ByteEntityOnDestroy());
        _ = clientWorld.Tick();
        _ = serverWorld.Tick();
        var system = new ByteSystem();
        serverWorld.GetStorage<ByteEntityOnDestroy>().Run(ref system);
        clientWorld.Exit();
        _ = clientWorld.Tick();
        _ = serverWorld.Tick();
        serverWorld.GetStorage<ByteEntityOnDestroy>().Run(ref system);
        _ = serverWorld.Tick();
        serverWorld.GetStorage<ByteEntityOnDestroy>().Run(ref system);
    }

    [TestMethod]
    public void Malformed()
    {
        clientWorld.GetStorage<MalformedEntity>().RequestCreate(new MalformedEntity());
        _ = clientWorld.Tick();
        _ = serverWorld.Tick();
        Assert.AreEqual(0, serverWorld.Clients.Length);
    }

    [TestMethod]
    public void MalformedWrong()
    {
        clientWorld.GetStorage<MalformedEntityWrong>().RequestCreate(new MalformedEntityWrong());
        _ = clientWorld.Tick();
        _ = Assert.ThrowsException<Exception>(() => _ = serverWorld.Tick());
    }

    struct ByteSystem : IServerSystem<ByteEntityOnDestroy>
    {
        public void Work(ServerHandle<ByteEntityOnDestroy> handle, ref ByteEntityOnDestroy entity)
        {
            handle.UpdateObservers();
        }
    }

    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    public struct MalformedEntity : IEntity<MalformedEntity>
    {
        void IEntity<MalformedEntity>.Deserialize<TDeser>(Key key, TDeser deserializer)
        {
            throw new IOException();
        }

        void IEntity<MalformedEntity>.Serialize<TSer>(TSer serializer)
        {
            serializer.ObjectNext("a").Write("b");
        }
    }

    [StorageOptions(ClientAuthority = ClientAuthority.Unauthenticated)]
    public struct MalformedEntityWrong : IEntity<MalformedEntityWrong>
    {
        void IEntity<MalformedEntityWrong>.Deserialize<TDeser>(Key key, TDeser deserializer)
        {
            throw new Exception();
        }

        void IEntity<MalformedEntityWrong>.Serialize<TSer>(TSer serializer)
        {
            serializer.ObjectNext("a").Write("b");
        }
    }

}