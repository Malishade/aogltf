using System.Numerics;

namespace aogltf
{
    /// <summary>
    /// Builds binary buffers for glTF files containing vertex and index data
    /// </summary>
    internal class BinaryBufferBuilder
    {
        /// <summary>
        /// Creates a binary buffer from mesh data with proper alignment
        /// </summary>
        public static BinaryBufferResult CreateBuffer(StaticMeshData meshData)
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

    /// <summary>
    /// Represents a section of data within a binary buffer
    /// </summary>
    internal readonly record struct BufferSection(int Offset, int Length);

    /// <summary>
    /// Describes the layout of vertex and index data within a binary buffer
    /// </summary>
    internal readonly record struct BufferLayout(BufferSection VertexSection, BufferSection IndexSection);

    /// <summary>
    /// Contains the binary buffer data and its layout information
    /// </summary>
    internal readonly record struct BinaryBufferResult(byte[] Data, BufferLayout Layout);
}