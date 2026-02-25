using AODB.Common.RDBObjects;
using System.Numerics;
namespace aogltf
{
    public class CirMeshProcessor
    {
        private readonly RDBCatMesh _catMesh;
        private NodeData[] _boneNodes;
        private Dictionary<int, int> _jointToNodeMap;
        public CirMeshProcessor(RDBCatMesh catMesh)
        {
            _catMesh = catMesh;
        }
        public void ProcessMeshData(SceneData sceneData, NodeData[] boneNodes)
        {
            _boneNodes = boneNodes;
            _jointToNodeMap = new Dictionary<int, int>();
          
            if (_catMesh.Joints != null)
            {
                for (int i = 0; i < _catMesh.Joints.Count && i < boneNodes.Length; i++)
                {
                    _jointToNodeMap[i] = Array.IndexOf([.. sceneData.Nodes], boneNodes[i]);
                }
            }
          
            var nodesToProcess = sceneData.Nodes.Where(n => n.SourceMeshIndex.HasValue).ToList();
           
            foreach (var parentNode in nodesToProcess)
            {
                if (parentNode.SourceMeshIndex.Value >= _catMesh.MeshGroups.Count)
                    continue;
               
                var meshGroup = _catMesh.MeshGroups[parentNode.SourceMeshIndex.Value];
                parentNode.SourceMeshIndex = null;
                parentNode.MeshIndex = null;
              
                for (int meshIdx = 0; meshIdx < meshGroup.Meshes.Count; meshIdx++)
                {
                    var cMesh = meshGroup.Meshes[meshIdx];
                    var meshData = CreateSingleMeshData(cMesh, meshGroup.Name, meshIdx);
                   
                    if (meshData != null && meshData.Primitives.Count > 0)
                    {
                        int meshIndex = sceneData.Meshes.Count;
                        sceneData.Meshes.Add(meshData);
                        var meshNode = new NodeData
                        {
                            Name = $"{meshGroup.Name}{meshIdx}",
                            MeshIndex = meshIndex
                        };
                       
                        int nodeIndex = sceneData.Nodes.Count;
                        sceneData.Nodes.Add(meshNode);
                        int parentIndex = sceneData.Nodes.IndexOf(parentNode);
                        
                        if (!parentNode.ChildIndices.Contains(nodeIndex))
                        {
                            parentNode.ChildIndices.Add(nodeIndex);
                        }
                    }
                }
            }

            if (_catMesh.Joints != null && _catMesh.Joints.Count > 0)
            {
                CreateSkinData(sceneData);
            }
        }
        private void CreateSkinData(SceneData sceneData)
        {
            var skinData = new SkinData
            {
                Joints = new int[_jointToNodeMap.Count],
                InverseBindMatrices = new Matrix4x4[_jointToNodeMap.Count]
            };
           
            int jointIndex = 0;
          
            foreach (var kvp in _jointToNodeMap.OrderBy(x => x.Key))
            {
                skinData.Joints[jointIndex] = kvp.Value;
                var boneNode = _boneNodes[kvp.Key];
                var bindPoseMatrix = GetGlobalBoneTransform(boneNode);
               
                if (!Matrix4x4.Invert(bindPoseMatrix, out skinData.InverseBindMatrices[jointIndex]))
                {
                    skinData.InverseBindMatrices[jointIndex] = Matrix4x4.Identity;
                    Console.WriteLine($"Warning: Failed to invert bind pose matrix for joint {kvp.Key}");
                }
               
                jointIndex++;
            }
           
            if (_jointToNodeMap.Count > 0)
            {
                skinData.SkeletonRootNodeIndex = _jointToNodeMap.Values.Min();
            }
         
            sceneData.Skins.Add(skinData);
        }
        private static Matrix4x4 GetBoneTransform(NodeData node)
        {
            var transform = Matrix4x4.Identity;
          
            if (node.Scale.HasValue)
                transform = Matrix4x4.CreateScale(node.Scale.Value);
           
            if (node.Rotation.HasValue)
                transform *= Matrix4x4.CreateFromQuaternion(node.Rotation.Value);
           
            if (node.Translation.HasValue)
                transform *= Matrix4x4.CreateTranslation(node.Translation.Value);
           
            return transform;
        }

        private bool HasParent(NodeData node)
        {
            var nodeIndex = Array.IndexOf(_boneNodes, node);
           
            for (int i = 0; i < _catMesh.Joints.Count; i++)
            {
                if (_catMesh.Joints[i].ChildJoints.Contains(nodeIndex))
                    return true;
            }

            return false;
        }

