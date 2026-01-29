using AODB;
using AODB.Common.RDBObjects;

namespace aogltf
{
    public class CirMaterialBuilder(RdbController rdbController, string outputPath, bool isGlb) : MaterialBuilder(rdbController, outputPath, isGlb)
    {
        private readonly Dictionary<RDBCatMesh.Material, int> _matMap = new();
        private readonly HashSet<int> _textureIds = new();
        public HashSet<int> TextureIds => _textureIds;

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

            var gltfMaterial = CreateBasicMaterial(catMaterial.Name);

            SetBasicMaterialProperties(
                gltfMaterial,
                catMaterial.Diffuse,
                catMaterial.SheenOpacity,
                catMaterial.Emission,
                catMaterial.Sheen
            );

            if (catTexture != null && catMaterial.Unknown2 == 5)
            {
                EnableAlphaBlend(gltfMaterial);
            }

            if (catTexture != null)
            {
                if (catTexture.Texture1 != 0)
                {
                    SetBaseColorTexture(gltfMaterial, catTexture.Texture1);
                    _textureIds.Add(catTexture.Texture1);
                }

                if (catTexture.Texture2 != 0)
                {
                    SetEmissiveTexture(gltfMaterial, catTexture.Texture2);
                    _textureIds.Add(catTexture.Texture2);
                }
            }

            int matIdx = AddMaterialToList(gltfMaterial);
            _matMap[catMaterial] = matIdx;

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
    }
}