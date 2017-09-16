using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

using NiDump;

using ObjectRef = System.Int32;
using StringRef = System.Int32;

namespace NiDumpScale
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: NiDumpScale <nif file>");
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

            DumpNodes();
        }

        NiNode[] nodes;
        int root_node_ref = -1;

        void DumpNodes()
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

            int string_root = header.GetStringIdxByName("NPC Root [Root]");
            //Console.WriteLine("String idx 'NPC Root [Root]': {0}", string_root);

            root_node_ref = -1;
            for (int i = 0; i < num_blocks; i++)
            {
                NiNode node = nodes[i];
                if (node == null)
                    continue;

                if (node.name == string_root)
                {
                    root_node_ref = i;
                    break;
                }
            }
            SetNodeRefParent(root_node_ref, null);

            DumpNodeRef(root_node_ref);
        }

        public void SetNodeRefParent(ObjectRef node_ref, NiNode parent)
        {
            if (node_ref == -1)
                return;

            NiNode node = nodes[node_ref];
            node.self_ref = node_ref;
            node.parent = parent;

            foreach (ObjectRef _node_ref in node.children)
            {
                SetNodeRefParent(_node_ref, node);
            }
        }

        public string GetString(StringRef string_ref)
        {
            return string_ref != -1 ? header.strings[string_ref] : "(undefined)";
        }

        public void DumpNodeRef(ObjectRef node_ref)
        {
            if (node_ref == -1)
                return;

            NiNode node = nodes[node_ref];

            if (!MathUtil.NearEqual(node.local.scale, 1.0f))
            {
                Console.Write(GetString(node.name));
                Console.WriteLine("\t{0:F6}", node.local.scale);
            }

            foreach (ObjectRef _node_ref in node.children)
                DumpNodeRef(_node_ref);
        }
    }
}