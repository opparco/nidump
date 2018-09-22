﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

namespace NiDump
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

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
