using AODB;
using gltf;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;

namespace aogltf
{
    public class CollisionExporter : PlayfieldExporterBase<List<PfCollisionMeshData>>
    {
        public CollisionExporter(RdbController rdbController) : base(rdbController)
        {
        }

        protected override List<PfCollisionMeshData> ParseData()
        {
            CollisionParser parser = new CollisionParser(_rdbController.BaseAoPath);
            return parser.Get(PlayfieldId);
        }

        public override bool ExportGlb(string outputFolder, List<PfCollisionMeshData> collisionData)
        {
            try
            {
                if (collisionData == null || collisionData.Count == 0)
                    return false;

                var sceneBuilder = new CollisionSceneBuilder();
                SceneData sceneData = sceneBuilder.BuildCollisionScene(collisionData);

                SceneTransformHelper.Apply(sceneData, ExportTransforms);

                Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);

                GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"Playfield_Collision_{PlayfieldId}.glb"), gltf, bufferData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool ExportGltf(string outputFolder, List<PfCollisionMeshData> collisionData)
        {
            var objectName = $"Playfield_Collision_{PlayfieldId}";

            try
            {
                if (collisionData == null || collisionData.Count == 0)
                    return false;

                var sceneBuilder = new CollisionSceneBuilder();
                SceneData sceneData = sceneBuilder.BuildCollisionScene(collisionData);

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

    public class CollisionSceneBuilder
    {
        public SceneData BuildCollisionScene(List<PfCollisionMeshData> collisionData)
        {
            var sceneData = new SceneData();
            var rootNode = new NodeData { Name = "Collision_Root" };

            int rootIndex = sceneData.Nodes.Count;
            sceneData.Nodes.Add(rootNode);
            sceneData.RootNodeIndex = rootIndex;

            foreach (var collision in collisionData)
            {
                var surfaceNode = new NodeData { Name = $"Surface_{collision.SurfaceId}" };
                int surfaceNodeIndex = sceneData.Nodes.Count;
                sceneData.Nodes.Add(surfaceNode);
                rootNode.ChildIndices.Add(surfaceNodeIndex);

                for (int i = 0; i < collision.Submeshes.Count; i++)
                {
                    var submesh = collision.Submeshes[i];
                    var submeshNode = CreateSubmeshNode(submesh, collision.SurfaceId, i, sceneData);

                    int nodeIndex = sceneData.Nodes.Count;
                    sceneData.Nodes.Add(submeshNode);
                    surfaceNode.ChildIndices.Add(nodeIndex);
                }
            }

            return sceneData;
        }

        private NodeData CreateSubmeshNode(PfCollisionSubmesh submesh, uint surfaceId, int submeshIndex, SceneData sceneData)
        {
            var meshData = new MeshData();
            var verts = submesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
            var normals = Array.Empty<Vector3>();
            var uvs = Array.Empty<Vector2>();
            var indices = submesh.Triangles.Select(t => (ushort)t).ToArray();

            var primitive = new PrimitiveData(verts, normals, uvs, indices, null);
            meshData.Primitives.Add(primitive);

            int meshIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);

            var node = new NodeData
            {
                Name = $"Collision_{surfaceId}_{submeshIndex}",
                MeshIndex = meshIndex
            };

            return node;
        }
    }
}