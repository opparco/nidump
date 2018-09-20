using System.IO;
using NiDump;

namespace NiDumpShape
{
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

        NiHeader header;

        public void Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                Load(source_stream);
        }

        public void Load(Stream source_stream)
        {
            {
                BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

                header = new NiHeader();
                header.Read(reader);

                header.SetBlocksOffset(source_stream.Position);
                //header.Dump();

                int num_blocks = header.blocks.Length;

                for (int i = 0; i < num_blocks; i++)
                {
                    header.blocks[i].Read(reader);
                }
            }

            {
                int bt_NiTriShapeData = header.GetBlockTypeIdxByName("NiTriShapeData");

                int num_blocks = header.blocks.Length;

                for (int i = 0; i < num_blocks; i++)
                {
                    if (header.blocks[i].type == bt_NiTriShapeData)
                    {
                        using (MemoryStream stream = new MemoryStream(header.blocks[i].data))
                        {
                            BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.Default);

                            NiTriShapeData triShapeData = new NiTriShapeData();
                            triShapeData.Read(reader);
                            triShapeData.Dump();
                        }
                    }
                }
            }
        }
    }
}
