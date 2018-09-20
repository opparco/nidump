using System.IO;
using NiDump;

namespace NiDumpShape
{
    using ObjectRef = System.Int32;

    // Group of vertex indices of vertices that match.
    struct MatchGroup
    {
        // Number of vertices in this group.
        ushort num_vertices;
        // The vertex indices.
        ushort[] vertex_indices;

        public void Read(BinaryReader reader)
        {
            num_vertices = reader.ReadUInt16();
            vertex_indices = new ushort[num_vertices];
            for (int i = 0; i < num_vertices; i++)
            {
                vertex_indices[i] = reader.ReadUInt16();
            }
        }
    }
#if false
    // Texture coordinates (u,v). As in OpenGL; image origin is in the lower left corner.
    struct TexCoord
    {
        float u;
        float v;
    }
#endif
    // List of three vertex indices.
    struct Triangle
    {
        ushort v1;
        ushort v2;
        ushort v3;

        public void Read(BinaryReader reader)
        {
            v1 = reader.ReadUInt16();
            v2 = reader.ReadUInt16();
            v3 = reader.ReadUInt16();
        }
    }
    // NiObject : NiGeometryData : NiTriBasedGeomData : NiTriShapeData
    abstract class NiObject
    {
        public abstract void Read(BinaryReader reader);
        public abstract void Dump();
    }
    // Mesh data: vertices, vertex normals, etc.
    abstract class NiGeometryData : NiObject
    {
        // Always zero.
        public int group_id;

        // Number of vertices.
        public ushort num_vertices;

        // Used with NiCollision objects when OBB or TRI is set.
        public byte keep_flags;
        // Unknown.
        public byte compress_flags;

        // Always non-zero.
        public bool has_vertices = true;
        // The mesh vertices.
        public SharpDX.Vector3[] vertices;

        //  0: has_uv
        // 12: has_tangents
        public ushort bs_vector_flags;

        public uint material_crc;
        // Do we have lighting normals? These are essential for proper lighting: if not present, the model will only be influenced by ambient light.
        public bool has_normals;
        // The lighting normals.
        public SharpDX.Vector3[] normals;
        // Tangent vectors.
        public SharpDX.Vector3[] tangents;
        // Bitangent vectors.
        public SharpDX.Vector3[] bitangents;

        // NiBound
        //
        // Center of the bounding box (smallest box that contains all vertices) of the mesh.
        SharpDX.Vector3 center;
        // Radius of the mesh: maximal Euclidean distance between the center and all vertices.
        float radius;

        // Do we have vertex colors? These are usually used to fine-tune the lighting of the model.
        bool has_vertex_colors;
        // The vertex colors.
        SharpDX.Color4[] vertex_colors;

        // The UV texture coordinates. They follow the OpenGL standard: some programs may require you to flip the second coordinate.
        // TODO: UV Sets
        public SharpDX.Vector2[] uvs;

        // Consistency Flags
        public ushort consistency_flags = 0x0000;  // CT_MUTABLE
        // Unknown.
        public ObjectRef additional_data;

        bool has_uv
        {
            get { return (bs_vector_flags & (1)) != 0; }
        }
        bool has_tangents
        {
            get { return (bs_vector_flags & (1<<12)) != 0; }
        }
        public override void Read(BinaryReader reader)
        {
            this.group_id = reader.ReadInt32();
            this.num_vertices = reader.ReadUInt16();
            this.keep_flags = reader.ReadByte();
            this.compress_flags = reader.ReadByte();

            this.has_vertices = reader.ReadByte() != 0;
            if (has_vertices)
            {
                this.vertices = new SharpDX.Vector3[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector3(out vertices[i]);
                }
            }

            this.bs_vector_flags = reader.ReadUInt16();
            this.material_crc = reader.ReadUInt32();

            this.has_normals = reader.ReadByte() != 0;
            if (has_vertices && has_normals)
            {
                this.normals = new SharpDX.Vector3[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector3(out normals[i]);
                }
            }
            if (has_normals && has_tangents)
            {
                this.tangents = new SharpDX.Vector3[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector3(out tangents[i]);
                }
            }
            if (has_normals && has_tangents)
            {
                this.bitangents = new SharpDX.Vector3[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector3(out bitangents[i]);
                }
            }

            reader.ReadVector3(out this.center);
            this.radius = reader.ReadSingle();

            this.has_vertex_colors = reader.ReadByte() != 0;
            if (has_vertices && has_vertex_colors)
            {
                this.vertex_colors = new SharpDX.Color4[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadColor4(out vertex_colors[i]);
                }
            }

            if (has_vertices && has_uv)
            {
                this.uvs = new SharpDX.Vector2[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector2(out uvs[i]);
                }
            }

            this.consistency_flags = reader.ReadUInt16();
            this.additional_data = reader.ReadInt32();
        }

        public override void Dump()
        {
            System.Console.WriteLine("-- NiGeometryData --");

            System.Console.WriteLine("num_vertices:{0}", this.num_vertices);
            System.Console.WriteLine("has_vertices:{0}", this.has_vertices);

            System.Console.WriteLine("bs_vector_flags:{0:X4}", this.bs_vector_flags);

            System.Console.WriteLine("material_crc:{0:X8}", this.material_crc);
            System.Console.WriteLine("has_normals:{0}", this.has_normals);

            System.Console.WriteLine("center:{0}", this.center);
            System.Console.WriteLine("radius:{0}", this.radius);

            System.Console.WriteLine("has_vertex_colors:{0}", this.has_vertex_colors);

            System.Console.WriteLine("consistency_flags:{0:X4}", this.consistency_flags);
            System.Console.WriteLine("additional_data:{0}", this.additional_data);
        }
    }
    // Describes a mesh, built from triangles.
    abstract class NiTriBasedGeomData : NiGeometryData
    {
        // Number of triangles.
        public ushort num_triangles;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            this.num_triangles = reader.ReadUInt16();
            System.Console.WriteLine("num_triangles:{0}", this.num_triangles);
        }

        public override void Dump()
        {
            base.Dump();

            System.Console.WriteLine("-- NiTriBasedGeomData --");

            System.Console.WriteLine("num_triangles:{0}", this.num_triangles);
        }
    }
    // Holds mesh data using a list of singular triangles.
    class NiTriShapeData : NiTriBasedGeomData
    {
        // Num Triangles times 3.
        public uint num_triangle_points;

        // Do we have triangle data?
        public bool has_triangles;
        // Triangle face data.
        Triangle[] triangles;

        // Number of shared normals groups.
        public ushort num_match_groups;
        // The shared normals.
        public MatchGroup[] match_groups;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            this.num_triangle_points = reader.ReadUInt32();
            this.has_triangles = reader.ReadByte() != 0;
            if (has_triangles)
            {
                this.triangles = new Triangle[num_triangles];
                for (int i = 0; i < num_triangles; i++)
                {
                    triangles[i].Read(reader);
                }
            }
            this.num_match_groups = reader.ReadUInt16();
            this.match_groups = new MatchGroup[num_match_groups];
            for (int i = 0; i < num_match_groups; i++)
            {
                match_groups[i].Read(reader);
            }
        }

        public override void Dump()
        {
            base.Dump();

            System.Console.WriteLine("-- NiTriShapeData --");

            System.Console.WriteLine("num_triangle_points:{0}", this.num_triangle_points);
            System.Console.WriteLine("has_triangles:{0}", this.has_triangles);
            System.Console.WriteLine("num_match_groups:{0}", this.num_match_groups);
        }
    }

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
