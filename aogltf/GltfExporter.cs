namespace aogltf
{
    /// <summary>
    /// Converts and exports mesh data to glTF/GLB format.
    ///
    /// <para>glTF (Graphics Language Transmission Format) comes in two variants:</para>
    ///
    /// <para>glTF (.gltf):
    /// - JSON-based format that stores scene description in human-readable text
    /// - Binary data (vertices, textures, animations) are stored in separate .bin files
    /// - Textures are referenced as external image files (.jpg, .png, etc.)
    /// - Results in multiple files that must be kept together</para>
    ///
    /// <para>GLB (.glb):
    /// - Binary format that packages everything into a single file
    /// - Contains the same JSON scene description but in a compact binary container
    /// - All binary data (geometry, textures) is embedded within the single file</para>
    ///
    /// <para>Both formats contain identical 3D scene information - the difference is purely
    /// in packaging and delivery.</para>
    /// </summary>
    internal class GltfExporter
    {
        /// <summary>
        /// Exports given object to the specified format at the given location
        /// </summary>
        /// <param name="outputFolder">Directory where the file will be written</param>
        /// <param name="fileNameNoExtension">Base filename without extension</param>
        /// <param name="fileExtension">Target format (glTF or GLB)</param>
        /// <param name="objectData">Object data hierarchy</param>
        public static void WriteAllData(string outputFolder, string fileNameNoExtension, FileExtension fileExtension, ObjectNode objectData)
        {
            switch (fileExtension)
            {
                case FileExtension.Gltf:
                    WriteGltf(outputFolder, fileNameNoExtension, objectData);
                    break;
                case FileExtension.Glb:
                    WriteGlb(outputFolder, fileNameNoExtension, objectData);
                    break;
                default:
                    throw new ArgumentException($"Unsupported file extension: {fileExtension}");
            }
        }

        private static void WriteGltf(string outputFolder, string fileNameNoExtension, ObjectNode nestedObjectData)
        {
            Gltf gltf = GltfBuilder.Create(nestedObjectData, out byte[] bufferData);
            gltf.Buffers[0].Uri = $"{fileNameNoExtension}.bin";
            var binPath = Path.Combine(outputFolder, $"{fileNameNoExtension}.bin");
            File.WriteAllBytes(binPath, bufferData);
            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{fileNameNoExtension}.gltf"), gltf);
        }

        private static void WriteGlb(string outputFolder, string fileNameNoExtension, ObjectNode nestedObjectData)
        {
            Gltf gltf = GltfBuilder.Create(nestedObjectData, out byte[] bufferData);
            GltfFileWriter.WriteToFile(Path.Combine(outputFolder, $"{fileNameNoExtension}.glb"), gltf, bufferData);
        }
    }
}

public enum FileExtension
{
    Gltf,
    Glb
}