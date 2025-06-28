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
            // Buffer targets
            public const int ARRAY_BUFFER = 34962;
            public const int ELEMENT_ARRAY_BUFFER = 34963;

            // Component types
            public const int FLOAT = 5126;
            public const int UNSIGNED_SHORT = 5123;

            // Primitive modes
            public const int TRIANGLES = 4;
        }

        /// <summary>
        /// Creates a complete glTF structure from mesh data and buffer information
        /// </summary>
        public static Gltf Create(StaticMeshData meshData, out byte[] bufferData)
        {
            BinaryBufferResult bufferResult = BinaryBufferBuilder.CreateBuffer(meshData);
            bufferData = bufferResult.Data;

            return new Gltf
            {
                Asset = CreateAsset(),
                Buffers = CreateBuffers(bufferResult.Data.Length),
                BufferViews = CreateBufferViews(bufferResult.Layout),
                Accessors = CreateAccessors(meshData, bufferResult.Layout),
                Meshes = CreateMeshes(),
                Nodes = CreateNodes(),
                Scenes = CreateScenes(),
                Scene = 0
            };
        }

        private static Asset CreateAsset()
        {
            return new Asset();
        }

        private static Buffer[] CreateBuffers(int binaryBufferLength)
        {
            return [new Buffer
            {
                Uri = null, // Embedded in GLB
                ByteLength = binaryBufferLength
            }];
        }

        private static BufferView[] CreateBufferViews(BufferLayout layout)
        {
            return [
                // Vertex buffer view
                new BufferView
                {
                    Buffer = 0,
                    ByteOffset = layout.VertexSection.Offset,
                    ByteLength = layout.VertexSection.Length,
                    Target = Constants.ARRAY_BUFFER
                },
                // Index buffer view
                new BufferView
                {
                    Buffer = 0,
                    ByteOffset = layout.IndexSection.Offset,
                    ByteLength = layout.IndexSection.Length,
                    Target = Constants.ELEMENT_ARRAY_BUFFER
                }
            ];
        }

        private static Accessor[] CreateAccessors(StaticMeshData meshData, BufferLayout layout)
        {
            return [
                // Vertex accessor
                new Accessor
                {
                    BufferView = 0,
                    ByteOffset = 0,
                    ComponentType = Constants.FLOAT,
                    Count = meshData.Vertices.Length,
                    Type = "VEC3",
                    Min = [meshData.Bounds.Min.X, meshData.Bounds.Min.Y, meshData.Bounds.Min.Z],
                    Max = [meshData.Bounds.Max.X, meshData.Bounds.Max.Y, meshData.Bounds.Max.Z]
                },
                // Index accessor
                new Accessor
                {
                    BufferView = 1,
                    ByteOffset = 0,
                    ComponentType = Constants.UNSIGNED_SHORT,
                    Count = meshData.Indices.Length,
                    Type = "SCALAR"
                }
            ];
        }

        private static Mesh[] CreateMeshes()
        {
            return [
                new Mesh
                {
                    Primitives = [
                        new Primitive
                        {
                            Attributes = new Dictionary<string, int>
                            {
                                { "POSITION", 0 }
                            },
                            Indices = 1,
                            Mode = Constants.TRIANGLES
                        }
                    ]
                }
            ];
        }

        private static Node[] CreateNodes()
        {
            return [new Node { Mesh = 0 }];
        }

        private static Scene[] CreateScenes()
        {
            return [new Scene { Nodes = [0] }];
        }

        #region Records
        /// <summary>
        /// Represents a section of data within a binary buffer
        /// </summary>
        private readonly record struct BufferSection(int Offset, int Length);

        /// <summary>
        /// Describes the layout of vertex and index data within a binary buffer
        /// </summary>
        private readonly record struct BufferLayout(BufferSection VertexSection, BufferSection IndexSection);

        /// <summary>
        /// Contains the binary buffer data and its layout information
        /// </summary>
        private readonly record struct BinaryBufferResult(byte[] Data, BufferLayout Layout);
        #endregion
        #region BinaryBufferBuilder
        /// <summary>
        /// Builds binary buffers for glTF files containing vertex and index data
        /// </summary>
        /// 
        private class BinaryBufferBuilder
        {
            /// <summary>
            /// Creates a binary buffer from mesh data with proper alignment
            /// </summary>
            internal static BinaryBufferResult CreateBuffer(StaticMeshData meshData)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);

                var vertexSection = WriteVertexData(writer, meshData.Vertices);
                AlignStream(writer, 4);
                var indexSection = WriteIndexData(writer, meshData.Indices);
                AlignStream(writer, 4);

                var layout = new BufferLayout(vertexSection, indexSection);
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
        #endregion
    }
}