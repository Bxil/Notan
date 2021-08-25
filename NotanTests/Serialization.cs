using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notan.Serialization;
using System;
using System.IO;
using System.Text.Json;

namespace Notan.Testing
{
    [TestClass]
    public class Serialization
    {
        private ServerWorld world;

        private Storage<ByteEntity> bytestorage;
        private Storage<ListEntity> liststorage;

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
            ""_gen"": 0,
            ""_alive"": true,
            ""Value"": 2
        },
        {
            ""_gen"": 42,
            ""_alive"": false
        },
        {
            ""_gen"": 12,
            ""_alive"": true,
            ""Value"": 3
        },
        {
            ""_gen"": 12,
            ""_alive"": true,
            ""Value"": 5
        },
        {
            ""_gen"": 12,
            ""_alive"": false
        }
    ],
    ""Notan.ListEntity"": [
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
