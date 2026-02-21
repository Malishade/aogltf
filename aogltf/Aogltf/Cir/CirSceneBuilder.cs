using AODB.Common.RDBObjects;
using System.Numerics;

namespace aogltf
{
    public class CirSceneBuilder
    {

        private readonly RDBCatMesh _catMesh;
        private readonly List<AnimData> _animData;
        private NodeData[] _boneNodes;

        public CirSceneBuilder(RDBCatMesh catMesh, List<AnimData> catAnims)
        {
            _catMesh = catMesh;
            _animData = catAnims;
        }

        public SceneData BuildSceneHierarchy(out NodeData[] boneNodes)
        {

            var sceneData = new SceneData();

            BuildSkeletonHierarchy(sceneData);
            BuildMeshGroupNodes(sceneData);
            BuildMaterials(sceneData);
            BuildAnimations(sceneData);
            boneNodes = _boneNodes;
            return sceneData;
        }

        private void BuildSkeletonHierarchy(SceneData sceneData)
        {
            if (_catMesh.Joints == null || _catMesh.Joints.Count == 0)
            {
                sceneData.Nodes.Add(new NodeData { Name = "Root" });
                sceneData.RootNodeIndex = 0;
                _boneNodes = null;
                return;
            }

            _boneNodes = new NodeData[_catMesh.Joints.Count];

            for (int i = 0; i < _catMesh.Joints.Count; i++)
            {
                var joint = _catMesh.Joints[i];
                _boneNodes[i] = new NodeData
                {
                    Name = $"Bone_{i}_{joint.Name}",
                    HasAnimation = _animData != null && _animData.Count > 0
                };

                if (_animData != null && _animData.Count > 0)
                {
                    var firstAnim = _animData[0];
                    var boneData = firstAnim.CatAnim.Animation.BoneData.FirstOrDefault(x => x.BoneId == i);
                    if (!boneData.Equals(default))
                    {
                        if (boneData.TranslationKeys.Count > 0)
                        {
                            var pos = boneData.TranslationKeys[0].Position;
                            _boneNodes[i].Translation = new Vector3(pos.X, pos.Y, pos.Z);
                        }

                        if (boneData.RotationKeys.Count > 0)
                        {
                            var rot = boneData.RotationKeys[0].Rotation;
                            _boneNodes[i].Rotation = new Quaternion(rot.X, rot.Y, rot.Z, rot.W);
                        }
                    }
                }

                sceneData.Nodes.Add(_boneNodes[i]);
            }

            for (int i = 0; i < _catMesh.Joints.Count; i++)
            {
                var joint = _catMesh.Joints[i];
                for (int j = 0; j < joint.ChildJoints.Length; j++)
                {
                    int childIndex = joint.ChildJoints[j];
                    _boneNodes[i].ChildIndices.Add(sceneData.Nodes.IndexOf(_boneNodes[childIndex]));
                }
            }

            var rootBones = new List<int>();
            for (int i = 0; i < _catMesh.Joints.Count; i++)
            {
                bool hasParent = false;
                for (int j = 0; j < _catMesh.Joints.Count; j++)
                {
                    if (_catMesh.Joints[j].ChildJoints.Contains(i))
                    {
                        hasParent = true;
                        break;
                    }
                }
                if (!hasParent)
                {
                    rootBones.Add(sceneData.Nodes.IndexOf(_boneNodes[i]));
                }
            }

            var rootNode = new NodeData
            {
                Name = "Root",
                ChildIndices = rootBones
            };

            sceneData.Nodes.Insert(0, rootNode);
            sceneData.RootNodeIndex = 0;

            for (int i = 0; i < sceneData.Nodes.Count; i++)
            {
                for (int j = 0; j < sceneData.Nodes[i].ChildIndices.Count; j++)
                {
                    sceneData.Nodes[i].ChildIndices[j]++;
                }
            }
        }

        private void BuildMeshGroupNodes(SceneData sceneData)
        {
            int rootNodeIndex = sceneData.RootNodeIndex;

            for (int i = 0; i < _catMesh.MeshGroups.Count; i++)
            {
                var meshGroup = _catMesh.MeshGroups[i];
                var meshGroupNode = new NodeData
                {
                    Name = meshGroup.Name,
                    SourceMeshIndex = i
                };

                int nodeIndex = sceneData.Nodes.Count;
                sceneData.Nodes.Add(meshGroupNode);

                sceneData.Nodes[rootNodeIndex].ChildIndices.Add(nodeIndex);
            }
        }

        private void BuildMaterials(SceneData sceneData)
        {
            foreach (var catMaterial in _catMesh.Materials)
            {
                var materialData = new MaterialData
                {
                    Name = catMaterial.Name,
                    BaseColor = new Vector4(catMaterial.Diffuse.R, catMaterial.Diffuse.G, catMaterial.Diffuse.B, 1.0f),
                    EmissiveFactor = new Vector3(catMaterial.Emission.R, catMaterial.Emission.G, catMaterial.Emission.B),
                    MetallicFactor = 0.0f,
                    RoughnessFactor = 1.0f - catMaterial.Sheen / 100.0f
                };

                sceneData.Materials.Add(materialData);
            }
        }

        private void BuildAnimations(SceneData sceneData)
        {
            if (_animData == null || _animData.Count == 0)
                return;

            foreach (var catAnim in _animData)
            {
                var animData = new AnimationData
                {
                    Name = catAnim.Name
                };

                if (catAnim.CatAnim == null)
                    continue;

                if (catAnim.CatAnim.Animation.BoneData == null)
                    continue;

                foreach (var boneData in catAnim.CatAnim.Animation.BoneData)
                {
                    int nodeIndex = boneData.BoneId + 1; // +1 because we inserted root node

                    if (boneData.TranslationKeys.Count > 0)
                    {
                        var translationChannel = new AnimationChannelData
                        {
                            NodeIndex = nodeIndex,
                            Path = "translation",
                            Interpolation = "LINEAR"
                        };

                        foreach (var key in boneData.TranslationKeys)
                        {
                            var keyframe = new KeyframeData(
                                key.Time / 1000.0f,
                                [key.Position.X, key.Position.Y, key.Position.Z]
                            );
                            translationChannel.Keyframes.Add(keyframe);
                        }

                        animData.Channels.Add(translationChannel);
                    }

                    if (boneData.RotationKeys.Count > 0)
                    {
                        var rotationChannel = new AnimationChannelData
                        {
                            NodeIndex = nodeIndex,
                            Path = "rotation",
                            Interpolation = "LINEAR"
                        };

                        foreach (var key in boneData.RotationKeys)
                        {
                            var keyframe = new KeyframeData(
                                key.Time / 1000.0f,
                                [key.Rotation.X, key.Rotation.Y, key.Rotation.Z, key.Rotation.W]
                            );
                            rotationChannel.Keyframes.Add(keyframe);
                        }

                        animData.Channels.Add(rotationChannel);
                    }
                }

                if (animData.Channels.Count > 0)
                {
                    animData.Duration = animData.Channels
                        .SelectMany(c => c.Keyframes)
                        .Max(k => k.Time);

                    sceneData.Animations.Add(animData);
                }
            }
        }
    }
}