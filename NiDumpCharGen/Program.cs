using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

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

        public void Dump()
        {
            Console.WriteLine("\t\tdata: {0} index: {1}, key: {2}, type: {3}", data, index, key, type);
        }
    }
    [DataContract]
    class Key
    {
        [DataMember]
        internal string name;
        [DataMember]
        internal Value[] values;

        public void Dump()
        {
            Console.WriteLine("\tname: {0}", name);
            foreach (Value baz in values)
                baz.Dump();
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

        public void Dump()
        {
            if (firstPerson)
                return;

            Console.WriteLine("node: {0}", node);
            foreach (Key bar in keys)
                bar.Dump();
        }
    }
    [DataContract]
    class RaceMenuSlot
    {
        [DataMember]
        Transform[] transforms;

        public void Dump()
        {
            foreach (Transform foo in transforms)
                foo.Dump();
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

            Stream stream = File.OpenRead(source_file);
            var serializer = new DataContractJsonSerializer(typeof(RaceMenuSlot));
            RaceMenuSlot slot = (RaceMenuSlot)serializer.ReadObject(stream);
            slot.Dump();
        }
    }
}
