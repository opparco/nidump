using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

namespace NiDump
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

    //KeyType: uint 1..5
    //1: LINEAR_KEY
    //2: QUADRATIC_KEY
    using KeyType = System.UInt32;

    public class ControllerLink
    {
        public ObjectRef interpolator;
        public ObjectRef controller; //= -1
        public byte priority;
        public StringRef node_name;
        public StringRef priority_type;
        public StringRef controller_type;
        public StringRef variable_1;
        public StringRef variable_2;

        public void Read(BinaryReader reader)
        {
            interpolator = reader.ReadInt32();
            controller = reader.ReadInt32(); //= -1
            priority = reader.ReadByte();
            node_name = reader.ReadInt32();
            priority_type = reader.ReadInt32();
            controller_type = reader.ReadInt32();
            variable_1 = reader.ReadInt32();
            variable_2 = reader.ReadInt32();
        }

        public void Dump()
        {
            Console.WriteLine("Interpolator: {0} Node Name: {1} Controller Type: {2}", interpolator, node_name, controller_type);
        }
    }

    public class NiControllerSequence
    {
        public StringRef name;
        public uint unknown_int_1;
        public ControllerLink[] controlled_blocks;
        public float weight;
        public ObjectRef text_keys;
        public uint cycle_type;
        public float frequency;
        public float start_time;
        public float stop_time;
        public ObjectRef manager;
        public StringRef target_name;
        public short unknown_short_1;

        public void Read(BinaryReader reader)
        {
            StringRef name = reader.ReadInt32();
            uint num_controlled_blocks = reader.ReadUInt32();
            controlled_blocks = new ControllerLink[num_controlled_blocks];
            unknown_int_1 = reader.ReadUInt32();

            //Controlled Blocks: array of ControllerLink
            for (int i = 0; i < num_controlled_blocks; i++)
            {
                controlled_blocks[i] = new ControllerLink();
                controlled_blocks[i].Read(reader);
            }

            weight = reader.ReadSingle();
            text_keys = reader.ReadInt32();
            cycle_type = reader.ReadUInt32();
            frequency = reader.ReadSingle();
            start_time = reader.ReadSingle();
            stop_time = reader.ReadSingle();
            manager = reader.ReadInt32();
            target_name = reader.ReadInt32();
            unknown_short_1 = reader.ReadInt16();
        }

        public void Dump()
        {
            Console.WriteLine("Name: {0}", name);

            int num_controlled_blocks = controlled_blocks.Length;
            Console.WriteLine("#Controlled Blocks: {0}", num_controlled_blocks);
            Console.WriteLine("Unknown Int 1: {0}", unknown_int_1);

            foreach (ControllerLink controller_link in controlled_blocks)
            {
                controller_link.Dump();
            }

            Console.WriteLine("Weight: {0:F6}", weight);
            Console.WriteLine("Text Keys: {0}", text_keys);
            Console.WriteLine("Cycle Type: {0}", cycle_type);
            Console.WriteLine("Frequency: {0:F6}", frequency);
            Console.WriteLine("Start Time: {0:F6}", start_time);
            Console.WriteLine("Stop Time: {0:F6}", stop_time);
            Console.WriteLine("Manager: {0}", manager);
            Console.WriteLine("Target Name: {0}", target_name);
            Console.WriteLine("Unknown Short 1: {0}", unknown_short_1);
        }
    }

    public class TextKey
    {
        public void Read(BinaryReader reader)
        {
            float time = reader.ReadSingle();
            StringRef value = reader.ReadInt32();
            Console.WriteLine("Time: {0:F6} Value: {1}", time, value);
        }
    }

    public class NiTextKeyExtraData
    {
        public void Read(BinaryReader reader)
        {
            Console.WriteLine("-- dump NiTextKeyExtraData --");

            StringRef name = reader.ReadInt32();
            uint num_text_keys = reader.ReadUInt32();
            Console.WriteLine("#Text Keys: {0}", num_text_keys);

            //Text Keys:
            for (int i = 0; i < num_text_keys; i++)
            {
                TextKey text_key = new TextKey();
                text_key.Read(reader);
            }
        }
    }

    public class NiTransformInterpolator
    {
        public Vector3 translation;
        public Quaternion rotation;
        public float scale;
        public ObjectRef data;

        public void Read(BinaryReader reader)
        {
            reader.ReadVector3(out translation);
            reader.ReadQuaternion(out rotation);
            scale = reader.ReadSingle();
            data = reader.ReadInt32();
        }

        public void Dump()
        {
            Console.WriteLine("-- dump NiTransformInterpolator --");

            Console.WriteLine("Translation: {0:F6} {1:F6} {2:F6}", translation.X, translation.Y, translation.Z);
            Console.WriteLine("Rotation: {0:F6} {1:F6} {2:F6} {3:F6}", rotation.W, rotation.X, rotation.Y, rotation.Z);
            Console.WriteLine("Scale: {0:F6}", scale);
            Console.WriteLine("Data: {0}", data);
        }
    }

    public class NiTransformData
    {
        Dictionary<float, Vector3> translations;
        Dictionary<float, Quaternion> rotations;
        Dictionary<float, float> scales;

        public void ReadTranslationKey(BinaryReader reader)
        {
            float time = reader.ReadSingle();

            Vector3 translation;
            reader.ReadVector3(out translation);

            translations[time] = translation;
        }

        public void ReadRotationKey(BinaryReader reader)
        {
            float time = reader.ReadSingle();

            Quaternion rotation;
            reader.ReadQuaternion(out rotation);

            rotations[time] = rotation;
        }

        public void ReadScaleKey(BinaryReader reader)
        {
            float time = reader.ReadSingle();
            float scale = reader.ReadSingle();

            scales[time] = scale;
        }

        public void Read(BinaryReader reader)
        {
            rotations = new Dictionary<float, Quaternion>();
            //Quaternion Keys:
            {
                uint num_rotation_keys = reader.ReadUInt32();
                //cond: Num Keys != 0
                if (num_rotation_keys != 0)
                {
                    KeyType rotation_type = reader.ReadUInt32();
                    for (int i = 0; i < num_rotation_keys; i++)
                    {
                        //cond: rotation_type == QUADRATIC_KEY
                        ReadRotationKey(reader);
                    }
                }
            }

            translations = new Dictionary<float, Vector3>();
            //Translations:
            {
                uint num_keys = reader.ReadUInt32();
                //cond: Num Keys != 0
                if (num_keys != 0)
                {
                    KeyType interpolation = reader.ReadUInt32();
                    for (int i = 0; i < num_keys; i++)
                    {
                        ReadTranslationKey(reader);
                    }
                }
            }

            scales = new Dictionary<float, float>();
            //Scales:
            {
                uint num_keys = reader.ReadUInt32();
                //cond: Num Keys != 0
                if (num_keys != 0)
                {
                    KeyType interpolation = reader.ReadUInt32();
                    for (int i = 0; i < num_keys; i++)
                    {
                        ReadScaleKey(reader);
                    }
                }
            }
        }

        public void Dump()
        {
            Console.WriteLine("-- dump NiTransformData --");

            Console.WriteLine("Quaternion Keys:");
            foreach (float time in rotations.Keys)
            {
                Quaternion rotation = rotations[time];

                Console.Write("Time: {0:F6} ", time);
                Console.WriteLine("Rotation: {0:F6} {1:F6} {2:F6} {3:F6}", rotation.W, rotation.X, rotation.Y, rotation.Z);
            }

            Console.WriteLine("Translations:");
            foreach (float time in translations.Keys)
            {
                Vector3 translation = translations[time];

                Console.Write("Time: {0:F6} ", time);
                Console.WriteLine("Translation: {0:F6} {1:F6} {2:F6}", translation.X, translation.Y, translation.Z);
            }

            Console.WriteLine("Scales:");
            foreach (float time in scales.Keys)
            {
                float scale = scales[time];

                Console.Write("Time: {0:F6} ", time);
                Console.WriteLine("Scale: {0:F6}", scale);
            }
        }
    }

    public class Transform
    {
        public Vector3 translation;
        public Matrix3x3 rotation;
        public float scale;

        public Transform()
        {
            this.translation = Vector3.Zero;
            this.rotation = Matrix3x3.Identity;
            this.scale = 1.0f;
        }

        public Transform(Vector3 translation, Matrix3x3 rotation, float scale)
        {
            this.translation = translation;
            this.rotation = rotation;
            this.scale = scale;
        }

        public static Transform operator *(Transform t1, Transform t2)
        {
            return new Transform(
                t1.translation + Vector3.Transform(t2.translation, t1.rotation) * t1.scale,
                t1.rotation * t2.rotation,
                t1.scale * t2.scale);
        }

        public void Dump()
        {
            Console.Write("\t{0:F6} {1:F6} {2:F6}", translation.X, translation.Y, translation.Z);
            Console.Write("\t{0:F6} {1:F6} {2:F6}", rotation.M11, rotation.M21, rotation.M31);
            Console.Write("\t{0:F6} {1:F6} {2:F6}", rotation.M12, rotation.M22, rotation.M32);
            Console.Write("\t{0:F6} {1:F6} {2:F6}", rotation.M13, rotation.M23, rotation.M33);
            Console.Write("\t{0:F6}", scale);
            Console.WriteLine();
        }
    }

    public class NiFooter
    {
        public ObjectRef[] roots;

        public void Read(BinaryReader reader)
        {
            uint num_roots = reader.ReadUInt32();
            this.roots = new ObjectRef[num_roots];
            // Roots: array of ref
            for (int i = 0; i < num_roots; i++)
            {
                this.roots[i] = reader.ReadInt32();
            }
        }

        public void Dump()
        {
            Console.WriteLine("#roots: {0}", this.roots.Length);
            foreach (ObjectRef root in this.roots)
                Console.WriteLine("Root: {0}", root);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((uint)this.roots.Length);
            foreach (ObjectRef root in this.roots)
                writer.Write(root);
        }
    }

    // Group of vertex indices of vertices that match.
    public struct MatchGroup
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
    public struct TexCoord
    {
        float u;
        float v;
    }
