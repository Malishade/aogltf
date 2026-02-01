using System.Text.Json.Serialization;

namespace gltf
{
    public static class GltfConstants
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

        //File write
        public const int ARRAY_BUFFER = 34962;
        public const int ELEMENT_ARRAY_BUFFER = 34963;
        public const int FLOAT = 5126;
        public const int UNSIGNED_SHORT = 5123;
        public const int UNSIGNED_BYTE = 5121;
        public const int TRIANGLES = 4;
    }

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

        [JsonPropertyName("materials")]
        public Material[]? Materials { get; set; }

        [JsonPropertyName("textures")]
        public Texture[]? Textures { get; set; }

        [JsonPropertyName("images")]
        public Image[]? Images { get; set; }

        [JsonPropertyName("samplers")]
        public Sampler[]? Samplers { get; set; }

        [JsonPropertyName("animations")]
        public Animation[]? Animations { get; set; }

        [JsonPropertyName("skins")]
        public Skin[]? Skins { get; set; }
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
        public float[]? Min { get; set; }

        [JsonPropertyName("max")]
        public float[]? Max { get; set; }
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

        [JsonPropertyName("material")]
        public int? Material { get; set; } // Reference to material index
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

        [JsonPropertyName("skin")]
        public int? Skin { get; set; } // Reference to skin index for skeletal meshes
    }

    public class Scene
    {
        [JsonPropertyName("nodes")]
        public int[] Nodes { get; set; }
    }

    public class Animation
    {
        [JsonPropertyName("channels")]
        public AnimationChannel[] Channels { get; set; }

        [JsonPropertyName("samplers")]
        public AnimationSampler[] Samplers { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class AnimationChannel
    {
        [JsonPropertyName("sampler")]
        public int Sampler { get; set; }

        [JsonPropertyName("target")]
        public AnimationTarget Target { get; set; }
    }

    public class AnimationTarget
    {
        [JsonPropertyName("node")]
        public int Node { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; } // "translation", "rotation", "scale"
    }

    public class AnimationSampler
    {
        [JsonPropertyName("input")]
        public int Input { get; set; } // Accessor index for keyframe times

        [JsonPropertyName("output")]
        public int Output { get; set; } // Accessor index for keyframe values

        [JsonPropertyName("interpolation")]
        public string Interpolation { get; set; } = "LINEAR"; // "LINEAR", "STEP", "CUBICSPLINE"
    }

    public class Skin
    {
        [JsonPropertyName("inverseBindMatrices")]
        public int? InverseBindMatrices { get; set; } // Accessor index for inverse bind matrices

        [JsonPropertyName("joints")]
        public int[]? Joints { get; set; } // Node indices that are joints

        [JsonPropertyName("skeleton")]
        public int? Skeleton { get; set; } // Root joint node index

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class Material
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("pbrMetallicRoughness")]
        public PbrMetallicRoughness? PbrMetallicRoughness { get; set; }

        [JsonPropertyName("normalTexture")]
        public TextureInfo? NormalTexture { get; set; }

        [JsonPropertyName("occlusionTexture")]
        public TextureInfo? OcclusionTexture { get; set; }

        [JsonPropertyName("emissiveTexture")]
        public TextureInfo? EmissiveTexture { get; set; }

        [JsonPropertyName("emissiveFactor")]
        public float[]? EmissiveFactor { get; set; }

        [JsonPropertyName("alphaMode")]
        public string? AlphaMode { get; set; } = "OPAQUE";

        [JsonPropertyName("alphaCutoff")]
        public float? AlphaCutoff { get; set; }

        [JsonPropertyName("doubleSided")]
        public bool? DoubleSided { get; set; }
    }

    public class PbrMetallicRoughness
    {
        [JsonPropertyName("baseColorFactor")]
        public float[]? BaseColorFactor { get; set; }

        [JsonPropertyName("baseColorTexture")]
        public TextureInfo? BaseColorTexture { get; set; }

        [JsonPropertyName("metallicFactor")]
        public float? MetallicFactor { get; set; } = 1.0f;

        [JsonPropertyName("roughnessFactor")]
        public float? RoughnessFactor { get; set; } = 1.0f;

        [JsonPropertyName("metallicRoughnessTexture")]
        public TextureInfo? MetallicRoughnessTexture { get; set; }
    }

    public class TextureInfo
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("texCoord")]
        public int TexCoord { get; set; } = 0;
    }

    public class Texture
    {
        [JsonPropertyName("sampler")]
        public int? Sampler { get; set; }

        [JsonPropertyName("source")]
        public int Source { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class NormalTextureInfo : TextureInfo
    {
        [JsonPropertyName("scale")]
        public float Scale { get; set; } = 1.0f;
    }

    public class Image
    {
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("bufferView")]
        public int? BufferView { get; set; } // For GLB embedded images
    }

    public class Sampler
    {
        [JsonPropertyName("magFilter")]
        public int? MagFilter { get; set; } = 9729; // LINEAR

        [JsonPropertyName("minFilter")]
        public int? MinFilter { get; set; } = 9987; // LINEAR_MIPMAP_LINEAR

        [JsonPropertyName("wrapS")]
        public int? WrapS { get; set; } = 10497; // REPEAT

        [JsonPropertyName("wrapT")]
        public int? WrapT { get; set; } = 10497; // REPEAT
    }
    
    public enum FileFormat
    {
        Glb,
        Gltf
    }
}