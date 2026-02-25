using AODB;
using AODB.Common.DbClasses;
using AODB.Common.RDBObjects;
using gltf;
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

        public bool Export(string outputFolder, int meshId, FileFormat format, out string objectName, ExportMirror transform = ExportMirror.NoMirror)
        {
            objectName = string.Empty;

            return format switch
            {
                FileFormat.Gltf => ExportGltf(outputFolder, meshId, out objectName, transform),
                FileFormat.Glb => ExportGlb(outputFolder, meshId, out objectName, transform),
                _ => false,
            };
        }

        private bool ExportGltf(string outputFolder, int meshId, out string objectName, ExportMirror transform)
        {
            var rdbMesh = _rdbController.Get<RDBMesh>(meshId).RDBMesh_t;
            var sceneBuilder = new AbiffSceneBuilder(rdbMesh);
            var meshProcessor = new AbiffMeshProcessor(rdbMesh);
            objectName = GetInfoObjectName(_rdbController, meshId);

            SceneData sceneData = sceneBuilder.BuildSceneHierarchy();
            meshProcessor.ProcessMeshData(sceneData);

            ApplySceneTransform(sceneData, transform);

            var materialBuilder = new AbiffMaterialBuilder(_rdbController, outputFolder, false);
            List<int> usedMaterialIndices = meshProcessor.GetUsedMaterialIndices(sceneData);
            List<FAFMaterial_t> usedMaterials = usedMaterialIndices
                .Where(idx => rdbMesh.Members[idx] is FAFMaterial_t)
                .Select(idx => (FAFMaterial_t)rdbMesh.Members[idx])
                .ToList();

            materialBuilder.BuildMaterials(rdbMesh, usedMaterials);
            ConvertAndResolveMaterials(sceneData, rdbMesh, materialBuilder);

            Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);
            gltf.BindMaterials(materialBuilder);
            gltf.Buffers[0].Uri = $"{objectName}.bin";

            File.WriteAllBytes(Path.Combine(outputFolder, $"{objectName}.bin"), bufferData);
            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.gltf"), gltf);
            return true;
        }

        private bool ExportGlb(string outputFolder, int meshId, out string objectName, ExportMirror transform)
        {
            objectName = string.Empty;
            RDBMesh_t? rdbMesh;

            try
            {
                rdbMesh = _rdbController.Get<RDBMesh>(meshId)?.RDBMesh_t;
            }
            catch
            {
                return false;
            }

            if (rdbMesh == null)
                return false;

            var sceneBuilder = new AbiffSceneBuilder(rdbMesh);
            var meshProcessor = new AbiffMeshProcessor(rdbMesh);
            objectName = GetInfoObjectName(_rdbController, meshId);

            SceneData sceneData = sceneBuilder.BuildSceneHierarchy();
            meshProcessor.ProcessMeshData(sceneData);

            ApplySceneTransform(sceneData, transform);

            var materialBuilder = new AbiffMaterialBuilder(_rdbController, outputFolder, true);
            var usedMaterialIndices = meshProcessor.GetUsedMaterialIndices(sceneData);
            var usedMaterials = usedMaterialIndices
                .Where(idx => rdbMesh.Members[idx] is FAFMaterial_t)
                .Select(idx => (FAFMaterial_t)rdbMesh.Members[idx])
                .ToList();

            materialBuilder.BuildMaterials(rdbMesh, usedMaterials);
            ConvertAndResolveMaterials(sceneData, rdbMesh, materialBuilder);

            Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);
            gltf.BindMaterials(materialBuilder);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.glb"), gltf, bufferData);
            return true;
        }
 
        private void ConvertAndResolveMaterials(SceneData sceneData, RDBMesh_t rdbMesh, AbiffMaterialBuilder materialBuilder)
        {
            var materialMap = new Dictionary<int, int>();

            foreach (var mesh in sceneData.Meshes)
            {
                foreach (var prim in mesh.Primitives)
                {
                    if (!prim.MaterialIndex.HasValue)
                        continue;

                    int rdbMatIndex = prim.MaterialIndex.Value;

                    if (materialMap.TryGetValue(rdbMatIndex, out int gltfMatIndex))
                    {
                        prim.MaterialIndex = gltfMatIndex;
                        continue;
                    }

                    int? resolvedGltfMatIndex = materialBuilder.ResolveMaterialIndex(rdbMatIndex, rdbMesh);

                    if (resolvedGltfMatIndex.HasValue)
                    {
                        materialMap[rdbMatIndex] = resolvedGltfMatIndex.Value;
                        prim.MaterialIndex = resolvedGltfMatIndex.Value;
                    }
                    else
                    {
                        prim.MaterialIndex = null;
                    }
                }
            }
        }

        private static string GetInfoObjectName(RdbController rdbController, int id)
        {
            return (rdbController.Get<InfoObject>(1).Types[ResourceTypeId.RdbMesh].TryGetValue(id, out string? rdbName)
                ? rdbName.Trim('\0') : $"Unnamed_{id}").Replace(".abiff", "");
        }

        private void ApplySceneTransform(SceneData sceneData, ExportMirror transform)
        {
            SceneTransformHelper.Apply(sceneData, ExportMirror.MirrorX);
            SceneTransformHelper.Apply(sceneData, transform);
        }
    }
}