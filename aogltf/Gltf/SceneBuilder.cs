using AODB.Common.DbClasses;
using System.Numerics;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class SceneBuilder
    {
        private readonly RDBMesh_t _rdbMesh;
        private readonly Dictionary<string, int> _nameCache = new();

        public SceneBuilder(RDBMesh_t rdbMesh)
        {
            _rdbMesh = rdbMesh;
        }

        private string GetUniqueNodeName(string name)
        {
            if (!_nameCache.ContainsKey(name))
            {
                _nameCache[name] = 1;
                return name;
            }
            return $"{name}.{_nameCache[name]++:D3}";
        }

        public SceneData BuildSceneHierarchy()
        {
            var sceneData = new SceneData();
            var nodeMap = new Dictionary<int, int>();

            // First pass: Create all nodes
            for (int i = 0; i < _rdbMesh.Members.Count; i++)
            {
                NodeData? sceneNode = null;

                switch (_rdbMesh.Members[i])
                {
                    case RTriMesh_t triMesh:
                        sceneNode = CreateTriMeshNode(triMesh);
                        break;

                    case RRefFrame_t refFrame:
                        sceneNode = CreateRefFrameNode(refFrame);
                        break;
                }

                if (sceneNode != null)
                {
                    if (_rdbMesh.Members[i] is Transform t && t.conn != -1)
                        sceneNode.Name = ((RRefFrameConnector)_rdbMesh.Members[t.conn]).name;

                    int newNodeIndex = sceneData.Nodes.Count;
                    sceneData.Nodes.Add(sceneNode);
                    nodeMap[i] = newNodeIndex;
                }
            }

            BuildHierarchy(sceneData, nodeMap);

            sceneData.RootNodeIndex = FindRootNode(sceneData, nodeMap);

            return sceneData;
        }

        private NodeData CreateRefFrameNode(RRefFrame_t refFrame)
        {
            var node = new NodeData
            {
                Name = GetUniqueNodeName("RRefFrame")
            };

            if (refFrame.anim_matrix.values != null)
            {
                var matrix = refFrame.anim_matrix.ToNumerics() *
                           ((Transform)_rdbMesh.Members[0]).anim_matrix.ToNumerics();

                Matrix4x4.Decompose(matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

                node.Translation = translation != Vector3.Zero ? translation : null;
                node.Rotation = rotation != Quaternion.Identity ? rotation : null;
                node.Scale = scale != Vector3.One ? scale : null;
            }

            return node;
        }

        private NodeData CreateTriMeshNode(RTriMesh_t triMesh)
        {
            var node = new NodeData
            {
                Translation = triMesh.local_pos.ToNumerics() != Vector3.Zero ? triMesh.local_pos.ToNumerics() : null,
                Rotation = triMesh.local_rot.ToNumerics() != Quaternion.Identity ? triMesh.local_rot.ToNumerics() : null,
                Scale = triMesh.scale != 1f ? new Vector3(triMesh.scale, triMesh.scale, triMesh.scale) : null
            };

            if (_rdbMesh.Members[triMesh.data] is FAFTriMeshData_t triMeshData)
            {
                node.Name = GetUniqueNodeName(triMeshData.name);
                node.SourceMeshIndex = triMesh.data;
            }
            else
            {
                node.Name = GetUniqueNodeName("EmptyMesh");
            }

            return node;
        }

        private void BuildHierarchy(SceneData sceneData, Dictionary<int, int> nodeMap)
        {
            var transforms = _rdbMesh.GetMembers<Transform>();

            for (int i = 0; i < transforms.Count; i++)
            {
                if (!nodeMap.TryGetValue(i, out int parentIndex))
                    continue;

                var parentNode = sceneData.Nodes[parentIndex];

                if (transforms[i].chld == null)
                    continue;

                foreach (int childId in transforms[i].chld)
                {
                    if (nodeMap.TryGetValue(childId, out int childIndex))
                    {
                        parentNode.ChildIndices.Add(childIndex);
                    }
                }
            }
        }

        private int FindRootNode(SceneData sceneData, Dictionary<int, int> nodeMap)
        {
            var allChildren = new HashSet<int>();
            foreach (var node in sceneData.Nodes)
            {
                foreach (var child in node.ChildIndices)
                    allChildren.Add(child);
            }

            for (int i = 0; i < sceneData.Nodes.Count; i++)
            {
                if (!allChildren.Contains(i))
                    return i;
            }

            return 0;
        }
    }
}
