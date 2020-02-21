using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

namespace NiDump
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

    // Mesh data: vertices, vertex normals, etc.
    public abstract class NiGeometryData : NiObject
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
        public Vector3[] vertices;

        //  0: has_uv
        // 12: has_tangents
        public ushort bs_vector_flags;

        public uint material_crc;
        // Do we have lighting normals? These are essential for proper lighting: if not present, the model will only be influenced by ambient light.
        public bool has_normals;
        // The lighting normals.
        public Vector3[] normals;
        // Tangent vectors.
        public Vector3[] tangents;
        // Bitangent vectors.
        public Vector3[] bitangents;

        // NiBound
        //
        // Center of the bounding box (smallest box that contains all vertices) of the mesh.
        Vector3 center;
        // Radius of the mesh: maximal Euclidean distance between the center and all vertices.
        float radius;

        // Do we have vertex colors? These are usually used to fine-tune the lighting of the model.
        bool has_vertex_colors;
        // The vertex colors.
        Color4[] vertex_colors; // float[4]

        // The UV texture coordinates. They follow the OpenGL standard: some programs may require you to flip the second coordinate.
        // TODO: UV Sets
        public Vector2[] uvs;

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
                this.vertices = new Vector3[num_vertices];
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
                this.normals = new Vector3[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector3(out normals[i]);
                }
            }
            if (has_normals && has_tangents)
            {
                this.tangents = new Vector3[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadVector3(out tangents[i]);
                }
            }
            if (has_normals && has_tangents)
            {
                this.bitangents = new Vector3[num_vertices];
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
                this.vertex_colors = new Color4[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    reader.ReadColor4(out vertex_colors[i]);
                }
            }

            if (has_vertices && has_uv)
            {
                this.uvs = new Vector2[num_vertices];
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

        public override void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    // Describes a mesh, built from triangles.
    public abstract class NiTriBasedGeomData : NiGeometryData
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

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            throw new NotImplementedException();
        }
    }

    // Holds mesh data using a list of singular triangles.
    public class NiTriShapeData : NiTriBasedGeomData
    {
        // Num Triangles times 3.
        public uint num_triangle_points;

        // Do we have triangle data?
        public bool has_triangles;
        // Triangle face data.
        public Triangle[] triangles;

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
                    triangles[i] = new Triangle();
                    triangles[i].Read(reader);
                }
            }
            this.num_match_groups = reader.ReadUInt16();
            this.match_groups = new MatchGroup[num_match_groups];
            for (int i = 0; i < num_match_groups; i++)
            {
                match_groups[i] = new MatchGroup();
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

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            throw new NotImplementedException();
        }
    }

    public struct MaterialData
    {

        uint num_materials;
        // The name of the material.
        StringRef[] material_name;
        // Extra data associated with the material. A value of -1 means the material is the default implementation.
        int[] material_extra_data;
        // The index of the currently active material.
        int active_material;
        // Whether the materials for this object always needs to be updated before rendering with them.
        bool material_needs_update;

        public void Read(BinaryReader reader)
        {
            this.num_materials = reader.ReadUInt32();
            this.material_name = new StringRef[num_materials];
            for (int i = 0; i < num_materials; i++)
            {
                this.material_name[i] = reader.ReadInt32();
            }
            this.material_extra_data = new int[num_materials];
            for (int i = 0; i < num_materials; i++)
            {
                this.material_extra_data[i] = reader.ReadInt32();
            }
            this.active_material = reader.ReadInt32();
            this.material_needs_update = reader.ReadByte() != 0;
        }

        public void Dump()
        {
            Console.WriteLine("-- MaterialData --");

            Console.WriteLine("num_materials:{0}", num_materials);
            Console.WriteLine("material_name:{0}", material_name);
            Console.WriteLine("material_extra_data:{0}", material_extra_data);
            Console.WriteLine("active_material:{0}", active_material);
            Console.WriteLine("material_needs_update:{0}", material_needs_update);
        }
    }

    // Describes a visible scene element with vertices like a mesh, a particle system, lines, etc.
    public abstract class NiGeometry : NiAVObject
    {
        // Data reference (NiTriShapeData/NiTriStripsData).
        public ObjectRef data;

        public ObjectRef skin_instance;

        public MaterialData material_data;

        public ObjectRef shader_property;

        public ObjectRef alpha_property;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            this.data = reader.ReadInt32();
            this.skin_instance = reader.ReadInt32();
            this.material_data = new MaterialData();
            this.material_data.Read(reader);
            this.shader_property = reader.ReadInt32();
            this.alpha_property = reader.ReadInt32();
        }

        public override void Dump()
        {
            Console.WriteLine("-- NiGeometry --");

            Console.WriteLine("data:{0}", data);
            Console.WriteLine("skin_instance:{0}", skin_instance);
            material_data.Dump();
            Console.WriteLine("shader_property:{0}", shader_property);
            Console.WriteLine("alpha_property:{0}", alpha_property);
        }
    }

    // Describes a mesh, built from triangles.
    public abstract class NiTriBasedGeom : NiGeometry
    {
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
        }

        public override void Dump()
        {
            base.Dump();
        }
    }

    // A shape node that refers to singular triangle data.
    public class NiTriShape : NiTriBasedGeom
    {
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
        }

        public override void Dump()
        {
            base.Dump();
        }
    }

    public class BSVertexDesc
    {
        public byte vf1;
        public byte vf2;
        public byte vf3;
        public byte vf4;
        public byte vf5;
        public byte vf6;
        public byte vf7;
        public byte vf8;

        public void Read(BinaryReader reader)
        {
            this.vf1 = reader.ReadByte();
            this.vf2 = reader.ReadByte();
            this.vf3 = reader.ReadByte();
            this.vf4 = reader.ReadByte();
            this.vf5 = reader.ReadByte();
            this.vf6 = reader.ReadByte();
            this.vf7 = reader.ReadByte();
            this.vf8 = reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(vf1);
            writer.Write(vf2);
            writer.Write(vf3);
            writer.Write(vf4);
            writer.Write(vf5);
            writer.Write(vf6);
            writer.Write(vf7);
            writer.Write(vf8);
        }
    }

    public class BSVertexData
    {
        // cond: vertex
        public Half3 vertex;
        // cond: vertex
        public Half bitangent_x; // not byte

        // cond: uvs
        public Half2 uv;

        // cond: normals
        //public ByteVector3 normal;
        public byte normal_x;
        public byte normal_y;
        public byte normal_z;
        // cond: normals
        public byte bitangent_y;

        // cond: normals && tangents
        //public ByteVector3 tangent;
        public byte tangent_x;
        public byte tangent_y;
        public byte tangent_z;
        // cond: normals && tangents
        public byte bitangent_z;

        // cond: colors
        public ColorBGRA vertex_colors; // byte[4]

        // cond: skinned
        public Half[] bone_weights;
        public byte[] bone_indices;

        public void Read(BinaryReader reader)
        {
            reader.ReadHalf3(out this.vertex);
            this.bitangent_x = new Half(reader.ReadUInt16());

            reader.ReadHalf2(out this.uv);

            this.normal_x = reader.ReadByte();
            this.normal_y = reader.ReadByte();
            this.normal_z = reader.ReadByte();
            this.bitangent_y = reader.ReadByte();

            this.tangent_x = reader.ReadByte();
            this.tangent_y = reader.ReadByte();
            this.tangent_z = reader.ReadByte();
            this.bitangent_z = reader.ReadByte();

            //TODO: vertex_colors

            this.bone_weights = new Half[4];
            for (int i = 0; i < 4; i++)
            {
                bone_weights[i] = new Half(reader.ReadUInt16());
            }
            this.bone_indices = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bone_indices[i] = reader.ReadByte();
            }
        }

        public void Dump()
        {
            Console.WriteLine("-- BSVertexData --");

            Console.WriteLine("bone_weights:");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("{0} {1}", i, bone_weights[i]);
            }
            Console.WriteLine("bone_indices:");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("{0} {1}", i, bone_indices[i]);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(ref this.vertex);
            writer.Write(this.bitangent_x.RawValue);

            writer.Write(ref this.uv);

            writer.Write(this.normal_x);
            writer.Write(this.normal_y);
            writer.Write(this.normal_z);
            writer.Write(this.bitangent_y);

            writer.Write(this.tangent_x);
            writer.Write(this.tangent_y);
            writer.Write(this.tangent_z);
            writer.Write(this.bitangent_z);

            //TODO: vertex_colors

            for (int i = 0; i < 4; i++)
            {
                writer.Write(bone_weights[i].RawValue);
            }
            for (int i = 0; i < 4; i++)
            {
                writer.Write(bone_indices[i]);
            }
        }
    }

    public class BSTriShape : NiAVObject
    {
        // NiBound
        //
        // Center of the bounding box (smallest box that contains all vertices) of the mesh.
        public Vector3 center;
        // Radius of the mesh: maximal Euclidean distance between the center and all vertices.
        public float radius;

        public ObjectRef skin;
        public ObjectRef shader_property;
        public ObjectRef alpha_property;

        public BSVertexDesc vertex_desc;

        public uint num_triangles;
        public ushort num_vertices;
        public uint data_size;

        public BSVertexData[] vertex_data;
        public Triangle[] triangles;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            reader.ReadVector3(out this.center);
            this.radius = reader.ReadSingle();

            this.skin = reader.ReadInt32();
            this.shader_property = reader.ReadInt32();
            this.alpha_property = reader.ReadInt32();
            this.vertex_desc = new BSVertexDesc();
            this.vertex_desc.Read(reader);
            this.num_triangles = reader.ReadUInt32();
            this.num_vertices = reader.ReadUInt16();
            this.data_size = reader.ReadUInt32();

            if (data_size != 0)
            {
                this.vertex_data = new BSVertexData[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    vertex_data[i] = new BSVertexData();
                    vertex_data[i].Read(reader);
                }
                this.triangles = new Triangle[num_triangles];
                for (int i = 0; i < num_triangles; i++)
                {
                    triangles[i] = new Triangle();
                    triangles[i].Read(reader);
                }
            }
        }

        public override void Dump()
        {
            base.Dump();

            Console.WriteLine("-- BSTriShape --");

            Console.WriteLine("skin:{0}", skin);
            Console.WriteLine("shader_property:{0}", shader_property);
            Console.WriteLine("alpha_property:{0}", alpha_property);
            Console.WriteLine("vertex_desc:{0}", vertex_desc);
            Console.WriteLine("num_triangles:{0}", num_triangles);
            Console.WriteLine("num_vertices:{0}", num_vertices);
            Console.WriteLine("data_size:{0}", data_size);

            if (data_size != 0)
            {
                vertex_data[0].Dump();
            }
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(ref this.center);
            writer.Write(this.radius);

            writer.Write(this.skin);
            writer.Write(this.shader_property);
            writer.Write(this.alpha_property);
            this.vertex_desc.Write(writer);
            writer.Write(this.num_triangles);
            writer.Write(this.num_vertices);
            writer.Write(this.data_size);

            if (data_size != 0)
            {
                for (int i = 0; i < num_vertices; i++)
                {
                    vertex_data[i].Write(writer);
                }
                for (int i = 0; i < num_triangles; i++)
                {
                    triangles[i].Write(writer);
                }
            }
        }
    }

    public class BSGeometrySubSegmentData
    {
        public uint start_index;
        public uint num_primitives;
        public uint parent_array_index = 0;
        public uint unused = 0;


        public void Read(BinaryReader reader)
        {
            this.start_index = reader.ReadUInt32();
            this.num_primitives = reader.ReadUInt32();
            this.parent_array_index = reader.ReadUInt32();
            this.unused = reader.ReadUInt32();
        }

        public void Dump()
        {
            Console.WriteLine("-- BSGeometrySubSegmentData --");

            Console.WriteLine("Start Index: {0}", this.start_index);
            Console.WriteLine("Num Primitives: {0}", this.num_primitives);
            Console.WriteLine("Parent Array Index: {0}", this.parent_array_index);
            Console.WriteLine("Unused: {0}", this.unused);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.start_index);
            writer.Write(this.num_primitives);
            writer.Write(this.parent_array_index);
            writer.Write(this.unused);
        }
    }

    public class BSGeometrySegmentData
    {
        public uint start_index;
        public uint num_primitives;
        public uint parent_array_index = 4294967295;
        public uint num_sub_segments = 0;
        public BSGeometrySubSegmentData[] sub_segment;

        public void Read(BinaryReader reader)
        {
            this.start_index = reader.ReadUInt32();
            this.num_primitives = reader.ReadUInt32();
            this.parent_array_index = reader.ReadUInt32();
            this.num_sub_segments = reader.ReadUInt32();

            sub_segment = new BSGeometrySubSegmentData[this.num_sub_segments];
            for (int i = 0; i < num_sub_segments; i++)
            {
                sub_segment[i] = new BSGeometrySubSegmentData();
                sub_segment[i].Read(reader);
            }
        }

        public void Dump()
        {
            Console.WriteLine("-- BSGeometrySegmentData --");

            Console.WriteLine("Start Index: {0}", this.start_index);
            Console.WriteLine("Num Primitives: {0}", this.num_primitives);
            Console.WriteLine("Parent Array Index: {0}", this.parent_array_index);
            Console.WriteLine("Num Sub Segments: {0}", this.num_sub_segments);

            for (int i = 0; i < num_sub_segments; i++)
            {
                sub_segment[i].Dump();
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.start_index);
            writer.Write(this.num_primitives);
            writer.Write(this.parent_array_index);
            writer.Write(this.num_sub_segments);

            for (int i = 0; i < num_sub_segments; i++)
            {
                sub_segment[i].Write(writer);
            }
        }
    }

    public class BSGeometryPerSegmentSharedData
    {
        public uint user_index;
        public uint bone_id; // hash of bone name
        public uint num_cut_offsets = 0;
        public float[] cut_offsets;

        public void Read(BinaryReader reader)
        {
            this.user_index = reader.ReadUInt32();
            this.bone_id = reader.ReadUInt32();
            this.num_cut_offsets = reader.ReadUInt32();

            for (int i = 0; i < num_cut_offsets; i++)
            {
                this.cut_offsets[i] = reader.ReadSingle();
            }
        }

        public void Dump()
        {
            Console.WriteLine("-- BSGeometryPerSegmentSharedData --");

            Console.WriteLine("User Index: {0}", this.user_index);
            Console.WriteLine("Bone Id: {0}", this.bone_id);

            // num_cut_offsets
            // cut_offsets
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.user_index);
            writer.Write(this.bone_id);
            writer.Write(this.num_cut_offsets);

            for (int i = 0; i < num_cut_offsets; i++)
            {
                writer.Write(this.cut_offsets[i]);
            }
        }
    }

    public class BSGeometrySegmentSharedData
    {
        public uint num_segments;
        public uint total_segments;
        public uint[] segment_starts;
        public BSGeometryPerSegmentSharedData[] per_segment;
        public string SSF_file;

        public void Read(BinaryReader reader)
        {
            this.num_segments = reader.ReadUInt32();
            this.total_segments = reader.ReadUInt32();

            this.segment_starts = new uint[this.num_segments];
            for (int i = 0; i < num_segments; i++)
            {
                this.segment_starts[i] = reader.ReadUInt32();
            }

            per_segment = new BSGeometryPerSegmentSharedData[this.total_segments];
            for (int i = 0; i < total_segments; i++)
            {
                per_segment[i] = new BSGeometryPerSegmentSharedData();
                per_segment[i].Read(reader);
            }

            this.SSF_file = reader.ReadShortString();
        }

        public void Dump()
        {
            Console.WriteLine("-- BSGeometrySegmentSharedData --");

            Console.WriteLine("Num Segments: {0}", this.num_segments);
            Console.WriteLine("Total Segments: {0}", this.total_segments);

            Console.WriteLine("Segment Starts:");
            for (int i = 0; i < num_segments; i++)
            {
                Console.WriteLine("{0}", segment_starts[i]);
            }

            for (int i = 0; i < total_segments; i++)
            {
                per_segment[i].Dump();
            }

            Console.WriteLine("SSF File: {0}", this.SSF_file ?? "(null)");
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.num_segments);
            writer.Write(this.total_segments);

            for (int i = 0; i < num_segments; i++)
            {
                writer.Write(this.segment_starts[i]);
            }

            for (int i = 0; i < total_segments; i++)
            {
                per_segment[i].Write(writer);
            }

            writer.WriteShortString(this.SSF_file);
        }
    }

    public class BSSubIndexTriShape : BSTriShape
    {
        // cond: data_size > 0
        public uint num_primitives;
        // cond: data_size > 0
        public uint num_segments;
        // cond: data_size > 0
        public uint total_segments;
        // cond: data_size > 0
        public BSGeometrySegmentData[] segment;
        // cond: num_segments < total_segments && data_size > 0
        public BSGeometrySegmentSharedData segment_data;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            this.num_primitives = reader.ReadUInt32();
            this.num_segments = reader.ReadUInt32();
            this.total_segments = reader.ReadUInt32();

            segment = new BSGeometrySegmentData[this.num_segments];
            for (int i = 0; i < num_segments; i++)
            {
                segment[i] = new BSGeometrySegmentData();
                segment[i].Read(reader);
            }

            if (num_segments < total_segments)
            {
                segment_data = new BSGeometrySegmentSharedData();
                segment_data.Read(reader);
            }
        }

        public override void Dump()
        {
            base.Dump();

            Console.WriteLine("-- BSSubIndexTriShape --");

            Console.WriteLine("Num Primitives: {0}", this.num_primitives);
            Console.WriteLine("Num Segments: {0}", this.num_segments);
            Console.WriteLine("Total Segments: {0}", this.total_segments);

            for (int i = 0; i < num_segments; i++)
            {
                segment[i].Dump();
            }

            if (segment_data != null)
                segment_data.Dump();
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(this.num_primitives);
            writer.Write(this.num_segments);
            writer.Write(this.total_segments);

            for (int i = 0; i < num_segments; i++)
            {
                segment[i].Write(writer);
            }

            if (segment_data != null)
                segment_data.Write(writer);
        }
    }
}
