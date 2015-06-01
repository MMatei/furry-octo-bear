using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChineseCheckers.Code
{
    public class DictionaryIO
    {
        public static void write(Dictionary<Action, string> dictionary, string file)
        {
            using (FileStream fs = File.OpenWrite(file))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Put count.
                writer.Write(dictionary.Count);
                // Write pairs.
                foreach (var pair in dictionary)
                {
                    pair.Key.toBinary(writer);
                    writer.Write(pair.Value);
                }
            }
        }

        public static Dictionary<Action, string> read(string file)
        {
            var result = new Dictionary<Action, string>();
            using (FileStream fs = File.OpenRead(file))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Get count.
                int count = reader.ReadInt32();
                // Read in all pairs.
                for (int i = 0; i < count; i++)
                {
                    Action key = Action.fromBinary(reader);
                    string value = reader.ReadString();
                    result[key] = value;
                }
            }
            return result;
        }
    }
}
