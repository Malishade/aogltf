using System.Numerics;

namespace aogltf
{
    /// <summary>
    /// Builds glTF JSON structures from mesh data and buffer layouts
    /// </summary>
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

        public static Gltf Create(ObjectNode rootNode, out byte[] bufferData)
        {
            var meshDataList = new List<StaticMeshData>();
            var nodeDataList = new List<NodeData>();

            CollectMeshesAndNodes(rootNode, meshDataList, nodeDataList, null);

            var bufferResult = BinaryBufferBuilder.CreateBuffer([.. meshDataList]);
            bufferData = bufferResult.Data;

            return new Gltf
            {
                Asset = CreateAsset(),
                Buffers = CreateBuffers(bufferResult.Data.Length),
                BufferViews = CreateBufferViews(bufferResult.Layout),
                Accessors = CreateAccessors(meshDataList.ToArray(), bufferResult.Layout),
                Meshes = CreateMeshes(meshDataList.Count),
                Nodes = CreateNodes(nodeDataList),
                Scenes = CreateScenes(),
                Scene = 0
            };
        }

        private static void CollectMeshesAndNodes(ObjectNode obj, List<StaticMeshData> meshes, List<NodeData> nodes, int? parentIndex)
        {
            int currentNodeIndex = nodes.Count;

            var nodeData = new NodeData
            {
                MeshIndex = obj.MeshData != null ? meshes.Count : null,
                Translation = obj.Translation != Vector3.Zero ? obj.Translation : null,
                Rotation = obj.Rotation != Quaternion.Identity ? obj.Rotation : null,
                Scale = obj.Scale != Vector3.One ? obj.Scale : null,
                Name = obj.Name,
                Children = new List<int>()
            };

            if (obj.MeshData != null)
            {
                meshes.Add(obj.MeshData);
            }

            nodes.Add(nodeData);

            if (parentIndex.HasValue)
            {
                nodes[parentIndex.Value].Children.Add(currentNodeIndex);
            }

            foreach (var child in obj.Children)
            {
                CollectMeshesAndNodes(child, meshes, nodes, currentNodeIndex);
            }
        }

        private static Node[] CreateNodes(List<NodeData> nodeDataList)
        {
            var nodes = new Node[nodeDataList.Count];

            for (int i = 0; i < nodeDataList.Count; i++)
            {
                var nodeData = nodeDataList[i];
                nodes[i] = new Node
                {
                    Mesh = nodeData.MeshIndex,
                    Children = nodeData.Children.Count > 0 ? nodeData.Children.ToArray() : null,
                    Translation = nodeData.Translation?.ToArray(),
                    Rotation = nodeData.Rotation?.ToArray(),
                    Scale = nodeData.Scale?.ToArray(),
                    Name = nodeData.Name
                };
            }

            return nodes;
        }

        private static Mesh[] CreateMeshes(int meshCount)
        {
            var meshes = new Mesh[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                meshes[i] = new Mesh
                {
                    Primitives = [
                        new Primitive
                        {
                            Attributes = new Dictionary<string, int> { { "POSITION", i * 2 } },
                            Indices = i * 2 + 1,
                            Mode = Constants.TRIANGLES
                        }
                    ]
                };
            }
            return meshes;
        }

        private class NodeData
        {
            public int? MeshIndex { get; set; }
            public Vector3? Translation { get; set; }
            public Quaternion? Rotation { get; set; }
            public Vector3? Scale { get; set; }
            public string? Name { get; set; }
            public List<int> Children { get; set; } = new();
        }

        private static Asset CreateAsset() => new Asset();

        private static Buffer[] CreateBuffers(int binaryBufferLength) => [new Buffer { Uri = null, ByteLength = binaryBufferLength }];

        private static BufferView[] CreateBufferViews(BufferLayout layout)
        {
            var views = new List<BufferView>();

            for (int i = 0; i < layout.MeshLayouts.Length; i++)
            {
                var meshLayout = layout.MeshLayouts[i];

                // Vertex buffer view
                views.Add(new BufferView
                {
                    Buffer = 0,
                    ByteOffset = meshLayout.VertexSection.Offset,
                    ByteLength = meshLayout.VertexSection.Length,
                    Target = Constants.ARRAY_BUFFER
                });

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

        private static Accessor[] CreateAccessors(StaticMeshData[] meshDataArray, BufferLayout layout)
        {
            var accessors = new List<Accessor>();

            for (int i = 0; i < meshDataArray.Length; i++)
            {
                var meshData = meshDataArray[i];

                accessors.Add(new Accessor
                {
                    BufferView = i * 2,
                    ByteOffset = 0,
                    ComponentType = Constants.FLOAT,
                    Count = meshData.Vertices.Length,
                    Type = "VEC3",
                    Min = [meshData.Bounds.Min.X, meshData.Bounds.Min.Y, meshData.Bounds.Min.Z],
                    Max = [meshData.Bounds.Max.X, meshData.Bounds.Max.Y, meshData.Bounds.Max.Z]
                });

                accessors.Add(new Accessor
                {
                    BufferView = i * 2 + 1,
                    ByteOffset = 0,
                    ComponentType = Constants.UNSIGNED_SHORT,
                    Count = meshData.Indices.Length,
                    Type = "SCALAR"
                });
            }

            return accessors.ToArray();
        }

        private static Scene[] CreateScenes() => [new Scene { Nodes = [0] }];

        private readonly record struct BufferSection(int Offset, int Length);
        private readonly record struct MeshLayout(BufferSection VertexSection, BufferSection IndexSection);
        private readonly record struct BufferLayout(MeshLayout[] MeshLayouts);
        private readonly record struct BinaryBufferResult(byte[] Data, BufferLayout Layout);

        private class BinaryBufferBuilder
        {
            internal static BinaryBufferResult CreateBuffer(StaticMeshData[] meshDataArray)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);

                var meshLayouts = new MeshLayout[meshDataArray.Length];

                for (int i = 0; i < meshDataArray.Length; i++)
                {
                    var vertexSection = WriteVertexData(writer, meshDataArray[i].Vertices);
                    AlignStream(writer, 4);
                    var indexSection = WriteIndexData(writer, meshDataArray[i].Indices);
                    AlignStream(writer, 4);

                    meshLayouts[i] = new MeshLayout(vertexSection, indexSection);
                }

                var layout = new BufferLayout(meshLayouts);
                var data = stream.ToArray();

                return new BinaryBufferResult(data, layout);
            }

            private static BufferSection WriteVertexData(BinaryWriter writer, Vector3[] vertices)
            {
                int startOffset = (int)writer.BaseStream.Position;
                foreach (var vertex in vertices)
                {
                    writer.Write(vertex.X);
                    writer.Write(vertex.Y);
                    writer.Write(vertex.Z);
                }
                int length = vertices.Length * sizeof(float) * 3;
                return new BufferSection(startOffset, length);
            }

            private static BufferSection WriteIndexData(BinaryWriter writer, ushort[] indices)
            {
                int startOffset = (int)writer.BaseStream.Position;
                foreach (var index in indices)
                {
                    writer.Write(index);
                }
                int length = indices.Length * sizeof(ushort);
                return new BufferSection(startOffset, length);
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