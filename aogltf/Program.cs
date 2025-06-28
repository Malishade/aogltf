using AODB;
using AODB.Common.RDBObjects;
using AODB.Encoding;
using System.Numerics;

namespace aogltf
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AOExportTest();
            //GltfExporter.WriteAllData($"C:\\Users\\abdul\\Desktop\\New folder (3)", "nested_cubes", FileExtension.Glb, NestedTestCubes());
        }

        private static void AOExportTest()
        {
            string aoPath = "D:\\Funcom\\Anarchy Online";
            int meshId = 283378;


            RdbController rdbController = new RdbController(aoPath);
            RDBMesh mesh = rdbController.Get<RDBMesh>(meshId);

            Console.WriteLine("");
            Console.WriteLine("---");


            AbiffExporter blah = new AbiffExporter(mesh.RDBMesh_t);

            GltfExporter.WriteAllData($"C:\\Users\\abdul\\Desktop\\New folder (3)", "test", FileExtension.Glb, blah.BuildScene());
        }

        private static ObjectNode NestedTestCubes()
        {
            var rootObject = new ObjectNode
            {
                Name = "Root Container",
                Translation = Vector3.Zero,
                Children = new List<ObjectNode>
                {
                    new ObjectNode
                    {
                        Name = "Parent Cube",
                        MeshData = MakeTestCube(),
                        Translation = new Vector3(-2, 0, 0),
                        Scale = new Vector3(1.5f, 1.5f, 1.5f),
                        Children = new List<ObjectNode>
                        {
                            // Child cube (nested inside parent)
                            new ObjectNode
                            {
                                Name = "Child Cube",
                                MeshData = MakeTestCube(),
                                Translation = new Vector3(3, 2, 0),
                                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4),
                                Scale = new Vector3(0.6f, 0.6f, 0.6f)
                            }
                        }
                    },
                    new ObjectNode
                    {
                        Name = "Independent Cube",
                        MeshData = MakeTestCube(),
                        Translation = new Vector3(4, 0, 0),
                        Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6),
                        Scale = new Vector3(0.8f, 0.8f, 0.8f)
                    }
                }
            };

            return rootObject;
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