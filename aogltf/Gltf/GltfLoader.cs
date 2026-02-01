using System.Text;
using System.Text.Json;

namespace gltf
{
    public class GltfLoader
    {
        public static bool Load(string path, out Gltf gltf, out byte[] bufferData)
        {
            gltf = null;
            bufferData = Array.Empty<byte>();

            try
            {
                if (!File.Exists(path))
                    return false;

                string extension = Path.GetExtension(path).ToLowerInvariant();

                return extension switch
                {
                    ".glb" => LoadGlb(path, out gltf, out bufferData),
                    ".gltf" => LoadGltf(path, out gltf, out bufferData),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static bool LoadGlb(string path, out Gltf? gltf, out byte[] bufferData)
        {
            gltf = null;
            bufferData = Array.Empty<byte>();

            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);

                uint magic = reader.ReadUInt32();
                if (magic != GltfConstants.GLTF_MAGIC)
                    return false;

                uint version = reader.ReadUInt32();
                if (version != GltfConstants.GLTF_VERSION)
                    return false;

                uint totalLength = reader.ReadUInt32();

                uint jsonChunkLength = reader.ReadUInt32();
                uint jsonChunkType = reader.ReadUInt32();

                if (jsonChunkType != GltfConstants.JSON_CHUNK_TYPE)
                    return false;

                byte[] jsonBytes = reader.ReadBytes((int)jsonChunkLength);
                string jsonString = Encoding.UTF8.GetString(jsonBytes).TrimEnd('\0', ' ');

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = null
                };
                gltf = JsonSerializer.Deserialize<Gltf>(jsonString, options);

                if (gltf == null)
                    return false;

                if (stream.Position < stream.Length)
                {
                    uint binChunkLength = reader.ReadUInt32();
                    uint binChunkType = reader.ReadUInt32();

                    if (binChunkType == GltfConstants.BIN_CHUNK_TYPE)
                    {
                        bufferData = reader.ReadBytes((int)binChunkLength);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool LoadGltf(string path, out Gltf? gltf, out byte[] bufferData)
        {
            gltf = null;
            bufferData = Array.Empty<byte>();

            try
            {
                string jsonString = File.ReadAllText(path);

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = null
                };
                gltf = JsonSerializer.Deserialize<Gltf>(jsonString, options);

                if (gltf == null)
                    return false;

                if (gltf.Buffers != null && gltf.Buffers.Length > 0)
                {
                    var buffer = gltf.Buffers[0];

                    if (!string.IsNullOrEmpty(buffer.Uri) && !buffer.Uri.StartsWith("data:"))
                    {
                        string directory = Path.GetDirectoryName(path) ?? "";
                        string bufferPath = Path.Combine(directory, buffer.Uri);

                        if (File.Exists(bufferPath))
                        {
                            bufferData = File.ReadAllBytes(bufferPath);
                        }
                    }
                    else if (buffer.Uri?.StartsWith("data:") == true)
                    {
                        bufferData = DecodeDataUri(buffer.Uri);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] DecodeDataUri(string dataUri)
        {
            try
            {
                // Data URI format: data:[<mediatype>][;base64],<data>
                int commaIndex = dataUri.IndexOf(',');
                if (commaIndex == -1)
                    return Array.Empty<byte>();

                string base64Data = dataUri.Substring(commaIndex + 1);
                return Convert.FromBase64String(base64Data);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}