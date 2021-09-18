using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using Notan.Serialization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Notan.Testing
{
    [TestClass]
    public class Serialization
    {
        private ServerWorld world;

        [TestInitialize]
        public void Init()
        {
            world = new ServerWorld(0);
            world.AddStorages(Assembly.GetExecutingAssembly());
        }

        [TestCleanup]
        public void End()
        {
            world.Exit();
            world.Tick();
        }

        [TestMethod]
        public void JsonDeserialize()
        {
            const string jsonsave = @"
{
    ""Notan.Testing.ByteEntity"": [
        {
            ""$gen"": 0,
            ""Value"": 2
        },
        {
            ""$gen"": 42,
            ""$dead"": """"
        },
        {
            ""$gen"": 12,
            ""Value"": 3
        },
        {
            ""$gen"": 12,
            ""Value"": 5
        },
        {
            ""$gen"": 12,
            ""$dead"": """"
        }
    ]
}
";

            var mem = new MemoryStream();
            var writer = new StreamWriter(mem);
            writer.Write(jsonsave);
            writer.Flush();
            mem.Position = 0;
            world.Deserialize(new JsonDeserializerEntry(world, mem));

            //TODO
        }

        [TestMethod]
        public void JsonSerialize()
        {
            var mem = new MemoryStream();
            {
                using var writer = new Utf8JsonWriter(mem, new JsonWriterOptions { Indented = true });
                world.Serialize(new JsonSerializerEntry(writer));
            }
            mem.Position = 0;
            new StreamReader(mem).ReadToEnd();
            //TODO
        }

        [TestMethod]
        public void LargeJson()
        {
            for (int i = 0; i < 1000; i++)
            {
                world.GetStorage<ByteEntity>().Create(new ByteEntity { Value = (byte)(i % 100) });
            }
            for (int i = 0; i < 100; i++)
            {
                world.GetStorage<HandleEntity>().Create(new HandleEntity { Value = new() });
            }
            var mem = new MemoryStream();
            using (var writer = new Utf8JsonWriter(mem))
            {
                world.Serialize(new JsonSerializerEntry(writer));
            }
            mem.Position = 0;
            world.Deserialize(new JsonDeserializerEntry(world, mem));
        }
    }
}
