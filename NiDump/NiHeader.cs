using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NiDump
{
    public class NiBlock
    {
        public ushort type;
        public uint size;
        public long offset;
        public byte[] data;

        public void Read(BinaryReader reader)
        {
            this.data = reader.ReadBytes((int)this.size);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.data);
        }
    }

    /**
     * Gamebryo File Format, Version 20.2.0.7
     */
    public class NiHeader
    {
        public string header_string;
        public uint version;
        public byte endian_type;
        public uint user_version;
        public uint user_version_2;

        // Export Info
        public string creator;
        public string export_info_1;
        public string export_info_2;

        public string max_filepath; // include Fallout 4

        public string[] block_types;
        public NiBlock[] blocks;
        public string[] strings;

        public uint unknown_int_2;

        public void Read(BinaryReader reader)
        {
            this.header_string = reader.ReadHeaderString();

            this.version = reader.ReadUInt32();
            this.endian_type = reader.ReadByte();
            this.user_version = reader.ReadUInt32();
            uint num_blocks = reader.ReadUInt32();
            this.user_version_2 = reader.ReadUInt32();

            this.creator = reader.ReadShortString();
            this.export_info_1 = reader.ReadShortString();
            this.export_info_2 = reader.ReadShortString();
            if (this.user_version_2 == 130)
                this.max_filepath = reader.ReadShortString();

            ushort num_block_types = reader.ReadUInt16();
            this.block_types = new string[num_block_types];

            for (int i = 0; i < num_block_types; i++)
                this.block_types[i] = reader.ReadSizedString();

            this.blocks = new NiBlock[num_blocks];

            for (int i = 0; i < num_blocks; i++)
                this.blocks[i] = new NiBlock();

            for (int i = 0; i < num_blocks; i++)
                this.blocks[i].type = reader.ReadUInt16();

            for (int i = 0; i < num_blocks; i++)
                this.blocks[i].size = reader.ReadUInt32();

            uint num_strings = reader.ReadUInt32();
            this.strings = new string[num_strings];

            uint max_string_len = reader.ReadUInt32();

            for (int i = 0; i < num_strings; i++)
                this.strings[i] = reader.ReadSizedString();

            this.unknown_int_2 = reader.ReadUInt32();
        }

        public void Dump()
        {
            Console.WriteLine("-- dump Header --");

            Console.WriteLine(this.header_string);

            Console.WriteLine("Version: 0x{0:X8}", this.version);
            Console.WriteLine("Endian Type: {0}", this.endian_type);
            Console.WriteLine("User Version: {0}", this.user_version);
            Console.WriteLine("User Version 2: {0}", this.user_version_2);

            // Export Info
            Console.WriteLine("Creator: {0}", this.creator);
            Console.WriteLine("Export Info 1: {0}", this.export_info_1);
            Console.WriteLine("Export Info 2: {0}", this.export_info_2);
            if (this.user_version_2 == 130)
                Console.WriteLine("Max Filepath: {0}", this.max_filepath);

            Console.WriteLine("#Block Types: {0}", this.block_types.Length);
            foreach (string block_type in this.block_types)
                Console.WriteLine("Block Type: {0}", block_type);

            Console.WriteLine("#Blocks: {0}", this.blocks.Length);
            foreach (NiBlock block in this.blocks)
                Console.WriteLine("Block Type: {0} Size: {1} Offset: 0x{2:x4}", block.type, block.size, block.offset);

            //Console.WriteLine("Max String Len: {0}", this.max_string_len);

            Console.WriteLine("#Strings: {0}", this.strings.Length);
            foreach (string str in this.strings)
                Console.WriteLine("String: {0}", str);

            Console.WriteLine("Unknown Int 2: {0}", this.unknown_int_2);
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteHeaderString(this.header_string);

            writer.Write(this.version);
            writer.Write(this.endian_type);
            writer.Write(this.user_version);
            int num_blocks = this.blocks.Length;
            writer.Write((uint)num_blocks);
            writer.Write(this.user_version_2);

            writer.WriteShortString(this.creator);
            writer.WriteShortString(this.export_info_1);
            writer.WriteShortString(this.export_info_2);
            if (this.user_version_2 == 130)
                writer.WriteShortString(this.max_filepath);

            writer.Write((ushort)this.block_types.Length);

            foreach (string block_type in this.block_types)
                writer.WriteSizedString(block_type);

            for (int i = 0; i < num_blocks; i++)
                writer.Write((ushort)this.blocks[i].type);

            for (int i = 0; i < num_blocks; i++)
                writer.Write((uint)this.blocks[i].size);

            int num_strings = this.strings.Length;
            writer.Write((uint)num_strings);

            int max_string_len = 0;
            for (int i = 0; i < num_strings; i++)
            {
                int len = this.strings[i].Length;
                if (max_string_len < len)
                    max_string_len = len;
            }
            writer.Write((uint)max_string_len);

            for (int i = 0; i < num_strings; i++)
                writer.WriteSizedString(this.strings[i]);

            writer.Write(this.unknown_int_2);
        }

        public long SetBlocksOffset(long offset)
        {
            foreach (NiBlock block in this.blocks)
            {
                block.offset = offset;
                offset += block.size;
            }
            return offset;
        }

        // 特定の block type を探索する
        public int GetBlockTypeIdxByName(string name)
        {
            int num_block_types = this.block_types.Length;

            int idx = -1;
            for (int i = 0; i < num_block_types; i++)
            {
                if (this.block_types[i] == name)
                {
                    idx = i;
                    break;
                }
            }
            return idx;
        }

        // 特定の string を探索する
        public int GetStringIdxByName(string name)
        {
            int num_strings = this.strings.Length;

            int idx = -1;
            for (int i = 0; i < num_strings; i++)
            {
                if (this.strings[i] == name)
                {
                    idx = i;
                    break;
                }
            }
            return idx;
        }

        public static NiHeader Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
            {
                return Load(source_stream);
            }
        }

        public static NiHeader Load(Stream source_stream)
        {
            BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            NiHeader header = new NiHeader();
            header.Read(reader);

            header.SetBlocksOffset(source_stream.Position);
            //header.Dump();

            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                header.blocks[i].Read(reader);
            }
            return header;
        }

        public T GetObject<T>(int object_ref) where T : NiObject, new()
        {
            //TODO: cmp T and header.block_types[object_ref]

            T instance;

            using (MemoryStream stream = new MemoryStream(this.blocks[object_ref].data))
            {
                BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.Default);

                instance = new T();
                instance.Read(reader);
            }
            return instance;
        }
    }
}
