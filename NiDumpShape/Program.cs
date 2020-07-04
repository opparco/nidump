using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SharpDX;

using NiDump;

namespace NiDumpShape
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: NiDumpShape <nif file>");
                return;
            }

            string source_file = args[0];

            string[] files = Directory.GetFiles(source_file, "*.nif");
            foreach (string file in files)
            {
                string dest_file = Path.Combine("output", Path.GetFileName(file));
                Program program = new Program();
                Console.WriteLine("processing " + file);
                program.Load(file);
                program.SaveMqoFile("out.mqo");
                //if (program.UpdateTriShapes())
                //    program.Save(dest_file);
            }
        }

        NiHeader header;
        NiFooter footer;
        Dictionary<ObjectRef, BSTriShape> triShapes;

        public void Load(string source_file)
        {
            using (Stream source_stream = File.OpenRead(source_file))
                Load(source_stream);
        }

        public void Load(Stream source_stream)
        {
            BinaryReader reader = new BinaryReader(source_stream, System.Text.Encoding.Default);

            header = new NiHeader();
            header.Read(reader);

            //header.Dump();

            int num_blocks = header.blocks.Length;

            for (int i = 0; i < num_blocks; i++)
            {
                header.blocks[i].Read(reader);
            }

            footer = new NiFooter();
            footer.Read(reader);

            NiObject.user_version = header.user_version;
            NiObject.user_version_2 = header.user_version_2;

            int bt_BSTriShape = header.GetBlockTypeIdxByName("BSTriShape");
            int bt_BSSubIndexTriShape = header.GetBlockTypeIdxByName("BSSubIndexTriShape");
            int bt_BSSkinInstance = header.GetBlockTypeIdxByName("BSSkin::Instance");
            int bt_BSSkinBoneData = header.GetBlockTypeIdxByName("BSSkin::BoneData");
#if false
            int bt_NiTriShapeData = header.GetBlockTypeIdxByName("NiTriShapeData");
            int bt_BSLightingShaderProperty = header.GetBlockTypeIdxByName("BSLightingShaderProperty");
            int bt_BSShaderTextureSet = header.GetBlockTypeIdxByName("BSShaderTextureSet");
            int bt_NiSkinInstance = header.GetBlockTypeIdxByName("NiSkinInstance");
            int bt_NiSkinPartition = header.GetBlockTypeIdxByName("NiSkinPartition");
#endif

            triShapes = new Dictionary<ObjectRef, BSTriShape>();

            for (int i = 0; i < num_blocks; i++)
            {
                if (header.blocks[i].type == bt_BSTriShape)
                {
                    BSTriShape triShape = GetObject<BSTriShape>(header, i);
                    triShape.Dump();
                    triShapes[i] = triShape;
                }
                if (header.blocks[i].type == bt_BSSubIndexTriShape)
                {
                    BSSubIndexTriShape triShape = GetObject<BSSubIndexTriShape>(header, i);
                    triShape.Dump();
                    triShapes[i] = triShape;
                }
                if (header.blocks[i].type == bt_BSSkinInstance)
                {
                    BSSkinInstance instance = GetObject<BSSkinInstance>(header, i);
                    instance.Dump();
                }
                if (header.blocks[i].type == bt_BSSkinBoneData)
                {
                    BSSkinBoneData bone_data = GetObject<BSSkinBoneData>(header, i);
                    bone_data.Dump();
                }
#if false
                if (header.blocks[i].type == bt_NiTriShapeData)
                {
                    NiTriShapeData triShapeData = GetObject<NiTriShapeData>(header, i);
                    triShapeData.Dump();
                }
                if (header.blocks[i].type == bt_BSLightingShaderProperty)
                {
                    BSLightingShaderProperty lightingShaderProperty = GetObject<BSLightingShaderProperty>(header, i);
                    lightingShaderProperty.Dump();
                }
                if (header.blocks[i].type == bt_BSShaderTextureSet)
                {
                    BSShaderTextureSet shaderTextureSet = GetObject<BSShaderTextureSet>(header, i);
                    shaderTextureSet.Dump();
                }
                if (header.blocks[i].type == bt_NiSkinInstance)
                {
                    NiSkinInstance skinInstance = GetObject<NiSkinInstance>(header, i);
                    skinInstance.Dump();
                    foreach (ObjectRef boneref in skinInstance.bones)
                    {
                        NiNode node = GetObject<NiNode>(header, boneref);
                        System.Console.WriteLine(header.strings[node.name]);
                    }
                }
                if (header.blocks[i].type == bt_NiSkinPartition)
                {
                    NiSkinPartition skinPartition = GetObject<NiSkinPartition>(header, i);
                    skinPartition.Dump();
                }
#endif
            }
        }

