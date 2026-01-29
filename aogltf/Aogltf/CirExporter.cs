using AODB;
using AODB.Common.RDBObjects;

namespace aogltf
{
    public class AnimData
    {
        public string Name;
        public CATAnim CatAnim;
    }

    public class CirExporter
    {
        private readonly RdbController _rdbController;
        private static Dictionary<int, List<int>>? _catMeshToAnimIds;

        public CirExporter(RdbController rdbController)
        {
            _rdbController = rdbController;
            var path = Path.Combine(AppContext.BaseDirectory, "Aogltf", "CatMeshToAnimId.bin");
            _catMeshToAnimIds = CatMeshToAnimIdSerializer.DeserializeCompressed(File.ReadAllBytes(path));
        }

        public bool Export(string outputFolder, int meshId, FileFormat format, out string objectName)
        {
            objectName = string.Empty;
            bool isGlb = format == FileFormat.Glb;

            try
            {
                var catMesh = _rdbController.Get<RDBCatMesh>(meshId);
                if (catMesh == null || !GetAnimData(meshId, out var animData))
                    return false;

                objectName = GetCatMeshName(_rdbController, meshId);

                var sceneBuilder = new CirSceneBuilder(catMesh, animData);
                var meshProcessor = new CirMeshProcessor(catMesh);

                SceneData sceneData = sceneBuilder.BuildSceneHierarchy(out var boneNodes);
                meshProcessor.ProcessMeshData(sceneData, boneNodes);

                var materialBuilder = new CirMaterialBuilder(_rdbController, outputFolder, isGlb);
                materialBuilder.BuildMaterials(catMesh);
                ConvertAndResolveMaterials(sceneData, catMesh, materialBuilder);

                var gltf = GltfBuilder.Create(sceneData, out byte[] bufferData);
                materialBuilder.AddToGltf(gltf);

                if (isGlb)
                {
                    GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.glb"), gltf, bufferData);
                }
                else
                {
                    gltf.Buffers[0].Uri = $"{objectName}.bin";
                    File.WriteAllBytes(Path.Combine(outputFolder, $"{objectName}.bin"), bufferData);
                    GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.gltf"), gltf);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ConvertAndResolveMaterials(SceneData sceneData, RDBCatMesh catMesh, CirMaterialBuilder materialBuilder)
        {
            var catToGltfMaterialMap = new Dictionary<int, int>();

            foreach (var mesh in sceneData.Meshes)
            {
                foreach (var prim in mesh.Primitives)
                {
                    if (!prim.MaterialIndex.HasValue)
                        continue;

                    int catMatIndex = prim.MaterialIndex.Value;

                    if (catMatIndex < 0 || catMatIndex >= catMesh.Materials.Count)
                    {
                        prim.MaterialIndex = null;
                        continue;
                    }

                    if (catToGltfMaterialMap.TryGetValue(catMatIndex, out int gltfMatIndex))
                    {
                        prim.MaterialIndex = gltfMatIndex;
                        continue;
                    }

                    int? resolvedGltfMatIndex = materialBuilder.ResolveMaterialIndex(catMatIndex, catMesh);
                    if (resolvedGltfMatIndex.HasValue)
                    {
                        catToGltfMaterialMap[catMatIndex] = resolvedGltfMatIndex.Value;
                        prim.MaterialIndex = resolvedGltfMatIndex.Value;
                    }
                    else
                    {
                        prim.MaterialIndex = null;
                    }
                }
            }
        }

        private static string GetCatMeshName(RdbController rdbController, int id)
        {
            return (rdbController.Get<InfoObject>(1).Types[ResourceTypeId.CatMesh].TryGetValue(id, out string? rdbName)               ? rdbName.Trim('\0')   : $"Unnamed_{id}").Replace(".cir", "");
        }

        private static string GetCatAnimName(RdbController rdbController, int id)
        {
            return (rdbController.Get<InfoObject>(1).Types[ResourceTypeId.Anim].TryGetValue(id, out string? rdbName) ? rdbName.Trim('\0') : $"Unnamed_{id}").Replace(".ani", "");
        }

        private bool GetAnimData(int meshId, out List<AnimData> animData)
        {
            animData = null;

            if (!_catMeshToAnimIds.TryGetValue(meshId, out List<int>? animIds) || animIds == null)
                return false;

            animData = animIds
                .Select(animId => new AnimData
                {
                    Name = GetCatAnimName(_rdbController, animId),
                    CatAnim = _rdbController.Get<CATAnim>(animId)
                })
                .OrderByDescending(x => x.Name.Contains("stand") && x.Name.Contains("idle"))
                .ThenByDescending(x => x.Name.Contains("stand"))
                .ThenByDescending(x => x.Name.Contains("idle"))
                .ThenByDescending(x => x.Name.Contains("unarmed"))
                .ToList();

            return animData.Count != 0;
        }
    }
}