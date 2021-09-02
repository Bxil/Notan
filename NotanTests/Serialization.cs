using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Reflection;
using Notan.Serialization;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Notan.Testing
{
    [TestClass]
    public class Serialization
    {
        private ServerWorld world;

        private Storage<ByteEntity> bytestorage;

        [TestInitialize]
        public void Init()
        {
            world = new ServerWorld(0)
            {
                Timestep = TimeSpan.Zero
            };
            world.AddStorages(Assembly.GetExecutingAssembly());
            bytestorage = world.GetStorage<ByteEntity>();
        }

        [TestCleanup]
        public void End()
        {
            world.Exit();
            world.Loop();
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
    ],
    ""NoSuchStorageLikeMe"": [
    ]
}
";

            world.Deserialize(new JsonDeserializer(world, JsonDocument.Parse(jsonsave).RootElement));

            //TODO
        }

        [TestMethod]
        public void JsonSerialize()
        {
            var mem = new MemoryStream();
            {
                using var writer = new Utf8JsonWriter(mem, new JsonWriterOptions { Indented = true });
                world.Serialize(new Notan.Serialization.JsonSerializer(writer));
            }
            mem.Position = 0;
            new StreamReader(mem).ReadToEnd();
            //TODO
        }
    }
}