#endif

    // List of three vertex indices.
    public struct Triangle
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
    public abstract class NiObject
    {
        public abstract void Read(BinaryReader reader);
        public abstract void Dump();
    }

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
    }

    // Bethesda-specific Texture Set.
    public class BSShaderTextureSet : NiObject
    {
        public int num_textures = 6;
        public string[] textures;

        public override void Read(BinaryReader reader)
        {
            this.num_textures = reader.ReadInt32();
            this.textures = new string[num_textures];
            for (int i = 0; i < num_textures; i++)
            {
                this.textures[i] = reader.ReadSizedString();
            }
        }

        public override void Dump()
        {
            System.Console.WriteLine("-- BSShaderTextureSet --");

            System.Console.WriteLine("num_textures:{0}", this.num_textures);
            /**
            0: Diffuse
            1: Normal/Gloss
            2: Glow(SLSF2_Glow_Map)/Skin/Hair/Rim light(SLSF2_Rim_Lighting)
            3: Height/Parallax
            4: Environment
            5: Environment Mask
            6: Subsurface for Multilayer Parallax
            7: Back Lighting Map (SLSF2_Back_Lighting)
            */
            for (int i = 0; i < num_textures; i++)
            {
                System.Console.WriteLine("{0}: {1}", i, this.textures[i]);
            }
        }
    }

    // Abstract base class for NiObjects that support names, extra data, and time controllers.
    public abstract class NiObjectNET : NiObject
    {
        public StringRef name;
        public ObjectRef[] extra_data;
        public ObjectRef controller;

        public override void Read(BinaryReader reader)
        {
            //cond: BSLightingShaderProperty
            //uint skyrim_shader_type;

            this.name = reader.ReadInt32();

            uint num_extra_data = reader.ReadUInt32();
            this.extra_data = new ObjectRef[num_extra_data];
            // Extra Data: array of ref
            for (int i = 0; i < num_extra_data; i++)
            {
                this.extra_data[i] = reader.ReadInt32();
            }

            // ref of NiTimeController
            this.controller = reader.ReadInt32();
        }
    }

    // Abstract audio-visual base class from which all of Gamebryo's scene graph objects inherit.
    public abstract class NiAVObject : NiObjectNET
    {
        public uint flags;
        public Transform local;
        public ObjectRef collision_object;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            this.flags = reader.ReadUInt32();
            this.local = new Transform();
            //todo: reader.ReadTransform
            reader.ReadVector3(out local.translation);
            reader.ReadMatrix3x3(out local.rotation);
            local.scale = reader.ReadSingle();

            // ref of NiCollisionObject
            this.collision_object = reader.ReadInt32();
        }
    }

    // Generic node object for grouping.
    public class NiNode : NiAVObject
    {
        public ObjectRef[] children;
        public ObjectRef[] effects;

        public ObjectRef self_ref;
        public NiNode parent;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            uint num_children = reader.ReadUInt32();
            this.children = new ObjectRef[num_children];
            // Children: array of ref
            for (int i = 0; i < num_children; i++)
            {
                this.children[i] = reader.ReadInt32();
            }

            uint num_effects = reader.ReadUInt32();
            this.effects = new ObjectRef[num_effects];
            // Effects: array of ref
            for (int i = 0; i < num_effects; i++)
            {
                this.effects[i] = reader.ReadInt32();
            }
        }

        public override void Dump()
        {
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.name);

            writer.Write((uint)this.extra_data.Length);
            foreach (ObjectRef extra_data in this.extra_data)
                writer.Write(extra_data);

            writer.Write(this.controller);
            writer.Write(this.flags);

            writer.Write(ref this.local.translation);
            writer.Write(ref this.local.rotation);
            writer.Write(this.local.scale);

            writer.Write(this.collision_object);

            writer.Write((uint)this.children.Length);
            foreach (ObjectRef node in this.children)
                writer.Write(node);

            writer.Write((uint)this.effects.Length);
            foreach (ObjectRef effect in this.effects)
                writer.Write(effect);
        }

        public Transform GetLocalTransform(int root_ref)
        {
            Transform t = new Transform();
            NiNode node = this;
            //int i = 0;
            while (node != null && node.self_ref != root_ref)
            {
                //Console.WriteLine(" local loop idx {0} Ref {1}", i, node.self_ref);
                t = node.local * t;
                node = node.parent;
                //i++;
            }
            return t;
        }
    }
}
