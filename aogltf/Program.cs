using AODB;
using AODB.Common.RDBObjects;
using System.Numerics;

namespace aogltf
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //AOExportTest();

            //Testing
            GltfExporter.WriteAllData($"C:\\Users\\abdul\\Desktop\\New folder (3)", "test", FileExtension.Gltf, MakeTestCube());
            GltfExporter.WriteAllData($"C:\\Users\\abdul\\Desktop\\New folder (3)", "test2", FileExtension.Glb, MakeTestCube());
        }

        private static void AOExportTest()
        {
            string aoPath = "D:\\Anarchy Online";
            int meshId = 283378;


            RdbController rdbController = new RdbController(aoPath);
            var mesh = rdbController.Get<RDBMesh>(meshId);
        }

        private static StaticMeshData MakeTestCube()
        {
            Vector3[] positions =
            [
                new(-1, -1, -1), new(1, -1, -1),
                new(1,  1, -1), new(-1, 1, -1),
                new(-1, -1,  1), new(1, -1,  1),
                new(1,  1,  1), new(-1, 1,  1)
            ];

            ushort[] indices =
            [
                0, 1, 2, 2, 3, 0,
                4, 5, 6, 6, 7, 4,
                3, 2, 6, 6, 7, 3,
                0, 1, 5, 5, 4, 0,
                1, 2, 6, 6, 5, 1,
                0, 3, 7, 7, 4, 0
            ];

            return new StaticMeshData(positions, indices);
        }
    }
}