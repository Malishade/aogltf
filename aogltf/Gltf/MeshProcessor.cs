using AODB.Common.DbClasses;
using System.Numerics;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class MeshProcessor
    {
        private readonly RDBMesh_t _rdbMesh;

        public MeshProcessor(RDBMesh_t rdbMesh)
        {
            _rdbMesh = rdbMesh;
        }

        public void ProcessMeshData(SceneData sceneData)
        {
            for (int i = 0; i < sceneData.Nodes.Count; i++)
            {
                NodeData node = sceneData.Nodes[i];
                if (node.SourceMeshIndex.HasValue)
                {
                    var meshIndex = CreateMeshData(node.SourceMeshIndex.Value, sceneData);
                    if (meshIndex.HasValue)
                    {
                        node.MeshIndex = meshIndex;
                    }
                }
            }
        }

        private int? CreateMeshData(int sourceMeshIndex, SceneData sceneData)
        {
            if (_rdbMesh.Members[sourceMeshIndex] is not FAFTriMeshData_t triMeshData)
                return null;

            if (triMeshData.mesh.Length == 0)
                return null;

            int meshIdx = triMeshData.mesh[0];

            if (_rdbMesh.Members[meshIdx] is not SimpleMesh simpleMesh ||
                _rdbMesh.Members[simpleMesh.trilist] is not TriList triList)
                return null;

            Matrix4x4 transform = CreateTransformMatrix(triMeshData);
            Vector3[] vertices = simpleMesh.Vertices.Select(v => transform.MultiplyPoint(v.Position.ToNumerics())).ToArray();
            ushort[] indices = triList.Triangles.Select(i => checked((ushort)i)).ToArray();
            Vector3[] normals = simpleMesh.Vertices.Select(v => v.Normal.ToNumerics()).ToArray();
            Vector2[] uvs = simpleMesh.Vertices.Select(v => new Vector2(v.UVs.X, v.UVs.Y)).ToArray();

            int? materialIndex = simpleMesh.material >= 0 ? simpleMesh.material : null;

            var meshData = new MeshData(vertices, indices, normals, uvs, materialIndex)
            {
                SourceMeshIndex = sourceMeshIndex
            };

            int newMeshIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);

            return newMeshIndex;
        }

        private Matrix4x4 CreateTransformMatrix(FAFTriMeshData_t triMeshData)
        {
            Quaternion rotation = triMeshData.anim_rot.ToNumerics();
            Vector3 position = triMeshData.anim_pos.ToNumerics();

            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(rotation);
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(position);

            return rotationMatrix * translationMatrix;
        }

        public List<int> GetUsedMaterialIndices(SceneData sceneData)
        {
            var materialIndices = new HashSet<int>();

            foreach (var mesh in sceneData.Meshes)
            {
                if (mesh.MaterialIndex.HasValue)
                    materialIndices.Add(mesh.MaterialIndex.Value);
            }

            return materialIndices.ToList();
        }
    }
}