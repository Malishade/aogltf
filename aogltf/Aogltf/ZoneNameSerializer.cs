using System.IO.Compression;
using System.Text;

public static class ZoneNameSerializer
{

    public static byte[] SerializeCompressed(Dictionary<int, string> data)
    {
        byte[] raw = Serialize(data);

        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Optimal))
            gz.Write(raw, 0, raw.Length);

        return output.ToArray();
    }

    public static Dictionary<int, string> DeserializeCompressed(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return Deserialize(output.ToArray());
    }

    public static byte[] Serialize(Dictionary<int, string> data)
    {
        var freq = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var v in data.Values)
            freq[v] = freq.TryGetValue(v, out int c) ? c + 1 : 1;

        var table = freq.OrderByDescending(kv => kv.Value)
                        .Select(kv => kv.Key)
                        .ToList();

        var tableIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < table.Count; i++)
            tableIndex[table[i]] = i;

        var sorted = data.OrderBy(kv => kv.Key).ToList();

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        WriteVarInt(writer, table.Count);
        foreach (var s in table)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            WriteVarInt(writer, bytes.Length);
            writer.Write(bytes);
        }

        WriteVarInt(writer, sorted.Count);

        int prevKey = 0;
        foreach (var kv in sorted)
        {
            int delta = kv.Key - prevKey;
            WriteVarInt(writer, delta);
            WriteVarInt(writer, tableIndex[kv.Value]);
            prevKey = kv.Key;
        }

        return ms.ToArray();
    }

    public static Dictionary<int, string> Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

        int tableSize = ReadVarInt(reader);
        var table = new string[tableSize];
        for (int i = 0; i < tableSize; i++)
        {
            int len = ReadVarInt(reader);
            byte[] b = reader.ReadBytes(len);
            table[i] = Encoding.UTF8.GetString(b);
        }

        int count = ReadVarInt(reader);
        var result = new Dictionary<int, string>(count);

        int key = 0;
        for (int i = 0; i < count; i++)
        {
            key += ReadVarInt(reader);
            int idx = ReadVarInt(reader);
            result[key] = table[idx];
        }

        return result;
    }

    private static void WriteVarInt(BinaryWriter w, int value)
    {
        uint v = (uint)value;
        while (v >= 0x80) { w.Write((byte)(v | 0x80)); v >>= 7; }
        w.Write((byte)v);
    }

    private static int ReadVarInt(BinaryReader r)
    {
        uint result = 0; int shift = 0;
        while (true)
        {
            byte b = r.ReadByte();
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }
        return (int)result;
    }
}