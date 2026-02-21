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

            Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);

            if (terrainData.Atlas != null)
            {
                AddTerrainMaterial(gltf, terrainData.Atlas, ref bufferData);
            }

            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"Playfield_Terrain_{PlayfieldId}.glb"), gltf, bufferData);

            return true;
        }

        public override bool ExportGltf(string outputFolder, PfTerrainData terrainData)
        {
            var objectName = $"Playfield_Terrain_{PlayfieldId}";

            var sceneBuilder = new TerrainSceneBuilder();
            SceneData sceneData = sceneBuilder.BuildTerrainScene(terrainData);

            Gltf gltf = AOGltfBuilder.Create(sceneData, out byte[] bufferData);

            if (terrainData.Atlas != null)
            {
                AddTerrainMaterial(gltf, terrainData.Atlas, ref bufferData);
            }

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
                {
                    foreach (var primitive in mesh.Primitives)
                    {
                        primitive.Material = 0;
                    }
                }
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

            var verts = chunk.Vertices.Select(v => new System.Numerics.Vector3(v.X, v.Y, -v.Z)).ToArray();
            var normals = chunk.Normals.Select(n => new System.Numerics.Vector3(n.X, n.Y, -n.Z)).ToArray();
            var uvs = chunk.UVs.Select(uv => new System.Numerics.Vector2(uv.X, uv.Y)).ToArray();

            var indices = new ushort[chunk.Triangles.Length];
            for (int i = 0; i < chunk.Triangles.Length; i += 3)
            {
                indices[i] = (ushort)chunk.Triangles[i];
                indices[i + 1] = (ushort)chunk.Triangles[i + 2];
                indices[i + 2] = (ushort)chunk.Triangles[i + 1];
            }

            var primitive = new PrimitiveData(verts, normals, uvs, indices, null);
            meshData.Primitives.Add(primitive);

            int meshIndex = sceneData.Meshes.Count;
            sceneData.Meshes.Add(meshData);

            var (translation, rotation, scale) = DecomposeMatrix(chunk.Transform);

            var node = new NodeData
            {
                Name = $"TerrainChunk_{chunkIndex}",
                MeshIndex = meshIndex,
                Translation = translation,
                Rotation = rotation,
                Scale = scale
            };

            return node;
        }

        private (System.Numerics.Vector3?, System.Numerics.Quaternion?, System.Numerics.Vector3?) DecomposeMatrix(AODB.Common.Structs.Matrix matrix)
        {
            // Fix coordinate system
            var mat = new System.Numerics.Matrix4x4(
                -matrix.values[0, 0], matrix.values[1, 0], -matrix.values[2, 0], matrix.values[3, 0],
                -matrix.values[0, 1], matrix.values[1, 1], -matrix.values[2, 1], matrix.values[3, 1],
                -matrix.values[0, 2], matrix.values[1, 2], -matrix.values[2, 2], matrix.values[3, 2],
                -matrix.values[0, 3], matrix.values[1, 3], -matrix.values[2, 3], matrix.values[3, 3]
            );

            System.Numerics.Matrix4x4.Decompose(mat, out var scale, out var rotation, out var translation);

            return (
                translation != System.Numerics.Vector3.Zero ? translation : null,
                rotation != System.Numerics.Quaternion.Identity ? rotation : null,
                scale != System.Numerics.Vector3.One ? scale : null
            );
        }
    }
}