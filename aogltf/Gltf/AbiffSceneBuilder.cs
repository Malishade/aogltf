using AODB.Common.DbClasses;
using System.Numerics;
using static AODB.Common.DbClasses.RDBMesh_t;

namespace aogltf
{
    public class AbiffSceneBuilder
    {
        private readonly RDBMesh_t _rdbMesh;
        private readonly Dictionary<string, int> _nameCache = new();

        public AbiffSceneBuilder(RDBMesh_t rdbMesh)
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
            BuildAnimations(sceneData, nodeMap);

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
                var matrix = refFrame.anim_matrix.ToNumerics() *((Transform)_rdbMesh.Members[0]).anim_matrix.ToNumerics();

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

        private void BuildAnimations(SceneData sceneData, Dictionary<int, int> nodeMap)
        {
            var masterAnimation = new AnimationData { Name = "Animation" };
            float maxDuration = 0f;

            for (int i = 0; i < _rdbMesh.Members.Count; i++)
            {
                if (_rdbMesh.Members[i] is RTriMesh_t triMesh && nodeMap.TryGetValue(i, out int nodeIndex))
                {
                    var animData = ExtractAnimationData(triMesh, nodeIndex);
                    if (animData != null)
                    {
                        sceneData.Nodes[nodeIndex].HasAnimation = true;
                        masterAnimation.Channels.AddRange(animData.Channels);
                        maxDuration = Math.Max(maxDuration, animData.Duration);
                    }
                }
            }

            if (masterAnimation.Channels.Count > 0)
            {
                masterAnimation.Duration = maxDuration;
                sceneData.Animations.Add(masterAnimation);
            }
        }

        private AnimationData? ExtractAnimationData(RTriMesh_t triMesh, int nodeIndex)
        {
            if (triMesh.anim == -1)
                return null;

            if (_rdbMesh.Members[triMesh.anim] is not FAFAnim_t animClass)
                return null;

            // Skip single keyframe animations
            if (animClass.num_trans_keys <= 1 && animClass.num_rot_keys <= 1)
                return null;

            var animData = new AnimationData
            {
                Name = $"Animation_{nodeIndex}"
            };

            float maxTime = 0f;

            if (animClass.num_rot_keys > 1 && animClass.RotKeys != null)
            {
                var rotationChannel = new AnimationChannelData
                {
                    NodeIndex = nodeIndex,
                    Path = "rotation"
                };

                foreach (var rotKey in animClass.RotKeys)
                {
                    var combinedRotation = rotKey.Rotation.ToNumerics() * triMesh.local_rot.ToNumerics();
                    rotationChannel.Keyframes.Add(new KeyframeData(rotKey.Time, [combinedRotation.X, combinedRotation.Y, combinedRotation.Z, combinedRotation.W]));
                    maxTime = Math.Max(maxTime, rotKey.Time);
                }

                if (rotationChannel.Keyframes.Count > 0)
                    animData.Channels.Add(rotationChannel);
            }

            if (animClass.num_trans_keys > 1 && animClass.TransKeys != null)
            {
                var translationChannel = new AnimationChannelData
                {
                    NodeIndex = nodeIndex,
                    Path = "translation"
                };

                foreach (var transKey in animClass.TransKeys)
                {
                    var combinedTranslation = transKey.Translation.ToNumerics() + triMesh.local_pos.ToNumerics();
                    translationChannel.Keyframes.Add(new KeyframeData(transKey.Time, [combinedTranslation.X, combinedTranslation.Y, combinedTranslation.Z]));
                    maxTime = Math.Max(maxTime, transKey.Time);
                }

                if (translationChannel.Keyframes.Count > 0)
                    animData.Channels.Add(translationChannel);
            }

            // Add scale channel with single keyframe
            var scaleChannel = new AnimationChannelData
            {
                NodeIndex = nodeIndex,
                Path = "scale"
            };

            scaleChannel.Keyframes.Add(new KeyframeData(0f, [1f, 1f, 1f]));
            animData.Channels.Add(scaleChannel);
            animData.Duration = maxTime;

            return animData.Channels.Count > 0 ? animData : null;
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