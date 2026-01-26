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
            _catMeshToAnimIds = CatMeshToAnimIdSerializer.DeserializeCompressed(File.ReadAllBytes("Aogltf\\CatMeshToAnimId.bin"));
        }

        public void ExportGltf(string outputFolder, int meshId)
        {
            var catMesh = _rdbController.Get<RDBCatMesh>(meshId);

            if (!GetAnimData(meshId, out var animData))
                return;

            var sceneBuilder = new CirSceneBuilder(catMesh, animData);
            var meshProcessor = new CirMeshProcessor(catMesh);
            var objectName = GetCatMeshName(_rdbController, meshId);

            SceneData sceneData = sceneBuilder.BuildSceneHierarchy(out var boneNodes);
            meshProcessor.ProcessMeshData(sceneData, boneNodes);

            var materialBuilder = new CirMaterialBuilder(_rdbController, outputFolder, false);
            materialBuilder.BuildMaterials(catMesh);

            //List<int> usedMaterialIndices = meshProcessor.GetUsedMaterialIndices(sceneData);
            ConvertAndResolveMaterials(sceneData, materialBuilder, catMesh);

            var gltf = GltfBuilder.Create(sceneData, out byte[] bufferData);

            materialBuilder.AddToGltf(gltf);
            gltf.Buffers[0].Uri = $"{objectName}.bin";

            var binPath = Path.Combine(outputFolder, $"{objectName}.bin");
            File.WriteAllBytes(binPath, bufferData);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.gltf"), gltf);
        }

        public bool ExportGlb(string outputFolder, int meshId, out string objectName)
        {
            objectName = string.Empty;
            RDBCatMesh? catMesh;

            try
            {
                catMesh = _rdbController.Get<RDBCatMesh>(meshId);
            }
            catch
            {
                return false;
            }

            if (catMesh == null)
                return false;

            if (!GetAnimData(meshId, out var animData))
                return false;

            var sceneBuilder = new CirSceneBuilder(catMesh, animData);
            var meshProcessor = new CirMeshProcessor(catMesh);
              objectName = GetCatMeshName(_rdbController, meshId);

            SceneData sceneData = sceneBuilder.BuildSceneHierarchy(out var boneData);
            meshProcessor.ProcessMeshData(sceneData, boneData);

            var materialBuilder = new CirMaterialBuilder(_rdbController, outputFolder, true);
            materialBuilder.BuildMaterials(catMesh);

            //List<int> usedMaterialIndices = meshProcessor.GetUsedMaterialIndices(sceneData);
            ConvertAndResolveMaterials(sceneData, materialBuilder, catMesh);

            var gltf = GltfBuilder.Create(sceneData, out byte[] bufferData);

            materialBuilder.AddToGltf(gltf);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.glb"), gltf, bufferData);

            return true;
        }

        private void ConvertAndResolveMaterials(SceneData sceneData, CirMaterialBuilder materialBuilder, RDBCatMesh catMesh)
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
            return (rdbController.Get<InfoObject>(1).Types[ResourceTypeId.CatMesh].TryGetValue(id, out string? rdbName)
                ? rdbName.Trim('\0')
                : $"Unnamed_{id}").Replace(".cir", "");
        }

        private static string GetCatAnimName(RdbController rdbController, int id)
        {
            return (rdbController.Get<InfoObject>(1).Types[ResourceTypeId.Anim].TryGetValue(id, out string? rdbName)
                ? rdbName.Trim('\0')
                : $"Unnamed_{id}").Replace(".ani", "");
        }

        private bool GetAnimData(int meshId, out List<AnimData> animData)
        {
            animData = null;

            if (!_catMeshToAnimIds.TryGetValue(meshId, out List<int>? animIds))
            {
                return false;
            }

            if (animIds == null)
            {
                return false;
            }

            animData = new List<AnimData>();

            foreach (var animId in animIds)
            {
                animData.Add(new AnimData
                {
                    Name = GetCatAnimName(_rdbController, animId),
                    CatAnim = _rdbController.Get<CATAnim>(animId)
                });
            }
            animData = animData
                .OrderByDescending(x => x.Name.Contains("stand") && x.Name.Contains("idle"))
                .ThenByDescending(x => x.Name.Contains("stand"))
                .ThenByDescending(x => x.Name.Contains("idle"))
                .ThenByDescending(x => x.Name.Contains("unarmed")).ToList();

            return animData.Count != 0;
        }

    }
}