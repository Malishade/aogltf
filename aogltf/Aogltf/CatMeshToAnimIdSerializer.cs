
using System.IO.Compression;

public class CatMeshToAnimIdSerializer
{
    public static byte[] SerializeCompressed(Dictionary<int, List<int>> data)
    {
        byte[] serialized = Serialize(data);

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(serialized, 0, serialized.Length);
        }

        return output.ToArray();
    }

    public static Dictionary<int, List<int>> DeserializeCompressed(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        gzip.CopyTo(output);
        byte[] decompressed = output.ToArray();

        return Deserialize(decompressed);
    }

    public static byte[] Serialize(Dictionary<int, List<int>> data)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write number of entries
        WriteVarInt(writer, data.Count);

        foreach (var kvp in data)
        {
            // Write key
            WriteVarInt(writer, kvp.Key);

            // Write list length
            WriteVarInt(writer, kvp.Value.Count);

            if (kvp.Value.Count == 0) continue;

            // Write first value
            WriteVarInt(writer, kvp.Value[0]);

            // Write deltas (differences between consecutive values)
            for (int i = 1; i < kvp.Value.Count; i++)
            {
                int delta = kvp.Value[i] - kvp.Value[i - 1];
                WriteSignedVarInt(writer, delta);
            }
        }

        return ms.ToArray();
    }

    // Deserialize bytes back to dictionary
    public static Dictionary<int, List<int>> Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var result = new Dictionary<int, List<int>>();
        int count = ReadVarInt(reader);

        for (int i = 0; i < count; i++)
        {
            int key = ReadVarInt(reader);
            int listCount = ReadVarInt(reader);

            var list = new List<int>(listCount);

            if (listCount > 0)
            {
                // Read first value
                int current = ReadVarInt(reader);
                list.Add(current);

                // Read deltas and reconstruct values
                for (int j = 1; j < listCount; j++)
                {
                    int delta = ReadSignedVarInt(reader);
                    current += delta;
                    list.Add(current);
                }
            }

            result[key] = list;
        }

        return result;
    }

    // Variable-length integer encoding (unsigned)
    private static void WriteVarInt(BinaryWriter writer, int value)
    {
        uint v = (uint)value;
        while (v >= 0x80)
        {
            writer.Write((byte)(v | 0x80));
            v >>= 7;
        }
        writer.Write((byte)v);
    }

    private static int ReadVarInt(BinaryReader reader)
    {
        uint result = 0;
        int shift = 0;

        while (true)
        {
            byte b = reader.ReadByte();
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;
        }

        return (int)result;
    }

    // ZigZag encoding for signed integers (efficient for positive and negative deltas)
    private static void WriteSignedVarInt(BinaryWriter writer, int value)
    {
        // ZigZag encode: maps negatives to positive odd numbers
        uint encoded = (uint)((value << 1) ^ (value >> 31));

        while (encoded >= 0x80)
        {
            writer.Write((byte)(encoded | 0x80));
            encoded >>= 7;
        }
        writer.Write((byte)encoded);
    }

    private static int ReadSignedVarInt(BinaryReader reader)
    {
        uint result = 0;
        int shift = 0;

        while (true)
        {
            byte b = reader.ReadByte();
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;
        }

        // ZigZag decode
        return (int)(result >> 1) ^ -(int)(result & 1);
    }
}