#if false
        bool UpdateTriShapes()
        {
            bool updated = false;

            foreach (ObjectRef triShape_ref in triShapes.Keys)
            {
                BSSubIndexTriShape triShape = triShapes[triShape_ref];

                if (triShape.data_size == 0)
                    continue;

                if (triShape.num_segments != triShape.total_segments)
                {
                    Console.WriteLine("num_segments: {0} != total_segments {1}", triShape.num_segments, triShape.total_segments);
                    continue;
                }
                updated = true;

                triShape.num_segments = 2;
                triShape.total_segments = 4; // +2
                Array.Resize(ref triShape.segment, 2);
                {
                    BSGeometrySegmentData seg = new BSGeometrySegmentData();
                    seg.start_index = 0;
                    seg.num_primitives = 0;

                    seg.num_sub_segments = 0;

                    triShape.segment[0] = seg;
                }

                {
                    BSGeometrySegmentData seg = new BSGeometrySegmentData();
                    seg.start_index = 0;
                    seg.num_primitives = triShape.num_primitives;

                    seg.num_sub_segments = 2;
                    seg.sub_segment = new BSGeometrySubSegmentData[2];

                    {
                        // create sub segment
                        BSGeometrySubSegmentData sub = new BSGeometrySubSegmentData();
                        sub.start_index = 0;
                        sub.num_primitives = triShape.num_primitives;
                        sub.parent_array_index = 1;

                        // attach
                        seg.sub_segment[0] = sub;
                    }

                    {
                        // create sub segment
                        BSGeometrySubSegmentData sub = new BSGeometrySubSegmentData();
                        sub.start_index = triShape.num_primitives * 3;
                        sub.num_primitives = 0;
                        sub.parent_array_index = 1;

                        // attach
                        seg.sub_segment[1] = sub;
                    }
                    triShape.segment[1] = seg;
                }

                {
                    triShape.segment_data = new BSGeometrySegmentSharedData();
                    ref BSGeometrySegmentSharedData seg = ref triShape.segment_data;

                    seg.num_segments = 2;
                    seg.total_segments = 4;

                    seg.segment_starts = new uint[2];
                    seg.segment_starts[0] = 0;
                    seg.segment_starts[1] = 1;

                    seg.per_segment = new BSGeometryPerSegmentSharedData[4];

                    seg.per_segment[0] = new BSGeometryPerSegmentSharedData();
                    seg.per_segment[0].user_index = 0;
                    seg.per_segment[0].bone_id = 4294967295;

                    seg.per_segment[1] = new BSGeometryPerSegmentSharedData();
                    seg.per_segment[1].user_index = 1;
                    seg.per_segment[1].bone_id = 4294967295;

                    seg.per_segment[2] = new BSGeometryPerSegmentSharedData();
                    seg.per_segment[2].user_index = 31;
                    seg.per_segment[2].bone_id = 2260150656;

                    seg.per_segment[3] = new BSGeometryPerSegmentSharedData();
                    seg.per_segment[3].user_index = 30;
                    seg.per_segment[3].bone_id = 2260150656;
                }
                //triShape.Dump();

#if false
                //SharpDX.Mathematics/BoundingSphere.cs
                //
                //Find the center of all vertices.
                Vector3 center = Vector3.Zero;
                foreach (BSVertexData v in triShape.vertex_data)
                {
                    Vector3 co = v.vertex;
                    Vector3.Add(ref co, ref center, out center);
                }

                //This is the center of our sphere.
                center /= (float)triShape.num_vertices;

                //Find the radius of the sphere
                float radius = 0;
                foreach (BSVertexData v in triShape.vertex_data)
                {
                    Vector3 co = v.vertex;
                    float distance;
                    Vector3.DistanceSquared(ref center, ref co, out distance);
                    if (radius < distance)
                        radius = distance;
                }

                //Find the real distance from the DistanceSquared.
                radius = (float)Math.Sqrt(radius);

                center.Z += 120.0f;

                triShape.center = center;
                triShape.radius = radius;
#endif
            }
            return updated;
        }
