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
                    Translation = nodeData.Translation?.ToArray(),
                    Rotation = nodeData.Rotation?.ToArray(),
                    Scale = nodeData.Scale?.ToArray(),
                    Name = nodeData.Name
                };
            }

            return gltfNodes;
        }

        private static Scene[] CreateScenes(SceneData sceneData)
        {
            return [new Scene { Nodes = [sceneData.RootNodeIndex] }];
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
                var primitives = new Primitive[meshData.Primitives.Count];

                for (int j = 0; j < meshData.Primitives.Count; j++)
                {
                    var prim = meshData.Primitives[j];
                    var attributes = new Dictionary<string, int> { { "POSITION", accessorIndex++ } };

                    if (prim.Normals?.Length > 0)
                        attributes["NORMAL"] = accessorIndex++;

                    if (prim.UVs?.Length > 0)
                        attributes["TEXCOORD_0"] = accessorIndex++;

                    int indicesAccessor = accessorIndex++;

                    primitives[j] = new Primitive
                    {
                        Attributes = attributes,
                        Indices = indicesAccessor,
                        Mode = Constants.TRIANGLES,
                        Material = prim.MaterialIndex
                    };
                }

                meshes[i] = new Mesh { Primitives = primitives };
            }

            return meshes;
        }

        private static Buffer[] CreateBuffers(int binaryBufferLength)
        {
            return [new Buffer { Uri = null, ByteLength = binaryBufferLength }];
        }

        private static BufferView[] CreateBufferViews(BufferLayout layout)
        {
            var views = new List<BufferView>();

            foreach (var meshLayout in layout.MeshLayouts)
            {
                foreach (var primLayout in meshLayout.Primitives)
                {
                    views.Add(new BufferView
                    {
                        Buffer = 0,
                        ByteOffset = primLayout.VertexSection.Offset,
                        ByteLength = primLayout.VertexSection.Length,
                        Target = Constants.ARRAY_BUFFER
                    });

                    if (primLayout.NormalSection.Length > 0)
                        views.Add(new BufferView
                        {
                            Buffer = 0,
                            ByteOffset = primLayout.NormalSection.Offset,
                            ByteLength = primLayout.NormalSection.Length,
                            Target = Constants.ARRAY_BUFFER
                        });

                    if (primLayout.UVSection.Length > 0)
                        views.Add(new BufferView
                        {
                            Buffer = 0,
                            ByteOffset = primLayout.UVSection.Offset,
                            ByteLength = primLayout.UVSection.Length,
                            Target = Constants.ARRAY_BUFFER
                        });

                    views.Add(new BufferView
                    {
                        Buffer = 0,
                        ByteOffset = primLayout.IndexSection.Offset,
                        ByteLength = primLayout.IndexSection.Length,
                        Target = Constants.ELEMENT_ARRAY_BUFFER
                    });
                }
            }

            return views.ToArray();
        }

        private static Accessor[] CreateAccessors(MeshData[] meshDataList, BufferLayout layout)
        {
            var accessors = new List<Accessor>();
            int bufferViewIndex = 0;

            foreach (var meshData in meshDataList)
            {
                foreach (var prim in meshData.Primitives)
                {
                    accessors.Add(new Accessor
                    {
                        BufferView = bufferViewIndex++,
                        ByteOffset = 0,
                        ComponentType = Constants.FLOAT,
                        Count = prim.Vertices.Length,
                        Type = "VEC3",
                        Min = [prim.Bounds.Min.X, prim.Bounds.Min.Y, prim.Bounds.Min.Z],
                        Max = [prim.Bounds.Max.X, prim.Bounds.Max.Y, prim.Bounds.Max.Z]
                    });

                    if (prim.Normals?.Length > 0)
                        accessors.Add(new Accessor
                        {
                            BufferView = bufferViewIndex++,
                            ByteOffset = 0,
                            ComponentType = Constants.FLOAT,
                            Count = prim.Normals.Length,
                            Type = "VEC3"
                        });

                    if (prim.UVs?.Length > 0)
                        accessors.Add(new Accessor
                        {
                            BufferView = bufferViewIndex++,
                            ByteOffset = 0,
                            ComponentType = Constants.FLOAT,
                            Count = prim.UVs.Length,
                            Type = "VEC2"
                        });

                    accessors.Add(new Accessor
                    {
                        BufferView = bufferViewIndex++,
                        ByteOffset = 0,
                        ComponentType = Constants.UNSIGNED_SHORT,
                        Count = prim.Indices.Length,
                        Type = "SCALAR"
                    });
                }
            }

            return accessors.ToArray();
        }

        private readonly record struct PrimitiveLayout(BufferSection VertexSection, BufferSection NormalSection, BufferSection UVSection, BufferSection IndexSection);
        private readonly record struct BufferSection(int Offset, int Length);
        private readonly record struct MeshLayout(PrimitiveLayout[] Primitives);
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
                    var meshData = meshDataArray[i];
                    var primLayouts = new PrimitiveLayout[meshData.Primitives.Count];

                    for (int j = 0; j < meshData.Primitives.Count; j++)
                    {
                        var prim = meshData.Primitives[j];
                        var v = WriteVertexData(writer, prim.Vertices); AlignStream(writer, 4);
                        var n = WriteNormalData(writer, prim.Normals); AlignStream(writer, 4);
                        var u = WriteUVData(writer, prim.UVs); AlignStream(writer, 4);
                        var iSec = WriteIndexData(writer, prim.Indices); AlignStream(writer, 4);

                        primLayouts[j] = new PrimitiveLayout(v, n, u, iSec);
                    }

                    meshLayouts[i] = new MeshLayout(primLayouts);
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
