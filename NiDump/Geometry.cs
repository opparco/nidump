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
}
