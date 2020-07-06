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
        NiFooter footer;
        NiNode[] nodes;
        int root_ref = -1;

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

            //header.Dump();

            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                header.blocks[i].Read(reader);
            }

            footer = new NiFooter();
            footer.Read(reader);

            NiObject.user_version = header.user_version;
            NiObject.user_version_2 = header.user_version_2;

            int bt_NiNode = header.GetBlockTypeIdxByName("NiNode");

            nodes = new NiNode[num_blocks];
            for (int i = 0; i < num_blocks; i++)
            {
                if (header.blocks[i].type == bt_NiNode)
                {
                    nodes[i] = header.GetObject<NiNode>(i);
                }
            }

            //StringRef string_root = header.GetStringRefByName("NPC Root [Root]");
            //Console.WriteLine("String idx 'NPC Root [Root]': {0}", string_root);
            StringRef root_name_ref = header.GetStringRefByName("Root");
            Console.WriteLine("String ref 'Root': {0}", root_name_ref);

            root_ref = -1;
            for (int i = 0; i < num_blocks; i++)
            {
                NiNode node = nodes[i];
                if (node == null)
                    continue;

                if (node.name == root_name_ref)
                {
                    root_ref = i;
                    break;
                }
            }
            //Console.WriteLine("root_ref 'NPC Root [Root]': {0}", root_node_ref);
            Console.WriteLine("root_ref 'Root': {0}", root_ref);
            SetNodeRefParent(root_ref, null);

            DumpNodeRef(root_ref);
        }

        public void SetNodeRefParent(ObjectRef node_ref, NiNode parent)
        {
            if (node_ref == -1)
                return;

            NiNode node = nodes[node_ref];
            if (node == null)
            {
                Console.Error.WriteLine("null ref node_ref:{0}", node_ref);
                return;
            }
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
            if (node == null)
            {
                Console.Error.WriteLine("null ref node_ref:{0}", node_ref);
                return;
            }
            //node.Dump();

#if true
            Console.Write(GetString(node.name));
            Console.WriteLine("\tScale\t{0:F6}", node.local.scale);

            ref Matrix3x3 rotation = ref node.local.rotation;
            Console.Write(GetString(node.name));
            Console.Write("\tRotation\t{0:F6} {1:F6} {2:F6}", rotation.M11, rotation.M21, rotation.M31);
            Console.Write(" {0:F6} {1:F6} {2:F6}", rotation.M12, rotation.M22, rotation.M32);
            Console.Write(" {0:F6} {1:F6} {2:F6}", rotation.M13, rotation.M23, rotation.M33);
            Console.WriteLine();

            //Quaternion rotation;
            //Quaternion.RotationMatrix(ref node.local.rotation, out rotation);
            //Console.Write(GetString(node.name));
            //Console.WriteLine("\tRotation\t{0:F6} {1:F6} {2:F6} {2:F6}", rotation.X, rotation.Y, rotation.Z, rotation.W);

            ref Vector3 translation = ref node.local.translation;
            Console.Write(GetString(node.name));
            Console.WriteLine("\tPosition\t{0:F6} {1:F6} {2:F6}", translation.X, translation.Y, translation.Z);
#endif

#if false
            if (!SharpDX.MathUtil.NearEqual(node.local.scale, 1.0f))
            {
                Console.Write(GetString(node.name));
                Console.WriteLine("\t{0:F6}", node.local.scale);
            }
#endif

            foreach (ObjectRef _node_ref in node.children)
                DumpNodeRef(_node_ref);
        }
    }
}
