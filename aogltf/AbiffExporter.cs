using AODB;
using AODB.Common.RDBObjects;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class AbiffExporter
    {
        private readonly RdbController _rdbController;

        public AbiffExporter(RdbController rdbController)
        {
            _rdbController = rdbController;
        }

        public void ExportGltf(string outputFolder, int meshId)
        {
            var rdbMesh = _rdbController.Get<RDBMesh>(meshId).RDBMesh_t;
            var sceneBuilder = new SceneBuilder(rdbMesh);
            var meshProcessor = new MeshProcessor(rdbMesh);
            var objectName = GetInfoObjectName(_rdbController, meshId);

            // Build scene
            SceneData sceneData = sceneBuilder.BuildSceneHierarchy();
            meshProcessor.ProcessMeshData(sceneData);
          
            var materialBuilder = new MaterialBuilder(_rdbController, rdbMesh, outputFolder, false);
         
            List<int> usedMaterialIndices = meshProcessor.GetUsedMaterialIndices(sceneData);
          
            List<FAFMaterial_t> usedMaterials = usedMaterialIndices
                .Where(idx => rdbMesh.Members[idx] is FAFMaterial_t)
                .Select(idx => (FAFMaterial_t)rdbMesh.Members[idx])
                .ToList();
           
            materialBuilder.BuildMaterials(usedMaterials);
            ConvertAndResolveMaterials(sceneData, materialBuilder);

            Gltf gltf = GltfBuilder.Create(sceneData, out byte[] bufferData);

            materialBuilder.AddToGltf(gltf);
            gltf.Buffers[0].Uri = $"{objectName}.bin";
         
            var binPath = Path.Combine(outputFolder, $"{objectName}.bin");
        
            File.WriteAllBytes(binPath, bufferData);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.gltf"), gltf);
        }

        public void ExportGlb(string outputFolder, int meshId)
        {
            var rdbMesh = _rdbController.Get<RDBMesh>(meshId).RDBMesh_t;
            var sceneBuilder = new SceneBuilder(rdbMesh);
            var meshProcessor = new MeshProcessor(rdbMesh);
            var objectName = GetInfoObjectName(_rdbController, meshId);

            SceneData sceneData = sceneBuilder.BuildSceneHierarchy();
            meshProcessor.ProcessMeshData(sceneData);

            var materialBuilder = new MaterialBuilder(_rdbController, rdbMesh, outputFolder, true);
            var usedMaterialIndices = meshProcessor.GetUsedMaterialIndices(sceneData);
            var usedMaterials = usedMaterialIndices
                .Where(idx => rdbMesh.Members[idx] is FAFMaterial_t)
                .Select(idx => (FAFMaterial_t)rdbMesh.Members[idx])
                .ToList();

            materialBuilder.BuildMaterials(usedMaterials);
            ConvertAndResolveMaterials(sceneData, materialBuilder);

            Gltf gltf = GltfBuilder.Create(sceneData, out byte[] bufferData);
            materialBuilder.AddToGltf(gltf);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.glb"), gltf, bufferData);
        }

        private void ConvertAndResolveMaterials(SceneData sceneData, MaterialBuilder materialBuilder)
        {
            var rdbToSceneMaterialMap = new Dictionary<int, int>();

            for (int i = 0; i < sceneData.Meshes.Count; i++)
            {
                var mesh = sceneData.Meshes[i];

                if (!mesh.MaterialIndex.HasValue)
                    continue;

                int rdbMatIndex = mesh.MaterialIndex.Value;

                if (rdbToSceneMaterialMap.TryGetValue(rdbMatIndex, out int sceneMaterialIndex))
                {
                    mesh.MaterialIndex = sceneMaterialIndex;
                    continue;
                }

                int? gltfMatIndex = materialBuilder.ResolveMaterialIndex(rdbMatIndex);

                if (gltfMatIndex.HasValue)
                {
                    var gltfMat = materialBuilder.GetMaterial(gltfMatIndex.Value);
                    var matData = gltfMat != null ? MaterialData.FromGltfMaterial(gltfMat) : new MaterialData();
                    int newSceneMaterialIndex = sceneData.Materials.Count;
                    sceneData.Materials.Add(matData);
                    rdbToSceneMaterialMap[rdbMatIndex] = newSceneMaterialIndex;
                    mesh.MaterialIndex = newSceneMaterialIndex;
                }
                else
                {
                    int fallbackIndex = sceneData.Materials.Count;
                    sceneData.Materials.Add(new MaterialData());
                    mesh.MaterialIndex = fallbackIndex;
                }
            }
        }

        private static string GetInfoObjectName(RdbController rdbController, int id)
        {
            return (rdbController.Get<InfoObject>(1).Types[ResourceTypeId.RdbMesh].TryGetValue(id, out string? rdbName) ? rdbName.Trim('\0') : $"Unnamed_{id}").Replace(".abiff", "");
        }
    }
}
