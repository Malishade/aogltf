using gltf;
using AODB;

namespace aogltf
{
    public class TerrainExporter : PlayfieldExporterBase<PfTerrainData>
    {
        public TerrainExporter(RdbController rdbController) : base(rdbController)
        {
        }

        protected override PfTerrainData ParseData()
        {
            TerrainParser terrainParser = new TerrainParser(_rdbController);
            return terrainParser.Get(PlayfieldId);
        }

        public override bool ExportGlb(string outputFolder, PfTerrainData terrainData)
        {
            var sceneBuilder = new TerrainSceneBuilder();
            SceneData sceneData = sceneBuilder.BuildTerrainScene(terrainData);

            SceneTransformHelper.Apply(sceneData, ExportTransforms);

            Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);

            if (terrainData.Atlas != null)
                AddTerrainMaterial(gltf, terrainData.Atlas, ref bufferData);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"Playfield_Terrain_{PlayfieldId}.glb"), gltf, bufferData);
            return true;
        }

        public override bool ExportGltf(string outputFolder, PfTerrainData terrainData)
        {
            var objectName = $"Playfield_Terrain_{PlayfieldId}";

            var sceneBuilder = new TerrainSceneBuilder();
            SceneData sceneData = sceneBuilder.BuildTerrainScene(terrainData);

            SceneTransformHelper.Apply(sceneData, ExportTransforms);

            Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);

            if (terrainData.Atlas != null)
                AddTerrainMaterial(gltf, terrainData.Atlas, ref bufferData);

            gltf.Buffers[0].Uri = $"{objectName}.bin";

            var binPath = Path.Combine(outputFolder, $"{objectName}.bin");
            File.WriteAllBytes(binPath, bufferData);

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{objectName}.gltf"), gltf);
            return true;
        }

        private void AddTerrainMaterial(Gltf gltf, PfTextureAtlas atlas, ref byte[] bufferData)
        {
            int imageOffset = bufferData.Length;
            byte[] newBuffer = new byte[bufferData.Length + atlas.ImageData.Length];
            Array.Copy(bufferData, 0, newBuffer, 0, bufferData.Length);
            Array.Copy(atlas.ImageData, 0, newBuffer, imageOffset, atlas.ImageData.Length);
            bufferData = newBuffer;

            gltf.Buffers[0].ByteLength = newBuffer.Length;

            var bufferViews = gltf.BufferViews?.ToList() ?? new List<BufferView>();
            int imageBufferViewIndex = bufferViews.Count;
            bufferViews.Add(new BufferView
            {
                Buffer = 0,
                ByteOffset = imageOffset,
                ByteLength = atlas.ImageData.Length
            });
            gltf.BufferViews = bufferViews.ToArray();

            gltf.Images =
            [
                new Image
                {
                    Name = "TerrainAtlas",
                    MimeType = "image/png",
                    BufferView = imageBufferViewIndex
                }
            ];

            gltf.Samplers =
            [
                new Sampler
                {
                    MagFilter = 9729,
                    MinFilter = 9987,
                    WrapS = 10497,
                    WrapT = 10497
                }
            ];

            gltf.Textures =
            [
                new Texture
                {
                    Name = "TerrainTexture",
                    Sampler = 0,
                    Source = 0
                }
            ];

            gltf.Materials =
            [
                new Material
                {
                    Name = "TerrainMaterial",
                    PbrMetallicRoughness = new PbrMetallicRoughness
                    {
                        BaseColorTexture = new TextureInfo
                        {
                            Index = 0,
                            TexCoord = 0
                        },
                        MetallicFactor = 0.0f,
                        RoughnessFactor = 1.0f
                    },
                    DoubleSided = false
                }
            ];

            if (gltf.Meshes != null)
            {
                foreach (var mesh in gltf.Meshes)
                    foreach (var primitive in mesh.Primitives)
                        primitive.Material = 0;
            }
        }
    }

    public class TerrainSceneBuilder
    {
        public SceneData BuildTerrainScene(PfTerrainData terrainData)
        {
            var sceneData = new SceneData();
            var rootNode = new NodeData { Name = "Terrain_Root" };

            int rootIndex = sceneData.Nodes.Count;
            sceneData.Nodes.Add(rootNode);
            sceneData.RootNodeIndex = rootIndex;

            for (int i = 0; i < terrainData.Chunks.Count; i++)
            {
                var chunk = terrainData.Chunks[i];
                var chunkNode = CreateChunkNode(chunk, i, sceneData);

                int nodeIndex = sceneData.Nodes.Count;
                sceneData.Nodes.Add(chunkNode);
                rootNode.ChildIndices.Add(nodeIndex);
            }

            return sceneData;
        }

        private NodeData CreateChunkNode(PfTerrainChunk chunk, int chunkIndex, SceneData sceneData)
        {
            var meshData = new MeshData();

            var mat = new System.Numerics.Matrix4x4(
                chunk.Transform.values[0, 0], chunk.Transform.values[1, 0], chunk.Transform.values[2, 0], chunk.Transform.values[3, 0],
                chunk.Transform.values[0, 1], chunk.Transform.values[1, 1], chunk.Transform.values[2, 1], chunk.Transform.values[3, 1],
                chunk.Transform.values[0, 2], chunk.Transform.values[1, 2], chunk.Transform.values[2, 2], chunk.Transform.values[3, 2],
                chunk.Transform.values[0, 3], chunk.Transform.values[1, 3], chunk.Transform.values[2, 3], chunk.Transform.values[3, 3]
            );

            var verts = chunk.Vertices
                .Select(v => System.Numerics.Vector3.Transform(new System.Numerics.Vector3(v.X, v.Y, v.Z), mat))
                .ToArray();

            var normals = chunk.Normals
                .Select(n => System.Numerics.Vector3.TransformNormal(new System.Numerics.Vector3(n.X, n.Y, n.Z), mat))
                .ToArray();

            var uvs = chunk.UVs.Select(uv => new System.Numerics.Vector2(uv.X, uv.Y)).ToArray();
            var indices = chunk.Triangles.Select(t => (ushort)t).ToArray();

            var primitive = new PrimitiveData(verts, normals, uvs, indices, null);
            meshData.Primitives.Add(primitive);

            int meshIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);

            return new NodeData
            {
                Name = $"TerrainChunk_{chunkIndex}",
                MeshIndex = meshIndex
            };
        }
    }
}