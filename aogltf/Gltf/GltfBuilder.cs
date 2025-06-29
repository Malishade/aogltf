using System.Numerics;

namespace aogltf
{
    internal class GltfBuilder
    {
        private static class Constants
        {
            public const int ARRAY_BUFFER = 34962;
            public const int ELEMENT_ARRAY_BUFFER = 34963;
            public const int FLOAT = 5126;
            public const int UNSIGNED_SHORT = 5123;
            public const int TRIANGLES = 4;
        }

        public static Gltf Create(SceneData sceneData, out byte[] bufferData)
        {
            var bufferResult = BinaryBufferBuilder.CreateBuffer(sceneData.Meshes.ToArray());
            bufferData = bufferResult.Data;

            return new Gltf
            {
                Asset = new Asset(),
                Buffers = CreateBuffers(bufferResult.Data.Length),
                BufferViews = CreateBufferViews(bufferResult.Layout),
                Accessors = CreateAccessors(sceneData.Meshes.ToArray(), bufferResult.Layout),
                Materials = CreateMaterials(sceneData.Materials.ToArray()),
                Meshes = CreateMeshes(sceneData.Meshes.ToArray()),
                Nodes = CreateNodes(sceneData),
                Scenes = CreateScenes(sceneData),
                Scene = 0,
            };
        }

        private static Node[] CreateNodes(SceneData sceneData)
        {
            var gltfNodes = new Node[sceneData.Nodes.Count];

            for (int i = 0; i < sceneData.Nodes.Count; i++)
            {
                var nodeData = sceneData.Nodes[i];

                gltfNodes[i] = new Node
                {
                    Mesh = nodeData.MeshIndex,
                    Children = nodeData.ChildIndices.Count > 0 ? nodeData.ChildIndices.ToArray() : null,
                    Translation = nodeData.Translation.HasValue ? nodeData.Translation.Value.ToArray() : null,
                    Rotation = nodeData.Rotation.HasValue ? nodeData.Rotation.Value.ToArray() : null,
                    Scale = nodeData.Scale.HasValue ? nodeData.Scale.Value.ToArray() : null,
                    Name = nodeData.Name
                };
            }

            return gltfNodes;
        }

        private static Scene[] CreateScenes(SceneData sceneData)
        {
            return
            [
                new Scene
                {
                    Nodes = new int[] { sceneData.RootNodeIndex }
                }
            ];
        }

        private static Material[] CreateMaterials(MaterialData[] materialDataArray)
        {
            var materials = new Material[materialDataArray.Length];

            for (int i = 0; i < materialDataArray.Length; i++)
            {
                var data = materialDataArray[i];
                materials[i] = new Material
                {
                    Name = data.Name,
                    PbrMetallicRoughness = new PbrMetallicRoughness
                    {
                        BaseColorFactor = data.BaseColor.HasValue ? [data.BaseColor.Value.X, data.BaseColor.Value.Y, data.BaseColor.Value.Z, data.BaseColor.Value.W] : null,
                        MetallicFactor = data.MetallicFactor,
                        RoughnessFactor = data.RoughnessFactor,
                        BaseColorTexture = data.BaseColorTextureIndex.HasValue ? new TextureInfo { Index = data.BaseColorTextureIndex.Value } : null
                    },
                    NormalTexture = data.NormalTextureIndex.HasValue ? new NormalTextureInfo { Index = data.NormalTextureIndex.Value } : null,
                    EmissiveFactor = data.EmissiveFactor?.ToArray(),
                    AlphaMode = data.AlphaMode,
                    AlphaCutoff = data.AlphaCutoff,
                    DoubleSided = data.DoubleSided
                };
            }

            return materials;
        }

        private static Mesh[] CreateMeshes(MeshData[] meshDataArray)
        {
            var meshes = new Mesh[meshDataArray.Length];
            int accessorIndex = 0;

            for (int i = 0; i < meshDataArray.Length; i++)
            {
                var meshData = meshDataArray[i];
                var attributes = new Dictionary<string, int>
                {
                    { "POSITION", accessorIndex++ }
                };

                if (meshData.Normals != null && meshData.Normals.Length > 0)
                    attributes.Add("NORMAL", accessorIndex++);

                if (meshData.UVs != null && meshData.UVs.Length > 0)
                    attributes.Add("TEXCOORD_0", accessorIndex++);

                int indicesAccessor = accessorIndex++;

                meshes[i] = new Mesh
                {
                    Primitives = new[]
                    {
                        new Primitive
                        {
                            Attributes = attributes,
                            Indices = indicesAccessor,
                            Mode = Constants.TRIANGLES,
                            Material = meshData.MaterialIndex
                        }
                    }
                };
            }

            return meshes;
        }

        private static Buffer[] CreateBuffers(int binaryBufferLength)
        {
            return new Buffer[]
            {
                new Buffer
                {
                    Uri = null,
                    ByteLength = binaryBufferLength
                }
            };
        }

        private static BufferView[] CreateBufferViews(BufferLayout layout)
        {
            var views = new List<BufferView>();

            foreach (var meshLayout in layout.MeshLayouts)
            {
                // Vertex buffer view
                views.Add(new BufferView
                {
                    Buffer = 0,
                    ByteOffset = meshLayout.VertexSection.Offset,
                    ByteLength = meshLayout.VertexSection.Length,
                    Target = Constants.ARRAY_BUFFER
                });

                // Normal buffer view (if normals exist)
                if (meshLayout.NormalSection.Length > 0)
                {
                    views.Add(new BufferView
                    {
                        Buffer = 0,
                        ByteOffset = meshLayout.NormalSection.Offset,
                        ByteLength = meshLayout.NormalSection.Length,
                        Target = Constants.ARRAY_BUFFER
                    });
                }

                // UV buffer view (if UVs exist)
                if (meshLayout.UVSection.Length > 0)
                {
                    views.Add(new BufferView
                    {
                        Buffer = 0,
                        ByteOffset = meshLayout.UVSection.Offset,
                        ByteLength = meshLayout.UVSection.Length,
                        Target = Constants.ARRAY_BUFFER
                    });
                }

                // Index buffer view
                views.Add(new BufferView
                {
                    Buffer = 0,
                    ByteOffset = meshLayout.IndexSection.Offset,
                    ByteLength = meshLayout.IndexSection.Length,
                    Target = Constants.ELEMENT_ARRAY_BUFFER
                });
            }

            return views.ToArray();
        }

