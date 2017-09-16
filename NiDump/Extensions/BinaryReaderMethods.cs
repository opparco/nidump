using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using SharpDX;

namespace NiDump
{
    public static class BinaryReaderMethods
    {
        public static void ReadVector3(this BinaryReader reader, out Vector3 v)
        {
            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
            v.Z = reader.ReadSingle();
        }

        public static void ReadVector4(this BinaryReader reader, out Vector4 v)
        {
            v.W = reader.ReadSingle();
            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
            v.Z = reader.ReadSingle();
        }

        public static void ReadQuaternion(this BinaryReader reader, out Quaternion q)
        {
            q.W = reader.ReadSingle();
            q.X = reader.ReadSingle();
            q.Y = reader.ReadSingle();
            q.Z = reader.ReadSingle();
        }

        public static void ReadMatrix3x3(this BinaryReader reader, out Matrix3x3 m)
        {
            m.M11 = reader.ReadSingle();
            m.M21 = reader.ReadSingle();
            m.M31 = reader.ReadSingle();

            m.M12 = reader.ReadSingle();
            m.M22 = reader.ReadSingle();
            m.M32 = reader.ReadSingle();

            m.M13 = reader.ReadSingle();
            m.M23 = reader.ReadSingle();
            m.M33 = reader.ReadSingle();
        }

        // read HeaderString
        // value: string (until #10)
        public static string ReadHeaderString(this BinaryReader reader)
        {
            StringBuilder string_builder = new StringBuilder();
            while (true)
            {
                char c = reader.ReadChar();
                if (c == 10)
                    break;
                string_builder.Append(c);
            }
            return string_builder.ToString();
        }

        // read ShortString
        // len: byte
        // value: array of char (null terminated)
        public static string ReadShortString(this BinaryReader reader)
        {
            byte len = reader.ReadByte();
            StringBuilder string_builder = new StringBuilder();
            while (string_builder.Length != len)
            {
                char c = reader.ReadChar();
                if (c == 0)
                    break;
                string_builder.Append(c);
            }
            return string_builder.ToString();
        }

        // read SizedString
        // len: uint
        // value: array of char
        public static string ReadSizedString(this BinaryReader reader)
        {
            uint len = reader.ReadUInt32();
            StringBuilder string_builder = new StringBuilder();
            while (string_builder.Length != len)
            {
                char c = reader.ReadChar();
                if (c == 0)
                    break;
                string_builder.Append(c);
            }
            return string_builder.ToString();
        }
    }
}