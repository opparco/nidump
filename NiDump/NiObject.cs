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

    public class NiNode
    {
        //
        // NiObjectNET members
        //
        public StringRef name;
        public ObjectRef[] extra_data;
        public ObjectRef controller;

        //
        // NiAVObject members
        //
        public uint flags;
        public Transform local;
        public ObjectRef collision_object;

        //
        // NiNode members
        //
        public ObjectRef[] children;
        public ObjectRef[] effects;

        public ObjectRef self_ref;
        public NiNode parent;

        public void Read(BinaryReader reader)
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

            this.flags = reader.ReadUInt32();
            this.local = new Transform();
            //todo: reader.ReadTransform
            reader.ReadVector3(out local.translation);
            reader.ReadMatrix3x3(out local.rotation);
            local.scale = reader.ReadSingle();

            // ref of NiCollisionObject
            this.collision_object = reader.ReadInt32();

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