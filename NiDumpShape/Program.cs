using System.IO;
using NiDump;

namespace NiDumpShape
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: NiDumpShape <nif file>");
                return;
            }

            string source_file = args[0];

            Program program = new Program();
            program.Load(source_file);
        }

        public void Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                Load(source_stream);
        }

        public void Load(Stream source_stream)
        {
            NiHeader header = GetHeader(source_stream);
            header.Dump();

            int bt_NiTriShapeData = header.GetBlockTypeIdxByName("NiTriShapeData");
            int bt_BSLightingShaderProperty = header.GetBlockTypeIdxByName("BSLightingShaderProperty");
            int bt_BSShaderTextureSet = header.GetBlockTypeIdxByName("BSShaderTextureSet");
            int bt_NiSkinInstance = header.GetBlockTypeIdxByName("NiSkinInstance");
            int bt_NiSkinPartition = header.GetBlockTypeIdxByName("NiSkinPartition");

            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                if (header.blocks[i].type == bt_NiTriShapeData)
                {
                    NiTriShapeData triShapeData = GetObject<NiTriShapeData>(header, i);
                    triShapeData.Dump();
                }
                if (header.blocks[i].type == bt_BSLightingShaderProperty)
                {
                    BSLightingShaderProperty lightingShaderProperty = GetObject<BSLightingShaderProperty>(header, i);
                    lightingShaderProperty.Dump();
                }
                if (header.blocks[i].type == bt_BSShaderTextureSet)
                {
                    BSShaderTextureSet shaderTextureSet = GetObject<BSShaderTextureSet>(header, i);
                    shaderTextureSet.Dump();
                }
                if (header.blocks[i].type == bt_NiSkinInstance)
                {
                    NiSkinInstance skinInstance = GetObject<NiSkinInstance>(header, i);
                    skinInstance.Dump();
                    foreach (ObjectRef boneref in skinInstance.bones)
                    {
                        NiNode node = GetObject<NiNode>(header, boneref);
                        System.Console.WriteLine(header.strings[node.name]);
                    }
                }
                if (header.blocks[i].type == bt_NiSkinPartition)
                {
                    NiSkinPartition skinPartition = GetObject<NiSkinPartition>(header, i);
                    skinPartition.Dump();
                }
            }
        }

        //TODO: nif

        static NiHeader GetHeader(Stream source_stream)
        {
            BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            NiHeader header = new NiHeader();
            header.Read(reader);

            //header.Dump();

            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                header.blocks[i].Read(reader);
            }
            return header;
        }

        static T GetObject<T>(NiHeader header, ObjectRef object_ref) where T : NiObject, new()
        {
            //TODO: cmp T and header.block_types[object_ref]

            T instance;

            using (MemoryStream stream = new MemoryStream(header.blocks[object_ref].data))
            {
                BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.Default);

                instance = new T();
                instance.Read(reader);
            }
            return instance;
        }
    }
}
