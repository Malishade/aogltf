using System.Text.Json.Serialization;

namespace aogltf
{
    public class Gltf
    {
        [JsonPropertyName("asset")]
        public Asset Asset { get; set; }

        [JsonPropertyName("buffers")]
        public Buffer[] Buffers { get; set; }

        [JsonPropertyName("bufferViews")]
        public BufferView[] BufferViews { get; set; }

        [JsonPropertyName("accessors")]
        public Accessor[] Accessors { get; set; }

        [JsonPropertyName("meshes")]
        public Mesh[] Meshes { get; set; }

        [JsonPropertyName("nodes")]
        public Node[] Nodes { get; set; }

        [JsonPropertyName("scenes")]
        public Scene[] Scenes { get; set; }

        [JsonPropertyName("scene")]
        public int Scene { get; set; }
    }

    public class Asset
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.0";

        [JsonPropertyName("generator")]
        public string Generator { get; set; } = "aogltf";
    }

    public class Buffer
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("byteLength")]
        public int ByteLength { get; set; }
    }

    public class BufferView
    {
        [JsonPropertyName("buffer")]
        public int Buffer { get; set; }

        [JsonPropertyName("byteOffset")]
        public int ByteOffset { get; set; }

        [JsonPropertyName("byteLength")]
        public int ByteLength { get; set; }

        [JsonPropertyName("target")]
        public int? Target { get; set; } // 34962 = ARRAY_BUFFER, 34963 = ELEMENT_ARRAY_BUFFER
    }

    public class Accessor
    {
        [JsonPropertyName("bufferView")]
        public int BufferView { get; set; }

        [JsonPropertyName("byteOffset")]
        public int ByteOffset { get; set; } = 0;

        [JsonPropertyName("componentType")]
        public int ComponentType { get; set; } // 5126 = FLOAT, 5123 = UNSIGNED_SHORT

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // "SCALAR", "VEC3", etc.

        [JsonPropertyName("min")]
        public float[] Min { get; set; }

        [JsonPropertyName("max")]
        public float[] Max { get; set; }
    }

    public class Mesh
    {
        [JsonPropertyName("primitives")]
        public Primitive[] Primitives { get; set; }
    }

    public class Primitive
    {
        [JsonPropertyName("attributes")]
        public Dictionary<string, int> Attributes { get; set; }

        [JsonPropertyName("indices")]
        public int Indices { get; set; }

        [JsonPropertyName("mode")]
        public int Mode { get; set; } = 4; // TRIANGLES
    }

    public class Node
    {
        [JsonPropertyName("mesh")]
        public int? Mesh { get; set; } // Nullable - not all nodes have meshes

        [JsonPropertyName("children")]
        public int[]? Children { get; set; } // Child node indices

        [JsonPropertyName("translation")]
        public float[]? Translation { get; set; } // [x, y, z]

        [JsonPropertyName("rotation")]
        public float[]? Rotation { get; set; } // [x, y, z, w]

        [JsonPropertyName("scale")]
        public float[]? Scale { get; set; } // [x, y, z]

        [JsonPropertyName("matrix")]
        public float[]? Matrix { get; set; } // 4x4 transformation matrix (alternative to TRS)

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class Scene
    {
        [JsonPropertyName("nodes")]
        public int[] Nodes { get; set; }
    }
}
