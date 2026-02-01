using gltf;
#nullable disable

namespace aogltf
{
    public static class AOGltfExtensions
    {
        public static void BindMaterials(this Gltf gltf, MaterialBuilder materialBuilder)
        {
            if (materialBuilder.Materials?.Count > 0) gltf.Materials = [.. materialBuilder.Materials];
            if (materialBuilder.Textures?.Count > 0) gltf.Textures = [.. materialBuilder.Textures];
            if (materialBuilder.Images?.Count > 0) gltf.Images = [.. materialBuilder.Images];
            if (materialBuilder.Samplers?.Count > 0) gltf.Samplers = [.. materialBuilder.Samplers];
        }
    }
}