#endif

        public void Save(string dest_file)
        {
            using (Stream dest_stream = File.Create(dest_file))
                Save(dest_stream);
        }

        public void Save(Stream dest_stream)
        {
            BinaryWriter writer = new BinaryWriter(dest_stream, System.Text.Encoding.Default);

            header.Write(writer);

            int num_blocks = header.blocks.Length;
            for (int i = 0; i < num_blocks; i++)
            {
                BSTriShape triShape;
                if (triShapes.TryGetValue(i, out triShape))
                    triShape.Write(writer);
                else
                    header.blocks[i].Write(writer);
            }

            footer.Write(writer);
        }

        public string GetString(StringRef string_ref)
        {
            return string_ref != -1 ? header.strings[string_ref] : "(undefined)";
        }

        void SaveMqoFile(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("Metasequoia Document");
                writer.WriteLine("Format Text Ver 1.0");
                writer.WriteLine("");
                writer.WriteLine("Scene {");
                writer.WriteLine("\tpos 0 0 1500");
                writer.WriteLine("\tlookat 0 0 0");
                writer.WriteLine("\thead -0.5236");
                writer.WriteLine("\tpich 0.5236");
                writer.WriteLine("\tortho 1");
                writer.WriteLine("\tzoom2 5.0000");
                writer.WriteLine("\tamb 0.250 0.250 0.250");
                writer.WriteLine("}");
                writer.WriteLine("Material 1 {");
                writer.WriteLine("\t\"mat1\" col(1.000 1.000 1.000 1.000) dif(0.800) amb(0.600) emi(0.000) spc(0.000) power(5.00) tex(\"mat1.png\")");
                writer.WriteLine("}");
                foreach (ObjectRef triShape_ref in triShapes.Keys)
                {
                    BSTriShape triShape = triShapes[triShape_ref];

                    if (triShape.data_size == 0)
                        continue;

                    writer.WriteLine("Object \"{0}\" {{", GetString(triShape.name));
                    writer.WriteLine("\tvisible 15");
                    writer.WriteLine("\tlocking 0");
                    writer.WriteLine("\tshading 1");
                    writer.WriteLine("\tfacet 59.5");
                    writer.WriteLine("\tcolor 0.898 0.498 0.698");
                    writer.WriteLine("\tcolor_type 0");
                    writer.WriteLine("\tvertex {0} {{", triShape.num_vertices);
                    foreach (BSVertexData vd in triShape.vertex_data)
                    {
                        writer.WriteLine("\t\t{0:F4} {1:F4} {2:F4}", vd.vertex.X, vd.vertex.Y, vd.vertex.Z);
                    }
                    writer.WriteLine("\t}");
                    writer.WriteLine("\tface {0} {{", triShape.num_triangles);
                    foreach (Triangle triangle in triShape.triangles)
                    {
                        BSVertexData v1, v2, v3;
                        v1 = triShape.vertex_data[triangle.v1];
                        v2 = triShape.vertex_data[triangle.v2];
                        v3 = triShape.vertex_data[triangle.v3];
                        writer.WriteLine("\t\t3 V({0} {2} {1}) M({3}) UV({4:F5} {5:F5} {8:F5} {9:F5} {6:F5} {7:F5})", triangle.v1, triangle.v2, triangle.v3, 0, v1.uv.X, v1.uv.Y, v2.uv.X, v2.uv.Y, v3.uv.X, v3.uv.Y);
                    }
                    writer.WriteLine("\t}");
                    writer.WriteLine("}");
                }
                writer.WriteLine("Eof");
            }
        }

        //TODO: nif

        static T GetObject<T>(NiHeader header, ObjectRef object_ref) where T : NiObject, new()
        {
            //TODO: cmp T and header.block_types[object_ref]

            T instance;

            using (MemoryStream stream = new MemoryStream(header.blocks[object_ref].data))
            {
                BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.Default);

                instance = new T();
                instance.Read(reader);
            }
            return instance;
        }
    }
}
