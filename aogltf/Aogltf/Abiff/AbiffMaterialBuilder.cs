using AODB;
using AODB.Common.DbClasses;
using gltf;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class AbiffMaterialBuilder(RdbController controller, string outputPath, bool isGlb) : MaterialBuilder(controller, outputPath, isGlb)
    {
        private readonly Dictionary<FAFMaterial_t, int> _matMap = new();

        public void BuildMaterials(RDBMesh_t rdbMesh, List<FAFMaterial_t> materials)
        {
            foreach (var material in materials)
                BuildMaterial(material, rdbMesh);
        }

        public int BuildMaterial(FAFMaterial_t materialClass, RDBMesh_t rdbMesh)
        {
            if (_matMap.TryGetValue(materialClass, out int idx))
                return idx;

            var gltfMaterial = CreateBasicMaterial(materialClass.name);

            SetBasicMaterialProperties(
                gltfMaterial,
                materialClass.diff,
                materialClass.opac,
                materialClass.emis,
                materialClass.shin
            );

            if (materialClass.delta_state >= 0)
                ProcessDeltaState(gltfMaterial, rdbMesh, materialClass);

            int matIdx = AddMaterialToList(gltfMaterial);
            _matMap[materialClass] = matIdx;

            return matIdx;
        }

        public int? ResolveMaterialIndex(int? rdbMaterialIndex, RDBMesh_t rdbMesh)
        {
            if (!rdbMaterialIndex.HasValue || rdbMaterialIndex.Value < 0)
                return null;

            if (MaterialIndexMap.TryGetValue(rdbMaterialIndex.Value, out int gltfIndex))
                return gltfIndex;

            if (rdbMesh.Members[rdbMaterialIndex.Value] is FAFMaterial_t mat)
            {
                int newIndex = BuildMaterial(mat, rdbMesh);
                MaterialIndexMap[rdbMaterialIndex.Value] = newIndex;
                return newIndex;
            }

            return null;
        }

        private void ProcessDeltaState(Material gltfMaterial, RDBMesh_t rdbMesh, FAFMaterial_t mat)
        {
            if (rdbMesh.Members[mat.delta_state] is not RDeltaState deltaState)
                return;

            for (int i = 0; i < deltaState.rst_count; i++)
            {
                var type = (D3DRenderStateType)deltaState.rst_type[i];
                var value = deltaState.rst_value[i];

                switch (type)
                {
                    case D3DRenderStateType.D3DRS_CULLMODE:
                        EnableDoubleSided(gltfMaterial);
                        break;
                    case D3DRenderStateType.D3DRS_ALPHABLENDENABLE:
                        if (value == 1)
                            EnableAlphaBlend(gltfMaterial);
                        break;
                    case D3DRenderStateType.D3DRS_SPECULARENABLE:
                        if (value == 0)
                            DisableSpecular(gltfMaterial);
                        break;
                }
            }

            for (int i = 0; i < deltaState.tch_count; i++)
            {
                if (rdbMesh.Members[deltaState.tch_text[i]] is FAFTexture_t tex &&
                    rdbMesh.Members[tex.creator] is AnarchyTexCreator_t creator)
                {
                    var type = (TextureChannelType)deltaState.tch_type[i];

                    if (type == TextureChannelType.Diffuse)
                        SetBaseColorTexture(gltfMaterial, (int)creator.inst);
                    else if (type == TextureChannelType.Emissive)
                        SetEmissiveTexture(gltfMaterial, (int)creator.inst);
                }
            }
        }
    }
}