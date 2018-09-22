using System.IO;

namespace NiDump
{
    using ObjectRef = System.Int32;
    using StringRef = System.Int32;

    // Skinning instance.
    public class NiSkinInstance : NiObject
    {
        // Skinning data reference.
        public ObjectRef data;
        // Refers to a NiSkinPartition objects, which partitions the mesh such that every vertex is only influenced by a limited number of bones.
        public ObjectRef skin_partition;
        // Armature root node.
        public ObjectRef skeleton_root;
        // The number of node bones referenced as influences.
        public uint num_bones;
        // List of all armature bones.
        public ObjectRef[] bones;

        public override void Read(BinaryReader reader)
        {
            this.data = reader.ReadInt32();
            this.skin_partition = reader.ReadInt32();
            this.skeleton_root = reader.ReadInt32();
            this.num_bones = reader.ReadUInt32();
            this.bones = new ObjectRef[num_bones];
            for (int i = 0; i < num_bones; i++)
            {
                bones[i] = reader.ReadInt32();
            }
        }

        public override void Dump()
        {
            System.Console.WriteLine("-- NiSkinInstance --");

            System.Console.WriteLine("num_bones:{0}", num_bones);
        }
    }

    // Skinning data for a submesh, optimized for hardware skinning. Part of NiSkinPartition.
    public struct SkinPartition
    {
        // Number of vertices in this submesh.
        ushort num_vertices;
        // Number of triangles in this submesh.
        ushort num_triangles;
        // Number of bones influencing this submesh.
        ushort num_bones;
        // Number of strips in this submesh (zero if not stripped).
        ushort num_strips;
        // Number of weight coefficients per vertex. The Gamebryo engine seems to work well only if this number is equal to 4, even if there are less than 4 influences per vertex.
        ushort num_weights_per_vertex;

        // List of bones.
        ushort[] bones;

        // Do we have a vertex map?
        bool has_vertex_map;
        // Maps the weight/influence lists in this submesh to the vertices in the shape being skinned.
        ushort[] vertex_map;

        // Do we have vertex weights?
        bool has_vertex_weights;
        // The vertex weights.
        float[] vertex_weights;

        // The strip lengths.
        ushort[] strip_lengths;

        // Do we have triangle or strip data?
        bool has_faces;
        // The strips.
        ushort[][] strips;
        // The triangles.
        Triangle[] triangles;

        // Do we have bone indices?
        bool has_bone_indices;
        // Bone indices, they index into 'bones'.
        byte[] bone_indices;

        // Unknown.
        ushort unknown;

        public void Read(BinaryReader reader)
        {
            this.num_vertices = reader.ReadUInt16();
            this.num_triangles = reader.ReadUInt16();
            this.num_bones = reader.ReadUInt16();
            this.num_strips = reader.ReadUInt16();
            this.num_weights_per_vertex = reader.ReadUInt16();

            // read bones
            this.bones = new ushort[num_bones];
            for (int i = 0; i < num_bones; i++)
            {
                bones[i] = reader.ReadUInt16();
            }

            this.has_vertex_map = reader.ReadByte() != 0;
            if (has_vertex_map)
            {
                // read vertex map
                this.vertex_map = new ushort[num_vertices];
                for (int i = 0; i < num_vertices; i++)
                {
                    vertex_map[i] = reader.ReadUInt16();
                }
            }

            this.has_vertex_weights = reader.ReadByte() != 0;
            if (has_vertex_weights)
            {
                // read vertex weights
                this.vertex_weights = new float[num_vertices * num_weights_per_vertex];
                for (int i = 0; i < num_vertices * num_weights_per_vertex; i++)
                {
                    vertex_weights[i] = reader.ReadSingle();
                }
            }

            // read strip_lengths
            this.strip_lengths = new ushort[num_strips];
            for (int i = 0; i < num_strips; i++)
            {
                strip_lengths[i] = reader.ReadUInt16();
            }

            this.has_faces = reader.ReadByte() != 0;
            if (has_faces)
            {
                if (num_strips != 0)
                {
                    strips = new ushort[num_strips][];
                    for (int i = 0; i < num_strips; i++)
                    {
                        ushort strip_length = strip_lengths[i];
                        ushort[] strip = new ushort[strip_length];
                        for (int j = 0; j < strip_length; j++)
                        {
                            strip[j] = reader.ReadUInt16();
                        }
                        strips[i] = strip;
                    }
                }
                else
                {
                    this.triangles = new Triangle[num_triangles];
                    for (int i = 0; i < num_triangles; i++)
                    {
                        triangles[i] = new Triangle();
                        triangles[i].Read(reader);
                    }
                }
            }

            this.has_bone_indices = reader.ReadByte() != 0;
            if (has_bone_indices)
            {
                // read bone_indices
                this.bone_indices = new byte[num_vertices * num_weights_per_vertex];
                for (int i = 0; i < num_vertices * num_weights_per_vertex; i++)
                {
                    bone_indices[i] = reader.ReadByte();
                }
            }
            this.unknown = reader.ReadUInt16();
        }

        public void Dump()
        {
            System.Console.WriteLine("-- SkinPartition --");

            System.Console.WriteLine("num_vertices:{0}", num_vertices);
            System.Console.WriteLine("num_triangles:{0}", num_triangles);
            System.Console.WriteLine("num_bones:{0}", num_bones);
            System.Console.WriteLine("num_strips:{0}", num_strips);
            System.Console.WriteLine("num_weights_per_vertex:{0}", num_weights_per_vertex);

            System.Console.WriteLine("has_vertex_map:{0}", has_vertex_map);
            System.Console.WriteLine("has_vertex_weights:{0}", has_vertex_weights);
            System.Console.WriteLine("has_faces:{0}", has_faces);
            System.Console.WriteLine("has_bone_indices:{0}", has_bone_indices);
            System.Console.WriteLine("unknown:{0}", unknown);
        }
    }

    // Skinning data, optimized for hardware skinning. The mesh is partitioned in submeshes such that each vertex of a submesh is influenced only by a limited and fixed number of bones.
    public class NiSkinPartition : NiObject
    {

        public uint num_skin_partitions;
        // Skin partition objects.
        public SkinPartition[] skin_partitions;

        public override void Read(BinaryReader reader)
        {
            this.num_skin_partitions = reader.ReadUInt32();
            this.skin_partitions = new SkinPartition[num_skin_partitions];
            for (int i = 0; i < num_skin_partitions; i++)
            {
                skin_partitions[i] = new SkinPartition();
                skin_partitions[i].Read(reader);
            }
        }

        public override void Dump()
        {
            System.Console.WriteLine("-- NiSkinPartition --");

            System.Console.WriteLine("num_skin_partitions:{0}", num_skin_partitions);
            for (int i = 0; i < num_skin_partitions; i++)
            {
                skin_partitions[i].Dump();
            }
        }
    }
}
