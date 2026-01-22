using AODB;
using AODB.Common.DbClasses;
using AODB.Common.RDBObjects;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class AbiffMaterialBuilder
    {
        private readonly List<Material> _materials = new();
        private readonly List<Texture> _textures = new();
        private readonly List<Image> _images = new();
        private readonly List<Sampler> _samplers = new();
        private readonly Dictionary<FAFMaterial_t, int> _matMap = new();
        private readonly Dictionary<int, int> _materialIndexMap = new();
        private readonly Dictionary<int, int> _textureIdMap = new();
        private readonly RDBMesh_t _rdbMesh;
        private readonly string _outputPath;
        private readonly bool _isGlb;
        private readonly RdbController _rdbController;

        public AbiffMaterialBuilder(RdbController controller, RDBMesh_t rdbMesh, string outputPath, bool isGlb)
        {
            _rdbController = controller;
            _rdbMesh = rdbMesh;
            _outputPath = outputPath;
            _isGlb = isGlb;

            _samplers.Add(new Sampler());
        }

        public void BuildMaterials(List<FAFMaterial_t> materials)
        {
            foreach (var material in materials)
                BuildMaterial(material);
        }

        public int BuildMaterial(FAFMaterial_t materialClass)
        {
            if (_matMap.TryGetValue(materialClass, out int idx))
                return idx;

            var gltfMaterial = new Material
            {
                Name = materialClass.name,
                PbrMetallicRoughness = new PbrMetallicRoughness()
            };

            var diffuse = materialClass.diff;
            gltfMaterial.PbrMetallicRoughness.BaseColorFactor = new float[]
            {
                diffuse.R, diffuse.G, diffuse.B, materialClass.opac
            };

            var emissive = materialClass.emis;
            gltfMaterial.EmissiveFactor = new float[] { emissive.R, emissive.G, emissive.B };

            gltfMaterial.PbrMetallicRoughness.RoughnessFactor = Math.Max(0.0f, 1.0f - (materialClass.shin / 128.0f));
            gltfMaterial.PbrMetallicRoughness.MetallicFactor = 0.0f;

            if (materialClass.delta_state >= 0)
                ProcessDeltaState(gltfMaterial, materialClass);

            int matIdx = _materials.Count;
            _matMap[materialClass] = matIdx;
            _materials.Add(gltfMaterial);

            return matIdx;
        }

        public int? ResolveMaterialIndex(int? rdbMaterialIndex)
        {
            if (!rdbMaterialIndex.HasValue || rdbMaterialIndex.Value < 0)
                return null;

            if (_materialIndexMap.TryGetValue(rdbMaterialIndex.Value, out int gltfIndex))
                return gltfIndex;

            if (_rdbMesh.Members[rdbMaterialIndex.Value] is FAFMaterial_t mat)
            {
                int newIndex = BuildMaterial(mat);
                _materialIndexMap[rdbMaterialIndex.Value] = newIndex;
                return newIndex;
            }

            return null;
        }

        public Material? GetMaterial(int index)
        {
            return index >= 0 && index < _materials.Count ? _materials[index] : null;
        }

        private void ProcessDeltaState(Material gltfMaterial, FAFMaterial_t mat)
        {
            if (_rdbMesh.Members[mat.delta_state] is not RDeltaState deltaState)
                return;

            for (int i = 0; i < deltaState.rst_count; i++)
            {
                var type = (D3DRenderStateType)deltaState.rst_type[i];
                var value = deltaState.rst_value[i];

                switch (type)
                {
                    case D3DRenderStateType.D3DRS_CULLMODE:
                        gltfMaterial.DoubleSided = true;
                        break;
                    case D3DRenderStateType.D3DRS_ALPHABLENDENABLE:
                        if (value == 1)
                            gltfMaterial.AlphaMode = "BLEND";
                        break;
                    case D3DRenderStateType.D3DRS_SPECULARENABLE:
                        if (value == 0)
                        {
                            gltfMaterial.PbrMetallicRoughness.MetallicFactor = 0.0f;
                            gltfMaterial.PbrMetallicRoughness.RoughnessFactor = 1.0f;
                        }
                        break;
                }
            }

            for (int i = 0; i < deltaState.tch_count; i++)
            {
                if (_rdbMesh.Members[deltaState.tch_text[i]] is FAFTexture_t tex &&
                    _rdbMesh.Members[tex.creator] is AnarchyTexCreator_t creator)
                {
                    int textureIndex = GetOrCreateTexture((int)creator.inst);
                    var type = (TextureChannelType)deltaState.tch_type[i];

                    if (type == TextureChannelType.Diffuse)
                        gltfMaterial.PbrMetallicRoughness.BaseColorTexture = new TextureInfo { Index = textureIndex };
                    else if (type == TextureChannelType.Emissive)
                        gltfMaterial.EmissiveTexture = new TextureInfo { Index = textureIndex };
                }
            }
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
            name = rdb.Get<InfoObject>(1).Types[ResourceTypeId.Texture].TryGetValue(textureId, out string rawName) ? rawName.Trim('\0') : $"UnnamedTex_{textureId}";
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
    }
}
