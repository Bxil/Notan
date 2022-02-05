using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
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

    struct ByteSystem : IServerSystem<ByteEntityOnDestroy>
    {
        public void Work(ServerHandle<ByteEntityOnDestroy> handle, ref ByteEntityOnDestroy entity)
        {
            handle.UpdateObservers();
        }
    }
}
