using System.Numerics;

namespace aogltf
{
    public class SceneData
    {
        public List<NodeData> Nodes { get; set; } = new List<NodeData>();
        public List<MeshData> Meshes { get; set; } = new List<MeshData>();
        public List<MaterialData> Materials { get; set; } = new List<MaterialData>();
        public List<AnimationData> Animations { get; set; } = new List<AnimationData>();
        public int RootNodeIndex { get; set; } = 0;
    }

    public class NodeData
    {
        public string? Name { get; set; }
        public int? MeshIndex { get; set; }
        public int? SourceMeshIndex { get; set; }
        public List<int> ChildIndices { get; set; } = new List<int>();
        public Vector3? Translation { get; set; }
        public Quaternion? Rotation { get; set; }
        public Vector3? Scale { get; set; }
        public bool HasAnimation { get; set; }
    }

    public class MeshData
    {
        public int SourceMeshIndex { get; set; }
        public List<PrimitiveData> Primitives { get; set; } = new List<PrimitiveData>();
    }

    public class PrimitiveData
    {
        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public Vector2[] UVs { get; set; }
        public ushort[] Indices { get; set; }
        public Bounds Bounds { get; set; }
        public int? MaterialIndex { get; set; }

        public PrimitiveData(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, ushort[] indices, int? materialIndex)
        {
            Vertices = vertices;
            Normals = normals;
            UVs = uvs;
            Indices = indices;
            Bounds = Bounds.FromVertices(vertices);
            MaterialIndex = materialIndex;
        }
    }

    public class MaterialData
    {
        public string? Name { get; set; } = "Default Material";
        public Vector4? BaseColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);
        public float MetallicFactor { get; set; } = 0.0f;
        public float RoughnessFactor { get; set; } = 1.0f;
        public Vector3? EmissiveFactor { get; set; } = Vector3.Zero;
        public int? BaseColorTextureIndex { get; set; }
        public int? NormalTextureIndex { get; set; }
        public string? AlphaMode { get; set; } = "OPAQUE";
        public float? AlphaCutoff { get; set; }
        public bool DoubleSided { get; set; } = false;

        public static MaterialData FromGltfMaterial(Material gltfMaterial)
        {
            return new MaterialData
            {
                Name = gltfMaterial.Name,
                BaseColor = gltfMaterial.PbrMetallicRoughness?.BaseColorFactor != null ? new Vector4(gltfMaterial.PbrMetallicRoughness.BaseColorFactor) : null,
                MetallicFactor = gltfMaterial.PbrMetallicRoughness?.MetallicFactor ?? 0.0f,
                RoughnessFactor = gltfMaterial.PbrMetallicRoughness?.RoughnessFactor ?? 1.0f,
                EmissiveFactor = gltfMaterial.EmissiveFactor != null ? new Vector3(gltfMaterial.EmissiveFactor) : null,
                BaseColorTextureIndex = gltfMaterial.PbrMetallicRoughness?.BaseColorTexture?.Index,
                NormalTextureIndex = gltfMaterial.NormalTexture?.Index,
                AlphaMode = gltfMaterial.AlphaMode ?? "OPAQUE",
                AlphaCutoff = gltfMaterial.AlphaCutoff,
                DoubleSided = gltfMaterial.DoubleSided ?? false
            };
        }
    }

    public class AnimationData
    {
        public string? Name { get; set; }
        public List<AnimationChannelData> Channels { get; set; } = new List<AnimationChannelData>();
        public float Duration { get; set; }
    }

    public class AnimationChannelData
    {
        public int NodeIndex { get; set; }
        public string Path { get; set; } // "translation", "rotation", "scale"
        public List<KeyframeData> Keyframes { get; set; } = new List<KeyframeData>();
        public string Interpolation { get; set; } = "LINEAR";
    }

    public class KeyframeData
    {
        public float Time { get; set; }
        public float[] Value { get; set; }

        public KeyframeData(float time, float[] value)
        {
            Time = time;
            Value = value;
        }
    }
}