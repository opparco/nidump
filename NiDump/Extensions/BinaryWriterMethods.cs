using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using SharpDX;

namespace NiDump
{
    public static class BinaryWriterMethods
    {
        public static void Write(this BinaryWriter writer, ref Vector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public static void Write(this BinaryWriter writer, ref Vector4 v)
        {
            writer.Write(v.W);
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public static void Write(this BinaryWriter writer, ref Quaternion q)
        {
            writer.Write(q.W);
            writer.Write(q.X);
            writer.Write(q.Y);
            writer.Write(q.Z);
        }

        public static void Write(this BinaryWriter writer, ref Matrix3x3 m)
        {
            writer.Write(m.M11);
            writer.Write(m.M21);
            writer.Write(m.M31);

            writer.Write(m.M12);
            writer.Write(m.M22);
            writer.Write(m.M32);

            writer.Write(m.M13);
            writer.Write(m.M23);
            writer.Write(m.M33);
        }

        public static void WriteHeaderString(this BinaryWriter writer, string value)
        {
            foreach (byte i in Encoding.Default.GetBytes(value))
                writer.Write(i);

            writer.Write((byte)10);
        }

        public static void WriteShortString(this BinaryWriter writer, string value)
        {
            int len = value.Length + 1;  // include null terminator
            writer.Write((byte)len);
            foreach (byte i in Encoding.Default.GetBytes(value))
                writer.Write(i);

            writer.Write((byte)0);
        }

        public static void WriteSizedString(this BinaryWriter writer, string value)
        {
            writer.Write((uint)value.Length);
            foreach (byte i in Encoding.Default.GetBytes(value))
                writer.Write(i);
        }
    }
}