        private NodeData GetParentNode(NodeData node)
        {
            var nodeIndex = Array.IndexOf(_boneNodes, node);
            
            for (int i = 0; i < _catMesh.Joints.Count; i++)
            {
                if (_catMesh.Joints[i].ChildJoints.Contains(nodeIndex))
                    return _boneNodes[i];
            }

            return null;
        }

        private Matrix4x4 GetGlobalBoneTransform(NodeData node)
        {
            var transform = GetBoneTransform(node);
            var currentNode = node;
            while (HasParent(currentNode))
            {
                var parentNode = GetParentNode(currentNode);
                var parentTransform = GetBoneTransform(parentNode);
                transform *= parentTransform;
                currentNode = parentNode;
            }
            return transform;
        }

        private MeshData CreateSingleMeshData(RDBCatMesh.Mesh cMesh, string baseName, int meshIndex)
        {
            var meshData = new MeshData
            {
                SourceMeshIndex = meshIndex
            };
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var joints = new List<Vector4>();
            var weights = new List<Vector4>();

            foreach (var vertex in cMesh.Vertices)
            {
                GetVertexBindPose(vertex, out Vector3 vertexPosition, out Vector3 vertexNormal);
                vertices.Add(vertexPosition);
                normals.Add(vertexNormal);
                uvs.Add(new Vector2(vertex.Uvs.X, vertex.Uvs.Y));

                if (_catMesh.Joints?.Count > 0)
                {
                    joints.Add(new Vector4(GetMappedJointIndex(vertex.Joint1), GetMappedJointIndex(vertex.Joint2), 0, 0));
                    weights.Add(vertex.Joint1 == vertex.Joint2 ? new Vector4(1, 0, 0, 0) : new Vector4(vertex.Joint1Weight, 1.0f - vertex.Joint1Weight, 0, 0));
                }
            }

            var indices = cMesh.Triangles.Select(i => checked((ushort)i)).ToArray();
            PrimitiveData primitive;
            
            if (_catMesh.Joints?.Count > 0)
            {
                primitive = new SkeletalPrimitiveData(
                    [.. vertices],
                    [.. normals],
                    [.. uvs],
                    indices,
                    cMesh.MaterialId,
                    [.. joints],
                    [.. weights]
                );
            }
            else
            {
                primitive = new PrimitiveData(
                    [.. vertices],
                    [.. normals],
                    [.. uvs],
                    indices,
                    cMesh.MaterialId
                );
            }
           
            meshData.Primitives.Add(primitive);
            
            return meshData;
        }

        private int GetMappedJointIndex(int localJointIndex)
        {
            if (_jointToNodeMap.ContainsKey(localJointIndex))
            {
                var orderedJoints = _jointToNodeMap.Keys.OrderBy(k => k).ToList();
                return orderedJoints.IndexOf(localJointIndex);
            }
           
            return 0;
        }

        private void GetVertexBindPose(RDBCatMesh.Vertex vertex, out Vector3 position, out Vector3 normal)
        {
            position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
            normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);

            if (_catMesh.Joints == null || _catMesh.Joints.Count == 0)
                return;

            var relToPos1 = new Vector3(vertex.RelToJoint1.X, vertex.RelToJoint1.Y, vertex.RelToJoint1.Z);
            var relToPos2 = new Vector3(vertex.RelToJoint2.X, vertex.RelToJoint2.Y, vertex.RelToJoint2.Z);

            var srcNormal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);

            var bone1GlobalTransform = GetGlobalBoneTransform(_boneNodes[vertex.Joint1]);
            var bone2GlobalTransform = GetGlobalBoneTransform(_boneNodes[vertex.Joint2]);

            var worldPos1 = Vector3.Transform(relToPos1, bone1GlobalTransform);
            var worldPos2 = Vector3.Transform(relToPos2, bone2GlobalTransform);

            position = Vector3.Lerp(worldPos2, worldPos1, vertex.Joint1Weight);

            var normal1 = Vector3.TransformNormal(srcNormal, bone1GlobalTransform);
            var normal2 = Vector3.TransformNormal(srcNormal, bone2GlobalTransform);

            normal = Vector3.Normalize(Vector3.Lerp(normal2, normal1, vertex.Joint1Weight));
        }
    }
}