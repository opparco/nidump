using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

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
            program.LoadNodeTransforms(txt_file);
            program.UpdateNodeTransforms();
            program.Save("out.nif");
        }

        //map node name to transform
        Dictionary<string, Transform> node_transforms = new Dictionary<string, Transform>();

        public void LoadNodeTransforms(string source_file)
        {
            StreamReader reader = new StreamReader(source_file, System.Text.Encoding.Default);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] tuple = line.Split(new char[] { '\t' });

                if (tuple.Length != 4)
                    continue;

                string name = tuple[0];
                Transform t = new Transform();
                {
                    float scale;
                    if (float.TryParse(tuple[1], out scale))
                        t.scale = scale;
                }
                {
                    string[] string_values = tuple[2].Split(new char[] { ' ' });

                    if (string_values.Length != 4)
                        continue;

                    float[] values = new float[4];
                    for (int i=0; i<4; i++)
                    {
                        float value;
                        if (float.TryParse(string_values[i], out value))
                            values[i] = value;
                    }
                    Quaternion rotation = new Quaternion(values);

                    Matrix3x3.RotationQuaternion(ref rotation, out t.rotation);
                }
                {
                    string[] string_values = tuple[3].Split(new char[] { ' ' });

                    if (string_values.Length != 3)
                        continue;

                    float[] values = new float[3];
                    for (int i=0; i<3; i++)
                    {
                        float value;
                        if (float.TryParse(string_values[i], out value))
                            values[i] = value;
                    }
                    t.translation = new Vector3(values);
                }
                node_transforms[name] = t;
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

        void UpdateNodeTransforms()
        {
            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                if (nodes[i] == null)
                    continue;

                string name = GetString(nodes[i].name);
                Transform t;
                if (node_transforms.TryGetValue(name, out t))
                {
                    nodes[i].local = t;
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
