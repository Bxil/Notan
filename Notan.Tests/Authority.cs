using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using System.Reflection;

namespace Notan.Tests;

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
        var storage1 = clientWorld1.GetStorage<ByteEntityPostUpdate>();
        var storage2 = clientWorld2.GetStorage<ByteEntityPostUpdate>();

        storage1.RequestCreate(new ByteEntityPostUpdate { Value = 1 });
        storage2.RequestCreate(new ByteEntityPostUpdate { Value = 3 });
        //Note ByteEntityPostUpdate's PostUpdate.

        _ = clientWorld1.Tick();
        _ = clientWorld2.Tick();

        _ = serverWorld.Tick(); //2, 4

        Assert.AreEqual(6, serverWorld.GetStorage<ByteEntityPostUpdate>().Run(new SumSystem()).Sum);

        _ = clientWorld1.Tick(); //3
        _ = clientWorld2.Tick(); //5

        _ = storage1.Run(new IncSystem()); //4
        _ = storage2.Run(new IncSystem()); //6

        _ = clientWorld1.Tick();
        _ = clientWorld2.Tick();

        _ = serverWorld.Tick(); //5, 7

        Assert.AreEqual(12, serverWorld.GetStorage<ByteEntityPostUpdate>().Run(new SumSystem()).Sum);

        _ = serverWorld.GetStorage<ByteEntityPostUpdate>().Run(new DestroySystem());

        _ = serverWorld.Tick();

        _ = clientWorld1.Tick();
        _ = clientWorld2.Tick();

        Assert.AreEqual(0, storage1.Run(new SumSystem()).Sum);
        Assert.AreEqual(0, storage2.Run(new SumSystem()).Sum);
    }

    struct IncSystem : IClientSystem<ByteEntityPostUpdate>
    {
        void IClientSystem<ByteEntityPostUpdate>.Work(ClientHandle<ByteEntityPostUpdate> handle, ref ByteEntityPostUpdate entity)
        {
            handle.RequestUpdate(new ByteEntityPostUpdate { Value = (byte)(entity.Value + 1) });
        }
    }

    struct SumSystem : IServerSystem<ByteEntityPostUpdate>, IClientSystem<ByteEntityPostUpdate>
    {
        public int Sum;

        void IServerSystem<ByteEntityPostUpdate>.Work(ServerHandle<ByteEntityPostUpdate> handle, ref ByteEntityPostUpdate entity)
        {
            Sum += entity.Value;
        }

        void IClientSystem<ByteEntityPostUpdate>.Work(ClientHandle<ByteEntityPostUpdate> handle, ref ByteEntityPostUpdate entity)
        {
            Sum += entity.Value;
        }
    }

    struct DestroySystem : IServerSystem<ByteEntityPostUpdate>
    {
        void IServerSystem<ByteEntityPostUpdate>.Work(ServerHandle<ByteEntityPostUpdate> handle, ref ByteEntityPostUpdate entity)
        {
            handle.Destroy();
        }
    }
}
