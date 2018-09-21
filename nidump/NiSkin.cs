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
            for (int i = 0; i < num_bones; i++)
            {
            }
            if (has_vertex_map)
            {
                // read vertex map
                for (int i = 0; i < num_vertices; i++)
                {
                }
            }
            if (has_vertex_weights)
            {
                // read vertex weights
                for (int i = 0; i < num_vertices; i++)
                {
                }
            }
            // read strip_lengths
            for (int i = 0; i < num_strips; i++)
            {
            }
            if (has_bone_indices)
            {
                // read bone_indices
                for (int i = 0; i < num_vertices; i++)
                {
                }
            }
        }

    }

    // Skinning data, optimized for hardware skinning. The mesh is partitioned in submeshes such that each vertex of a submesh is influenced only by a limited and fixed number of bones.
    public class NiSkinPartition : NiObject
    {

        uint num_skin_partitions;
        // Skin partition objects.
        SkinPartition[] skin_partitions;

        public override void Read(BinaryReader reader)
        {
        }

        public override void Dump()
        {
            System.Console.WriteLine("-- NiSkinPartition --");
        }
    }
}