        private static Accessor[] CreateAccessors(MeshData[] meshDataArray, BufferLayout layout)
        {
            var accessors = new List<Accessor>();
            int bufferViewIndex = 0;

            foreach (var meshData in meshDataArray)
            {
                // Position accessor
                accessors.Add(new Accessor
                {
                    BufferView = bufferViewIndex++,
                    ByteOffset = 0,
                    ComponentType = Constants.FLOAT,
                    Count = meshData.Vertices.Length,
                    Type = "VEC3",
                    Min = [meshData.Bounds.Min.X, meshData.Bounds.Min.Y, meshData.Bounds.Min.Z],
                    Max = new[] { meshData.Bounds.Max.X, meshData.Bounds.Max.Y, meshData.Bounds.Max.Z }
                });

                // Normal accessor (if normals exist)
                if (meshData.Normals != null && meshData.Normals.Length > 0)
                {
                    accessors.Add(new Accessor
                    {
                        BufferView = bufferViewIndex++,
                        ByteOffset = 0,
                        ComponentType = Constants.FLOAT,
                        Count = meshData.Normals.Length,
                        Type = "VEC3"
                    });
                }

                // UV accessor (if UVs exist)
                if (meshData.UVs != null && meshData.UVs.Length > 0)
                {
                    accessors.Add(new Accessor
                    {
                        BufferView = bufferViewIndex++,
                        ByteOffset = 0,
                        ComponentType = Constants.FLOAT,
                        Count = meshData.UVs.Length,
                        Type = "VEC2"
                        // Is min/max needed here? (maybe for optimization??)
                    });
                }

                // Index accessor
                accessors.Add(new Accessor
                {
                    BufferView = bufferViewIndex++,
                    ByteOffset = 0,
                    ComponentType = Constants.UNSIGNED_SHORT,
                    Count = meshData.Indices.Length,
                    Type = "SCALAR"
                });
            }

            return accessors.ToArray();
        }

        private readonly record struct BufferSection(int Offset, int Length);
        private readonly record struct MeshLayout(BufferSection VertexSection, BufferSection NormalSection, BufferSection UVSection, BufferSection IndexSection);
        private readonly record struct BufferLayout(MeshLayout[] MeshLayouts);
        private readonly record struct BinaryBufferResult(byte[] Data, BufferLayout Layout);

        private class BinaryBufferBuilder
        {
            internal static BinaryBufferResult CreateBuffer(MeshData[] meshDataArray)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                var meshLayouts = new MeshLayout[meshDataArray.Length];

                for (int i = 0; i < meshDataArray.Length; i++)
                {
                    var vertexSection = WriteVertexData(writer, meshDataArray[i].Vertices);
                    AlignStream(writer, 4);

                    var normalSection = WriteNormalData(writer, meshDataArray[i].Normals);
                    AlignStream(writer, 4);

                    var uvSection = WriteUVData(writer, meshDataArray[i].UVs);
                    AlignStream(writer, 4);

                    var indexSection = WriteIndexData(writer, meshDataArray[i].Indices);
                    AlignStream(writer, 4);

                    meshLayouts[i] = new MeshLayout(vertexSection, normalSection, uvSection, indexSection);
                }

                return new BinaryBufferResult(stream.ToArray(), new BufferLayout(meshLayouts));
            }

            private static BufferSection WriteVertexData(BinaryWriter writer, Vector3[] vertices)
            {
                int offset = (int)writer.BaseStream.Position;
                foreach (var v in vertices)
                {
                    writer.Write(v.X); writer.Write(v.Y); writer.Write(v.Z);
                }
                return new BufferSection(offset, vertices.Length * sizeof(float) * 3);
            }

            private static BufferSection WriteNormalData(BinaryWriter writer, Vector3[] normals)
            {
                int offset = (int)writer.BaseStream.Position;
                if (normals != null)
                {
                    foreach (var n in normals)
                    {
                        writer.Write(n.X); writer.Write(n.Y); writer.Write(n.Z);
                    }
                }
                return new BufferSection(offset, (normals?.Length ?? 0) * sizeof(float) * 3);
            }

            private static BufferSection WriteUVData(BinaryWriter writer, Vector2[] uvs)
            {
                int offset = (int)writer.BaseStream.Position;
                if (uvs != null)
                {
                    foreach (var uv in uvs)
                    {
                        writer.Write(uv.X); writer.Write(uv.Y);
                    }
                }
                return new BufferSection(offset, (uvs?.Length ?? 0) * sizeof(float) * 2);
            }

            private static BufferSection WriteIndexData(BinaryWriter writer, ushort[] indices)
            {
                int offset = (int)writer.BaseStream.Position;
                foreach (var index in indices)
                {
                    writer.Write(index);
                }
                return new BufferSection(offset, indices.Length * sizeof(ushort));
            }

            private static void AlignStream(BinaryWriter writer, int alignment)
            {
                while (writer.BaseStream.Position % alignment != 0)
                {
                    writer.Write((byte)0);
                }
            }
        }
    }
}
