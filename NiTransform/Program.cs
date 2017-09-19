using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using NiDump;

using ObjectRef = System.Int32;
using StringRef = System.Int32;

using SharpDX;

namespace NiTransform
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine("Usage: NiTransform <nif file> <txt file>");
                return;
            }

            string nif_file = args[0];
            string txt_file = args[1];

            Program program = new Program();
            program.Load(nif_file);
            program.LoadNodeTransforms(txt_file);
            program.UpdateNodes();
            program.Save("out.nif");
        }

        //map node name to scale factor
        Dictionary<string, float> node_scales = new Dictionary<string, float>();
        Dictionary<string, Vector3> node_positions = new Dictionary<string, Vector3>();
        Dictionary<string, Matrix3x3> node_rotations = new Dictionary<string, Matrix3x3>();

        public void LoadNodeTransforms(string source_file)
        {
            StreamReader reader = new StreamReader(source_file, System.Text.Encoding.Default);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] tuple = line.Split(new char[] { '\t' });

                if (tuple.Length != 3)
                    continue;

                string name = tuple[0];
                string method = tuple[1];
                if (method == "Scale")
                {
                    //todo: catch FormatException
                    //todo: catch OverflowException
                    float scale = float.Parse(tuple[2]);
                    node_scales[name] = scale;
                }
                else if (method == "Position")
                {
                    string[] tmp = tuple[2].Split(new char[] { ' ' });

                    if (tmp.Length != 3)
                        continue;

                    Vector3 position;
                    position.X = float.Parse(tmp[0]);
                    position.Y = float.Parse(tmp[1]);
                    position.Z = float.Parse(tmp[2]);
                    node_positions[name] = position;
                }
                else if (method == "Rotation")
                {
                    string[] tmp = tuple[2].Split(new char[] { ' ' });

                    if (tmp.Length != 9)
                        continue;

                    Matrix3x3 rotation;
                    rotation.M11 = float.Parse(tmp[0]);
                    rotation.M21 = float.Parse(tmp[1]);
                    rotation.M31 = float.Parse(tmp[2]);
                    rotation.M12 = float.Parse(tmp[3]);
                    rotation.M22 = float.Parse(tmp[4]);
                    rotation.M32 = float.Parse(tmp[5]);
                    rotation.M13 = float.Parse(tmp[6]);
                    rotation.M23 = float.Parse(tmp[7]);
                    rotation.M33 = float.Parse(tmp[8]);
                    node_rotations[name] = rotation;
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

        void UpdateNodes()
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
                    nodes[i].local.scale = nodes[i].local.scale * scale;
                }
                Vector3 position;
                if (node_positions.TryGetValue(name, out position))
                {
                    nodes[i].local.translation = nodes[i].local.translation + position;
                }
                Matrix3x3 rotation;
                if (node_rotations.TryGetValue(name, out rotation))
                {
                    nodes[i].local.rotation = nodes[i].local.rotation * rotation;
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
