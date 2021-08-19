using Notan;
using Notan.Serialization;
using System;
using System.IO;
using System.Text.Json;

namespace Test
{
    struct EntityA : IEntity
    {
        public Handle Handle { get; set; }

        public string Value;


        public void Deserialize<T>(T deserializer) where T : IDeserializer<T>
        {
            Value = deserializer.GetEntry(nameof(Value)).ReadString();
        }

        public void Serialize<T>(T serializer, bool nodelta) where T : ISerializer
        {
            serializer.Write(nameof(Value), Value);
        }
    }

    struct SystemA : ISystem<EntityA>
    {
        public void Work(Storage<EntityA> storage, ref EntityA entity)
        {
            Console.WriteLine(entity.Value);
        }
    }

    class Program
    {
        static void Main()
        {
            var world = new ServerWorld(0);
            world.AddStorage<EntityA>();
            var storage = world.GetStorage<EntityA>();

            ref var one = ref storage.Create();
            one.Value = "Alpha";
            ref var two = ref storage.Create();
            two.Value = "Beta";
            storage.Create().Value = "Gamma";

            var systemA = new SystemA();
            storage.Run(ref systemA);
            Console.WriteLine();

            storage.Destroy(one.Handle);
            storage.Destroy(two.Handle);

            storage.Run(ref systemA);
            Console.WriteLine();

            JsonConsole(world);

            storage.Create().Value = "Delta";
            storage.Create().Value = "Epsilon";
            storage.Create().Value = "Zeta";
            storage.Run(ref systemA);
            Console.WriteLine();

            JsonConsole(world);

            string save =
@"
{
    ""Test.EntityA"": [
        {
            ""_gen"": 0,
            ""_alive"": true,
            ""Value"": ""Alpha""
        },
        {
            ""_gen"": 42,
            ""_alive"": false
        },
        {
            ""_gen"": 12,
            ""_alive"": true,
            ""Value"": ""Beta""
        },
        {
            ""_gen"": 12,
            ""_alive"": true,
            ""Value"": ""Gamma""
        },
        {
            ""_gen"": 12,
            ""_alive"": false
        }
    ]
}
";
            world.Deserialize(new JsonDeserializer(JsonDocument.Parse(save).RootElement));

            JsonConsole(world);
        }

        private static void JsonConsole(World world)
        {
            var mem = new MemoryStream();
            {
                using var writer = new Utf8JsonWriter(mem, new JsonWriterOptions { Indented = true });
                world.Serialize(new Notan.Serialization.JsonSerializer(writer));
            }
            mem.Position = 0;
            Console.WriteLine(new StreamReader(mem).ReadToEnd());
            Console.WriteLine();
        }
    }
}
