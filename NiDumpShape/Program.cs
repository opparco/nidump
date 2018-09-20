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
    // Texture coordinates (u,v). As in OpenGL; image origin is in the lower left corner.
    struct TexCoord
    {
        float u;
        float v;
    }
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
        public TexCoord[] uv_sets;

        // Consistency Flags
        public ushort consistency_flags = 0x0000;  // CT_MUTABLE
        // Unknown.
        public ObjectRef additional_data;

        public override void Read(BinaryReader reader)
        {
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

            num_triangle_points = reader.ReadUInt32();
            has_triangles = reader.ReadByte() != 0;
            if (has_triangles)
            {
                triangles = new Triangle[num_triangle_points/3];
                for (int i = 0; i < num_triangle_points/3; i++)
                {
                    triangles[i].Read(reader);
                }
            }
            num_match_groups = reader.ReadUInt16();
            match_groups = new MatchGroup[num_match_groups];
            for (int i = 0; i < num_triangle_points; i++)
            {
                match_groups[i].Read(reader);
            }
        }

        public void Dump()
        {
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
            BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            header = new NiHeader();
            header.Read(reader);

            header.SetBlocksOffset(source_stream.Position);
            header.Dump();
        }
    }
}
