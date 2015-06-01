using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChineseCheckers
{
    public class DictionaryIO
    {
        internal static void write(Dictionary<Action, int> dictionary, string file)
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

        internal static Dictionary<Action, int> read(string file)
        {
            var result = new Dictionary<Action, int>();
            try
            {
                using (FileStream fs = File.OpenRead(file))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // Get count.
                    int count = reader.ReadInt32();
                    // Read in all pairs.
                    for (int i = 0; i < count; i++)
                    {
                        Action key = Action.fromBinary(reader);
                        int value = reader.ReadInt32();
                        result[key] = value;
                    }
                }
            } catch (Exception ex) { // if file doesn't exist, don't brutally end the program
                Console.WriteLine(ex.StackTrace);
            }
            return result;
        }
    }
}
