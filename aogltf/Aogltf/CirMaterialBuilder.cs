using AODB;
using AODB.Common.RDBObjects;

namespace aogltf
{
    public class CirMaterialBuilder
    {
        private readonly List<Material> _materials = new();
        private readonly List<Texture> _textures = new();
        private readonly List<Image> _images = new();
        private readonly List<Sampler> _samplers = new();
        private readonly Dictionary<RDBCatMesh.Material, int> _matMap = new();
        private readonly Dictionary<int, int> _textureIdMap = new();
        private readonly HashSet<int> _textureIds = new();
        private readonly RdbController _rdbController;
        private readonly string _outputPath;
        private readonly bool _isGlb;

        public CirMaterialBuilder(RdbController controller, string outputPath, bool isGlb)
        {
            _rdbController = controller;
            _outputPath = outputPath;
            _isGlb = isGlb;

            _samplers.Add(new Sampler());
        }

        public void BuildMaterials(RDBCatMesh catMesh)
        {
            foreach (var catMaterial in catMesh.Materials)
            {
                var catTexture = catMesh.Textures.FirstOrDefault(x => x.Name == catMaterial.Name);
                BuildMaterial(catMaterial, catTexture);
            }
        }

        public int BuildMaterial(RDBCatMesh.Material catMaterial, RDBCatMesh.Texture catTexture)
        {
            if (_matMap.TryGetValue(catMaterial, out int existingIdx))
                return existingIdx;

            var gltfMaterial = new Material
            {
                Name = catMaterial.Name,
                PbrMetallicRoughness = new PbrMetallicRoughness()
            };

            // Convert material properties to glTF PBR
            var diffuse = catMaterial.Diffuse;
            gltfMaterial.PbrMetallicRoughness.BaseColorFactor = new float[]
            {
                diffuse.R, diffuse.G, diffuse.B, catMaterial.SheenOpacity
            };

            var emissive = catMaterial.Emission;
            gltfMaterial.EmissiveFactor = new float[] { emissive.R, emissive.G, emissive.B };

            // Convert sheen to roughness (inverse relationship)
            gltfMaterial.PbrMetallicRoughness.RoughnessFactor = Math.Max(0.0f, 1.0f - (catMaterial.Sheen / 128.0f));
            gltfMaterial.PbrMetallicRoughness.MetallicFactor = 0.0f;

            // Handle alpha blending
            if (catTexture != null && catMaterial.Unknown2 == 5)
            {
                gltfMaterial.AlphaMode = "BLEND";
            }

            // Process textures
            if (catTexture != null)
            {
                // Diffuse texture
                if (catTexture.Texture1 != 0)
                {
                    int diffuseTextureIndex = GetOrCreateTexture(catTexture.Texture1);
                    gltfMaterial.PbrMetallicRoughness.BaseColorTexture = new TextureInfo { Index = diffuseTextureIndex };
                    _textureIds.Add(catTexture.Texture1);
                }

                // Emissive texture
                if (catTexture.Texture2 != 0)
                {
                    int emissiveTextureIndex = GetOrCreateTexture(catTexture.Texture2);
                    gltfMaterial.EmissiveTexture = new TextureInfo { Index = emissiveTextureIndex };
                    _textureIds.Add(catTexture.Texture2);
                }
            }

            int matIdx = _materials.Count;
            _matMap[catMaterial] = matIdx;
            _materials.Add(gltfMaterial);

            return matIdx;
        }

        public int? ResolveMaterialIndex(int catMaterialIndex, RDBCatMesh catMesh)
        {
            if (catMaterialIndex < 0 || catMaterialIndex >= catMesh.Materials.Count)
                return null;

            var catMaterial = catMesh.Materials[catMaterialIndex];

            if (_matMap.TryGetValue(catMaterial, out int gltfIndex))
                return gltfIndex;

            var catTexture = catMesh.Textures.FirstOrDefault(x => x.Name == catMaterial.Name);
            return BuildMaterial(catMaterial, catTexture);
        }

        public Material? GetMaterial(int index)
        {
            return index >= 0 && index < _materials.Count ? _materials[index] : null;
        }

        private int GetOrCreateTexture(int textureId)
        {
            if (_textureIdMap.TryGetValue(textureId, out int index))
                return index;

            GetTexture(_rdbController, textureId, out string name, out byte[] data);

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

        private static void GetTexture(RdbController rdb, int textureId, out string name, out byte[] data)
        {
            name = rdb.Get<InfoObject>(1).Types[ResourceTypeId.Texture].TryGetValue(textureId, out string rawName)
                ? rawName.Trim('\0')
                : $"UnnamedTex_{textureId}";
            data = rdb.Get<AOTexture>(textureId).JpgData;
        }

        private static string GetMimeType(string fileName)
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

        public void AddToGltf(Gltf gltf)
        {
            if (_materials.Count > 0) gltf.Materials = _materials.ToArray();
            if (_textures.Count > 0) gltf.Textures = _textures.ToArray();
            if (_images.Count > 0) gltf.Images = _images.ToArray();
            if (_samplers.Count > 0) gltf.Samplers = _samplers.ToArray();
        }

        public int MaterialCount => _materials.Count;
        public HashSet<int> TextureIds => _textureIds;
    }
}