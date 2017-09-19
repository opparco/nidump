using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using SharpDX;

namespace NiDumpCharGen
{
    [DataContract]
    class Value
    {
        [DataMember]
        internal float data;
        [DataMember]
        internal int index;
        [DataMember]
        internal int key;
        [DataMember]
        internal int type;
    }
    [DataContract]
    class Key
    {
        [DataMember]
        internal string name;
        [DataMember]
        internal Value[] values;

        bool hasScale = false;
        bool hasPosition = false;
        bool hasRotation = false;

        public bool HasScale { get { return hasScale; } }
        public bool HasPosition { get { return hasPosition; } }
        public bool HasRotation { get { return hasRotation; } }

        float scale;
        Vector3 position;
        Matrix3x3 rotation;

        public void Compute()
        {
            //Console.WriteLine("\tname: {0}", name);

            foreach (Value value in values)
            {
                switch (value.key)
                {
                    case 30: // Scale
                        hasScale = true;
                        scale = value.data;
                        break;
                    case 31: // Position
                        hasPosition = true;
                        switch (value.index)
                        {
                            case 0: // x
                                position.X = value.data;
                                break;
                            case 1: // y
                                position.Y = value.data;
                                break;
                            case 2: // z
                                position.Z = value.data;
                                break;
                        }
                        break;
                    case 32: // Rotation
                        hasRotation = true;
                        switch (value.index)
                        {
                            case 0: // m11
                                rotation.M11 = value.data;
                                break;
                            case 1: // m21
                                rotation.M21 = value.data;
                                break;
                            case 2: // m31
                                rotation.M31 = value.data;
                                break;
                            case 3: // m12
                                rotation.M12 = value.data;
                                break;
                            case 4: // m22
                                rotation.M22 = value.data;
                                break;
                            case 5: // m32
                                rotation.M32 = value.data;
                                break;
                            case 6: // m13
                                rotation.M13 = value.data;
                                break;
                            case 7: // m23
                                rotation.M23 = value.data;
                                break;
                            case 8: // m33
                                rotation.M33 = value.data;
                                break;
                        }
                        break;
                }
            }
        }

        public float GetScale()
        {
            return scale;
        }

        public void GetPosition(out Vector3 position)
        {
            position = this.position;
        }

        public void GetRotation(out Matrix3x3 rotation)
        {
            rotation = this.rotation;
        }
    }
    [DataContract]
    class Transform
    {
        [DataMember]
        internal bool firstPerson;
        [DataMember]
        internal string node;
        [DataMember]
        internal Key[] keys;

        public void Dump(ref float f)
        {
            Console.Write("\t{0}", f);
        }
        public void Dump(ref Vector3 v)
        {
            Console.Write("\t{0} {1} {2}", v.X, v.Y, v.Z);
        }
        public void Dump(ref Matrix3x3 m)
        {
            Console.Write("\t{0} {1} {2}", m.M11, m.M21, m.M31);
            Console.Write(" {0} {1} {2}", m.M12, m.M22, m.M32);
            Console.Write(" {0} {1} {2}", m.M13, m.M23, m.M33);
        }
        public void Dump()
        {
            if (firstPerson)
                return;

            foreach (Key key in keys)
            {
                key.Compute();

                if (key.HasScale)
                {
                    Console.Write(node);
                    Console.Write("\tScale");
                    float scale = key.GetScale();
                    Dump(ref scale);
                    Console.WriteLine();
                }
                if (key.HasPosition)
                {
                    Console.Write(node);
                    Console.Write("\tPosition");
                    Vector3 position;
                    key.GetPosition(out position);
                    Dump(ref position);
                    Console.WriteLine();
                }
                if (key.HasRotation)
                {
                    Console.Write(node);
                    Console.Write("\tRotation");
                    Matrix3x3 rotation;
                    key.GetRotation(out rotation);
                    Dump(ref rotation);
                    Console.WriteLine();
                }
            }
        }
    }
    [DataContract]
    class RaceMenuSlot
    {
        [DataMember]
        Transform[] transforms;

        public void Dump()
        {
            foreach (Transform transform in transforms)
                transform.Dump();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: NiDumpCharGen <source file>");
                return;
            }

            string source_file = args[0];

            FileStream stream = File.OpenRead(source_file);
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(RaceMenuSlot));
                RaceMenuSlot slot = (RaceMenuSlot)serializer.ReadObject(stream);
                slot.Dump();
            }
            catch (SerializationException ex)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + ex.Message);
            }
            finally
            {
                stream.Close();
            }
        }
    }
}
