using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using NiDump;

using ObjectRef = System.Int32;
using StringRef = System.Int32;

namespace NiUpdateScale
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine("Usage: NiUpdateScale <nif file> <txt file>");
                return;
            }

            string nif_file = args[0];
            string txt_file = args[1];

            Program program = new Program();
            program.Load(nif_file);
            program.LoadNodeScales(txt_file);
            program.UpdateNodeScales();
            program.Save("out.nif");
        }

        //map node name to scale factor
        Dictionary<string, float> node_scales = new Dictionary<string, float>();

        public void LoadNodeScales(string source_file)
        {
            StreamReader reader = new StreamReader(source_file, System.Text.Encoding.Default);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] tuple = line.Split(new char[] { '\t' });

                if (tuple.Length != 2)
                    continue;

                string name = tuple[0];
                float scale;
                if (float.TryParse(tuple[1], out scale))
                {
                    node_scales[name] = scale;
                }
            }
        }

        NiHeader header;
        NiFooter footer;

        public void Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                Load(source_stream);
        }

        public void Load(Stream source_stream)
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

            footer = new NiFooter();
            footer.Read(reader);
            //footer.Dump();

            CreateNodes();
        }

        NiNode[] nodes;

        void CreateNodes()
        {
            int bt_NiNode = header.GetBlockTypeIdxByName("NiNode");
            //Console.WriteLine("BT idx 'NiNode': {0}", bt_NiNode);

            int num_blocks = header.blocks.Length;

            nodes = new NiNode[num_blocks];
            for (int i = 0; i < num_blocks; i++)
            {
                if (header.blocks[i].type == bt_NiNode)
                {
                    using (MemoryStream stream = new MemoryStream(header.blocks[i].data))
                    {
                        BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.Default);

                        nodes[i] = new NiNode();
                        nodes[i].Read(reader);
                    }
                }
            }
        }

        public string GetString(StringRef string_ref)
        {
            return string_ref != -1 ? header.strings[string_ref] : "(undefined)";
        }

        void UpdateNodeScales()
        {
            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                if (nodes[i] == null)
                    continue;

                string name = GetString(nodes[i].name);
                float scale;
                if (node_scales.TryGetValue(name, out scale))
                {
                    nodes[i].local.scale = scale;
                }
            }
        }

        public void Save(string dest_file)
        {
            using (Stream dest_stream = File.Create(dest_file))
                Save(dest_stream);
        }

        public void Save(Stream dest_stream)
        {
            BinaryWriter writer = new BinaryWriter(dest_stream, System.Text.Encoding.Default);

            header.Write(writer);

            int num_blocks = header.blocks.Length;
            for (int i = 0; i < num_blocks; i++)
            {
                if (nodes[i] != null)
                    nodes[i].Write(writer);
                else
                    header.blocks[i].Write(writer);
            }

            footer.Write(writer);
        }
    }
}