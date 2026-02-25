using AODB;
using gltf;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;

namespace aogltf
{
    public class WaterExporter : PlayfieldExporterBase<List<PfWaterMeshData>>
    {
        public WaterExporter(RdbController rdbController) : base(rdbController)
        {
        }

        protected override List<PfWaterMeshData> ParseData()
        {
            WaterParser parser = new WaterParser(_rdbController);
            return parser.Get(PlayfieldId);
        }

        public override bool ExportGlb(string outputFolder, List<PfWaterMeshData> waterData)
        {
            try
            {
                if (waterData == null || waterData.Count == 0)
                    return false;

                var sceneBuilder = new WaterSceneBuilder();
                SceneData sceneData = sceneBuilder.BuildWaterScene(waterData);

                SceneTransformHelper.Apply(sceneData, ExportTransforms);

                Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);

                GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"Playfield_Water_{PlayfieldId}.glb"), gltf, bufferData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool ExportGltf(string outputFolder, List<PfWaterMeshData> waterData)
        {
            var objectName = $"Playfield_Water_{PlayfieldId}";

            try
            {
                if (waterData == null || waterData.Count == 0)
                    return false;

                var sceneBuilder = new WaterSceneBuilder();
                SceneData sceneData = sceneBuilder.BuildWaterScene(waterData);

                SceneTransformHelper.Apply(sceneData, ExportTransforms);

                Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);
                gltf.Buffers[0].Uri = $"{objectName}.bin";

                var binPath = Path.Combine(outputFolder, $"{objectName}.bin");
                File.WriteAllBytes(binPath, bufferData);

                GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.gltf"), gltf);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class WaterSceneBuilder
    {
        public SceneData BuildWaterScene(List<PfWaterMeshData> waterData)
        {
            var sceneData = new SceneData();
            var rootNode = new NodeData { Name = "Water_Root" };

            int rootIndex = sceneData.Nodes.Count;
            sceneData.Nodes.Add(rootNode);
            sceneData.RootNodeIndex = rootIndex;

            for (int i = 0; i < waterData.Count; i++)
            {
                var water = waterData[i];
                var waterNode = CreateWaterNode(water, i, sceneData);

                int nodeIndex = sceneData.Nodes.Count;
                sceneData.Nodes.Add(waterNode);
                rootNode.ChildIndices.Add(nodeIndex);
            }

            return sceneData;
        }

        private NodeData CreateWaterNode(PfWaterMeshData water, int waterIndex, SceneData sceneData)
        {
            var meshData = new MeshData();
            var verts = water.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
            var normals = Array.Empty<Vector3>();
            var uvs = Array.Empty<Vector2>();
            var indices = water.Triangles.Select(t => (ushort)t).ToArray();

            var primitive = new PrimitiveData(verts, normals, uvs, indices, null);
            meshData.Primitives.Add(primitive);

            int meshIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);

            var node = new NodeData
            {
                Name = $"Water_{waterIndex}",
                MeshIndex = meshIndex
            };

            return node;
        }
    }
}