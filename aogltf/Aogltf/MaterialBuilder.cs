using AODB;
using AODB.Common.RDBObjects;
using AODB.Common.Structs;
#nullable disable

namespace aogltf
{
    public abstract class MaterialBuilder(RdbController rdbController, string outputPath, bool isGlb)
    {
        private readonly RdbController _rdbController = rdbController;
        private readonly string _outputPath = outputPath;
        private readonly bool _isGlb = isGlb;
        private readonly Dictionary<int, int> _textureIdMap = new();
        private readonly List<Image> _images = new();
        private readonly List<Texture> _textures = new();
        protected readonly List<Material> _materials = new();
        private readonly List<Sampler> _samplers = new List<Sampler> { new Sampler() };
        protected readonly Dictionary<int, int> _materialIndexMap = new();
        protected int MaterialCount => _materials.Count;

        protected static string GetMimeType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".bmp" => "image/bmp",
                ".tga" => "image/x-tga",
                _ => "application/octet-stream"
            };
        }

        protected void GetTexture(int textureId, out string name, out byte[] data)
        {
            name = _rdbController.Get<InfoObject>(1).Types[ResourceTypeId.Texture].TryGetValue(textureId, out string rawName) ? rawName.Trim('\0') : $"UnnamedTex_{textureId}";
            data = _rdbController.Get<AOTexture>(textureId).JpgData;
        }

        protected int GetOrCreateTexture(int textureId)
        {
            if (_textureIdMap.TryGetValue(textureId, out int index))
                return index;
            GetTexture(textureId, out string name, out byte[] data);
            var image = new Image { Name = name };
            if (_isGlb)
            {
                string mimeType = GetMimeType(name);
                string base64Data = Convert.ToBase64String(data);
                image.Uri = $"data:{mimeType};base64,{base64Data}";
            }
            else
            {
                string path = Path.Combine(_outputPath, name);
                File.WriteAllBytes(path, data);
                image.Uri = Path.GetFileName(path);
                image.MimeType = GetMimeType(name);
            }
            int imageIndex = _images.Count;
            _images.Add(image);
            var texture = new Texture
            {
                Name = $"texture_{textureId}",
                Source = imageIndex,
                Sampler = 0
            };
            int texIndex = _textures.Count;
            _textures.Add(texture);
            _textureIdMap[textureId] = texIndex;
            return texIndex;
        }

        public Material GetMaterial(int index)
        {
            return index >= 0 && index < _materials.Count ? _materials[index] : null;
        }

        protected Material CreateBasicMaterial(string name)
        {
            return new Material
            {
                Name = name,
                PbrMetallicRoughness = new PbrMetallicRoughness()
            };
        }

        protected void SetBasicMaterialProperties(
            Material material,
            Color diffuse,
            float opacity,
            Color emissive,
            float shininess)
        {
            material.EmissiveFactor = [emissive.R, emissive.G, emissive.B];

            material.PbrMetallicRoughness.BaseColorFactor = [diffuse.R, diffuse.G, diffuse.B, opacity];
            material.PbrMetallicRoughness.RoughnessFactor = Math.Max(0.0f, 1.0f - (shininess / 128.0f));
            material.PbrMetallicRoughness.MetallicFactor = 0.0f;
        }

        protected void SetBaseColorTexture(Material material, int textureId)
        {
            int textureIndex = GetOrCreateTexture(textureId);
            material.PbrMetallicRoughness.BaseColorTexture = new TextureInfo { Index = textureIndex };
        }

        protected void SetEmissiveTexture(Material material, int textureId)
        {
            int textureIndex = GetOrCreateTexture(textureId);
            material.EmissiveTexture = new TextureInfo { Index = textureIndex };
        }

        protected void EnableAlphaBlend(Material material)
        {
            material.AlphaMode = "BLEND";
        }

        protected void EnableDoubleSided(Material material)
        {
            material.DoubleSided = true;
        }

        protected void DisableSpecular(Material material)
        {
            material.PbrMetallicRoughness.MetallicFactor = 0.0f;
            material.PbrMetallicRoughness.RoughnessFactor = 1.0f;
        }

        protected int AddMaterialToList(Material material)
        {
            int index = _materials.Count;
            _materials.Add(material);
            return index;
        }
        public void AddToGltf(Gltf gltf)
        {
            if (_materials.Count > 0) gltf.Materials = [.. _materials];
            if (_textures.Count > 0) gltf.Textures = [.. _textures];
            if (_images.Count > 0) gltf.Images = [.. _images];
            if (_samplers.Count > 0) gltf.Samplers = [.. _samplers];
        }
    }
}