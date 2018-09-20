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
            v = new Vector3();
            for (int i = 0; i < 3; i++)
                v[i] = reader.ReadSingle();
        }

        public static void ReadVector4(this BinaryReader reader, out Vector4 v)
        {
            v = new Vector4();
            for (int i = 0; i < 4; i++)
                v[i] = reader.ReadSingle();
        }

        public static void ReadColor3(this BinaryReader reader, out Color3 c)
        {
            c = new Color3();
            for (int i = 0; i < 3; i++)
                c[i] = reader.ReadSingle();
        }

        public static void ReadColor4(this BinaryReader reader, out Color4 c)
        {
            c = new Color4();
            for (int i = 0; i < 4; i++)
                c[i] = reader.ReadSingle();
        }

        // order: w x y z
        public static void ReadQuaternion(this BinaryReader reader, out Quaternion q)
        {
            q = new Quaternion();

            q.W = reader.ReadSingle();
            q.X = reader.ReadSingle();
            q.Y = reader.ReadSingle();
            q.Z = reader.ReadSingle();
        }

        public static void ReadMatrix3x3(this BinaryReader reader, out Matrix3x3 m)
        {
            m = new Matrix3x3();

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
