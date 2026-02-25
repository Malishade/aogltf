using AODB.Common.DbClasses;
using System.Numerics;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class AbiffMeshProcessor
    {
        private readonly RDBMesh_t _rdbMesh;

        public AbiffMeshProcessor(RDBMesh_t rdbMesh)
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
                    var meshIndex = CreateMeshData(node.SourceMeshIndex.Value, sceneData, node.HasAnimation);
                    if (meshIndex.HasValue)
                    {
                        node.MeshIndex = meshIndex;
                    }
                }
            }
        }

        private int? CreateMeshData(int sourceMeshIndex, SceneData sceneData, bool hasAnimation)
        {
            if (_rdbMesh.Members[sourceMeshIndex] is not FAFTriMeshData_t triMeshData)
                return null;

            var meshData = new MeshData { SourceMeshIndex = sourceMeshIndex };

            foreach (int meshIdx in triMeshData.mesh)
            {
                if (_rdbMesh.Members[meshIdx] is not SimpleMesh simpleMesh ||
                    _rdbMesh.Members[simpleMesh.trilist] is not TriList triList)
                    continue;

                Vector3[] vertices;
                Vector3[] normals;

                if (!hasAnimation)
                {
                    Matrix4x4 transform = CreateTransformMatrix(triMeshData);
                    vertices = [.. simpleMesh.Vertices.Select(v => transform.MultiplyPoint(v.Position.ToNumerics()))];
                    normals = [.. simpleMesh.Vertices.Select(v => Vector3.TransformNormal(v.Normal.ToNumerics(), transform))];
                }
                else
                {
                    vertices = [.. simpleMesh.Vertices.Select(v => v.Position.ToNumerics())];
                    normals = [.. simpleMesh.Vertices.Select(v => v.Normal.ToNumerics())];
                }

                var uvs = simpleMesh.Vertices
                    .Select(v => new Vector2(v.UVs.X, v.UVs.Y))
                    .ToArray();

                var indices = triList.Triangles
                    .Select(i => checked((ushort)i))
                    .ToArray();

                int? materialIndex = simpleMesh.material >= 0 ? simpleMesh.material : null;

                var primitive = new PrimitiveData(vertices, normals, uvs, indices, materialIndex);
                meshData.Primitives.Add(primitive);
            }

            int newIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);
            return newIndex;
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
                foreach (var prim in mesh.Primitives)
                {
                    if (prim.MaterialIndex.HasValue)
                        materialIndices.Add(prim.MaterialIndex.Value);
                }
            }

            return [.. materialIndices];
        }
    }
}