using AODB;
using gltf;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace aogltf
{
    public class StatelExporter : PlayfieldExporterBase<List<PfStatelData>>
    {
        public StatelExporter(RdbController rdbController) : base(rdbController)
        {
        }

        private void ConvertAndResolveMaterials(SceneData sceneData, StatelMaterialBuilder materialBuilder)
        {
            var materialMap = new Dictionary<int, int>();

            foreach (var mesh in sceneData.Meshes)
            {
                foreach (var prim in mesh.Primitives)
                {
                    if (!prim.MaterialIndex.HasValue)
                        continue;

                    int textureId = prim.MaterialIndex.Value;

                    if (materialMap.TryGetValue(textureId, out int gltfMatIndex))
                    {
                        prim.MaterialIndex = gltfMatIndex;
                        continue;
                    }

                    int? resolvedGltfMatIndex = materialBuilder.ResolveMaterialByTextureId(textureId);

                    if (resolvedGltfMatIndex.HasValue)
                    {
                        materialMap[textureId] = resolvedGltfMatIndex.Value;
                        prim.MaterialIndex = resolvedGltfMatIndex.Value;
                    }
                    else
                    {
                        prim.MaterialIndex = null;
                    }
                }
            }
        }

        protected override List<PfStatelData> ParseData()
        {
            StatelParser parser = new StatelParser(_rdbController);
            return parser.Get(PlayfieldId);
        }

        public override bool ExportGlb(string outputFolder, List<PfStatelData> statelData)
        {
            try
            {
                if (statelData == null || statelData.Count == 0)
                    return false;

                var sceneBuilder = new StatelSceneBuilder();
                SceneData sceneData = sceneBuilder.BuildStatelScene(statelData);

                SceneTransformHelper.Apply(sceneData, ExportTransforms);

                var materialBuilder = new StatelMaterialBuilder(_rdbController, outputFolder, true);
                materialBuilder.BuildMaterialsFromStatelData(statelData);

                ConvertAndResolveMaterials(sceneData, materialBuilder);

                Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);
                gltf.BindMaterials(materialBuilder);

                GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"Playfield_Statels_{PlayfieldId}.glb"), gltf, bufferData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool ExportGltf(string outputFolder, List<PfStatelData> data)
        {
            var objectName = $"Playfield_Statels_{PlayfieldId}";

            try
            {
                var statelParser = new StatelParser(_rdbController);
                var pfStatelData = statelParser.Get(PlayfieldId);

                if (pfStatelData == null || pfStatelData.Count == 0)
                    return false;

                var sceneBuilder = new StatelSceneBuilder();
                SceneData sceneData = sceneBuilder.BuildStatelScene(pfStatelData);

                SceneTransformHelper.Apply(sceneData, ExportTransforms);

                var materialBuilder = new StatelMaterialBuilder(_rdbController, outputFolder, false);
                materialBuilder.BuildMaterialsFromStatelData(pfStatelData);

                ConvertAndResolveMaterials(sceneData, materialBuilder);

                Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);
                gltf.BindMaterials(materialBuilder);
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

    public class StatelSceneBuilder
    {
        public SceneData BuildStatelScene(List<PfStatelData> pfStatelData)
        {
            var sceneData = new SceneData();
            var rootNode = new NodeData { Name = "Statels_Root" };

            int rootIndex = sceneData.Nodes.Count;
            sceneData.Nodes.Add(rootNode);
            sceneData.RootNodeIndex = rootIndex;

            foreach (var pfStatel in pfStatelData)
            {
                var statelNode = CreateStatelNode(pfStatel, sceneData);
                if (statelNode != null)
                {
                    int nodeIndex = sceneData.Nodes.Count;
                    sceneData.Nodes.Add(statelNode);
                    rootNode.ChildIndices.Add(nodeIndex);
                }
            }

            return sceneData;
        }

        private NodeData? CreateStatelNode(PfStatelData pfStatel, SceneData sceneData)
        {
            if (pfStatel.Meshes == null || pfStatel.Meshes.Count == 0)
                return null;

            var statelNode = new NodeData { Name = pfStatel.Name };

            ApplyStatelTransform(statelNode, pfStatel);

            foreach (var pfMesh in pfStatel.Meshes)
            {
                int? meshIndex = CreateMeshFromPfMeshData(pfMesh, sceneData);
                if (meshIndex.HasValue)
                {
                    var submeshNode = new NodeData
                    {
                        Name = pfMesh.Name,
                        MeshIndex = meshIndex.Value,
                        Translation = new Vector3(pfMesh.BasePosition.X, pfMesh.BasePosition.Y, pfMesh.BasePosition.Z),
                        Rotation = new Quaternion(
                            pfMesh.BaseRotation.X,
                            pfMesh.BaseRotation.Y,
                            pfMesh.BaseRotation.Z,
                            pfMesh.BaseRotation.W
                        )
                    };

                    int submeshNodeIndex = sceneData.Nodes.Count;
                    sceneData.Nodes.Add(submeshNode);
                    statelNode.ChildIndices.Add(submeshNodeIndex);
                }
            }

            return statelNode;
        }

        private int? CreateMeshFromPfMeshData(PfMeshData pfMesh, SceneData sceneData)
        {
            if (pfMesh.Vertices == null || pfMesh.Vertices.Count == 0)
                return null;

            var meshData = new MeshData();

            var verts = pfMesh.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();

            var normals = Array.Empty<Vector3>();
            if (pfMesh.Normals != null && pfMesh.Normals.Count > 0)
                normals = pfMesh.Normals.Select(n => new Vector3(n.X, n.Y, n.Z)).ToArray();

            var uvs = Array.Empty<Vector2>();
            if (pfMesh.UV != null && pfMesh.UV.Count > 0)
                uvs = pfMesh.UV.Select(uv => new Vector2(uv.X, uv.Y)).ToArray();

            var indices = Array.Empty<ushort>();
            if (pfMesh.Triangles != null && pfMesh.Triangles.Count > 0)
                indices = pfMesh.Triangles.Select(t => checked((ushort)t)).ToArray();

            var matIndex = pfMesh.Material?.TextureId ?? 0;

            var primitive = new PrimitiveData(verts, normals, uvs, indices, matIndex);
            meshData.Primitives.Add(primitive);

            int meshIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);
            return meshIndex;
        }

        private void ApplyStatelTransform(NodeData node, PfStatelData pfStatel)
        {
            var position = new Vector3(pfStatel.Position.X, pfStatel.Position.Y, pfStatel.Position.Z);

            var rotation = new Quaternion(
                pfStatel.Rotation.X,
                pfStatel.Rotation.Y,
                pfStatel.Rotation.Z,
                pfStatel.Rotation.W
            );

            Vector3 scale;
            if ((pfStatel.Flag & 1) > 0)
            {
                position.Y += pfStatel.ShearFactor * 4;
                scale = new Vector3(pfStatel.Scale.X, 1f, 1f);
            }
            else
            {
                scale = new Vector3(pfStatel.Scale.X, pfStatel.Scale.Y, pfStatel.Scale.Z);
            }

            node.Translation = position != Vector3.Zero ? position : null;
            node.Rotation = rotation != Quaternion.Identity ? rotation : null;
            node.Scale = scale != Vector3.One ? scale : null;
        }
    }

    public class StatelMaterialBuilder : MaterialBuilder
    {
        private readonly Dictionary<int, int> _textureToMaterialMap = new();

        public StatelMaterialBuilder(RdbController controller, string outputPath, bool isGlb) : base(controller, outputPath, isGlb)
        {
        }

        public void BuildMaterialsFromStatelData(List<PfStatelData> pfStatelData)
        {
            foreach (var pfStatel in pfStatelData)
            {
                if (pfStatel.Meshes == null)
                    continue;

                foreach (var pfMesh in pfStatel.Meshes)
                {
                    if (pfMesh.Material != null && !_textureToMaterialMap.ContainsKey(pfMesh.Material.TextureId))
                        BuildMaterialFromPfMaterial(pfMesh.Material);
                }
            }
        }

        private void BuildMaterialFromPfMaterial(PfMaterialData pfMaterial)
        {
            if (_textureToMaterialMap.ContainsKey(pfMaterial.TextureId))
                return;

            var gltfMaterial = CreateBasicMaterial(pfMaterial.TextureName);

            if (pfMaterial.TextureId > 0)
                SetBaseColorTexture(gltfMaterial, pfMaterial.TextureId);

            int matIdx = AddMaterialToList(gltfMaterial);
            _textureToMaterialMap[pfMaterial.TextureId] = matIdx;
        }

        public int? ResolveMaterialByTextureId(int textureId)
        {
            if (_textureToMaterialMap.TryGetValue(textureId, out int gltfIndex))
                return gltfIndex;

            return null;
        }
    }
}