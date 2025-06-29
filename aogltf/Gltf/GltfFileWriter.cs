using System.Text.Json;

namespace aogltf
{
    internal class GltfFileWriter
    {
        private static class Constants
        {
            // GLTF file format constants
            public const uint GLTF_MAGIC = 0x46546C67; // "glTF"
            public const int GLTF_VERSION = 2;
            public const uint JSON_CHUNK_TYPE = 0x4E4F534A; // "JSON"
            public const uint BIN_CHUNK_TYPE = 0x004E4942;  // "BIN\0"

            // Padding constants
            public const byte JSON_PADDING_BYTE = 0x20; // Space character
            public const byte BIN_PADDING_BYTE = 0x00;  // Null byte

            // File structure sizes
            public const int GLTF_HEADER_SIZE = 12; // magic + version + length
            public const int CHUNK_HEADER_SIZE = 8; // length + type
        }

        public static void WriteToFile(string path, Gltf gltf, byte[] bufferData, MaterialBuilder? materialBuilder = null)
        {
            materialBuilder?.AddToGltf(gltf);

            byte[] jsonBytes = SerializeGltfToJson(gltf);
            ValidateGltfData(bufferData, jsonBytes);

            FileSizes fileSizes = CalculateFileSizes(jsonBytes.Length, bufferData.Length);

            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);

            WriteGlbHeader(writer, fileSizes.TotalSize);
            WriteJsonChunk(writer, jsonBytes, fileSizes.JsonChunkSize);
            WriteBinaryChunk(writer, bufferData, fileSizes.BinChunkSize);
        }

        public static void WriteToFile(string path, Gltf gltf, MaterialBuilder? materialBuilder = null)
        {
            materialBuilder?.AddToGltf(gltf);

            byte[] jsonBytes = SerializeGltfToJson(gltf);
            File.WriteAllBytes(path, jsonBytes);
        }

        private static byte[] SerializeGltfToJson(Gltf gltf)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = null
            };
            return JsonSerializer.SerializeToUtf8Bytes(gltf, options);
        }

        private static void ValidateGltfData(byte[] bufferData, byte[] jsonBytes)
        {
            if (bufferData == null)
                throw new ArgumentNullException(nameof(bufferData), "Binary data cannot be null");

            if (jsonBytes.Length == 0)
                throw new ArgumentException("JSON data cannot be empty", nameof(jsonBytes));
        }

        private static FileSizes CalculateFileSizes(int jsonLength, int binLength)
        {
            int jsonChunkSize = Align4(jsonLength);
            int binChunkSize = Align4(binLength);
            int totalSize = Constants.GLTF_HEADER_SIZE +
                           Constants.CHUNK_HEADER_SIZE + jsonChunkSize +
                           Constants.CHUNK_HEADER_SIZE + binChunkSize;

            return new FileSizes(totalSize, jsonChunkSize, binChunkSize);
        }

        private static void WriteGlbHeader(BinaryWriter writer, int totalLength)
        {            
            // Write file header
            writer.Write(Constants.GLTF_MAGIC);
            writer.Write(Constants.GLTF_VERSION);
            writer.Write(totalLength);
        }

        private static void WriteJsonChunk(BinaryWriter writer, byte[] jsonBytes, int chunkSize)
        {
            // Write chunk header
            writer.Write(chunkSize);
            writer.Write(Constants.JSON_CHUNK_TYPE);

            // Write JSON data
            writer.Write(jsonBytes);

            // Pad with spaces to maintain 4-byte alignment
            WritePadding(writer, chunkSize - jsonBytes.Length, Constants.JSON_PADDING_BYTE);
        }

        private static void WriteBinaryChunk(BinaryWriter writer, byte[] binData, int chunkSize)
        {
            // Write chunk header
            writer.Write(chunkSize);
            writer.Write(Constants.BIN_CHUNK_TYPE);

            // Write binary data
            writer.Write(binData);

            // Pad with null bytes to maintain 4-byte alignment
            WritePadding(writer, chunkSize - binData.Length, Constants.BIN_PADDING_BYTE);
        }

        private static void WritePadding(BinaryWriter writer, int paddingBytes, byte paddingValue)
        {
            for (int i = 0; i < paddingBytes; i++)
            {
                writer.Write(paddingValue);
            }
        }

        private static int Align4(int value) => (value + 3) & ~3;

        private readonly record struct FileSizes(int TotalSize, int JsonChunkSize, int BinChunkSize);
    }
}