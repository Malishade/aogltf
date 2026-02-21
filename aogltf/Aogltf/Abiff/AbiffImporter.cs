using AODB.Common.DbClasses;
using AODB.Common.RDBObjects;
using AODB.Common.Structs;
using aogltf.importer;
using gltf;
using System.Numerics;
using static AODB.Common.DbClasses.RDBMesh_t;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace aogltf
{
    public class ImportOptions
    {
        public string AOPath;
        public string ModelPath;
        public int? RecordId = null;
    }

    public class GltfMaterial
    {
        public string FilePath { get; set; }
    }

    public class AbiffImporter
    {
        public static bool Import(ImportOptions opts)
        {
            var rdbPath = Path.Combine(opts.AOPath, "cd_image\\data\\db\\ResourceDatabase.idx");

            if (!File.Exists(rdbPath))
                return false;

            Directory.SetCurrentDirectory(opts.AOPath);
            var db = new NativeDbLite();
            db.LoadDatabase(rdbPath);

            try
            {
                var infoObject = db.Get<InfoObject>(1);

                RDBMesh_t mesh = Load(opts.ModelPath, infoObject, out Dictionary<int, GltfMaterial> mats);

                if (opts.RecordId != null)
                {
                    var existingMesh = db.Get<RDBMesh>(opts.RecordId.Value);
                    existingMesh.RDBMesh_t = mesh;
                    db.PutRaw((int)ResourceTypeId.RdbMesh, opts.RecordId.Value, 8008, existingMesh.Serialize());
                }

                foreach (var texture in mats)
                {
                    db.PutRaw((int)ResourceTypeId.Texture, texture.Key, 8008, File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(opts.ModelPath), texture.Value.FilePath)));
                }
                db.PutRaw((int)ResourceTypeId.InfoObject, 1, 8008, infoObject.Serialize());

                return true;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Failed to import mesh.");
            }
            finally
            {
                db.Dispose();
            }

            return false;
        }

        public static RDBMesh_t Load(string fileName, InfoObject infoObject, out Dictionary<int, GltfMaterial> mats)
        {
            mats = new Dictionary<int, GltfMaterial>();

            if (!GltfLoader.Load(fileName, out Gltf? gltf, out byte[] bufferData))
            {
                throw new Exception($"Failed to load GLTF file: {fileName}");
            }

            if (gltf == null)
            {
                throw new Exception("GLTF object is null");
            }

            RDBMesh_t rdbMesh = new RDBMesh_t();

            if (gltf.Scenes != null && gltf.Scenes.Length > 0)
            {
                var scene = gltf.Scenes[gltf.Scene];

                foreach (int nodeIndex in scene.Nodes)
                {
                    ProcessNode(gltf, nodeIndex, bufferData, rdbMesh, infoObject, mats, null);
                }
            }

            return rdbMesh;
        }

        private static Transform ProcessNode(Gltf gltf, int nodeIndex, byte[] bufferData, RDBMesh_t rdbMesh,
            InfoObject infoObject, Dictionary<int, GltfMaterial> mats, Transform parent)
        {
            var node = gltf.Nodes[nodeIndex];
            Transform transform = null;

            if (node.Mesh.HasValue)
            {
                Dictionary<int, FAFMaterial_t> materialMap = new Dictionary<int, FAFMaterial_t>();

                var triMesh = AddTriMesh(rdbMesh, node, nodeIndex);
                var triMeshData = rdbMesh.Members[triMesh.data] as FAFTriMeshData_t;

                var mesh = gltf.Meshes[node.Mesh.Value];
                var simpleMeshes = AddMesh(rdbMesh, gltf, mesh, bufferData, infoObject, materialMap, mats);

                foreach (var simpleMesh in simpleMeshes)
                {
                    triMeshData.AddMesh(rdbMesh.Members.IndexOf(simpleMesh));
                }

                GetNodeTransform(node, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                triMeshData.anim_pos = new AODB.Common.Structs.Vector3(position.X, position.Y, position.Z);
                triMeshData.anim_rot = new AODB.Common.Structs.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

                transform = triMesh;
            }
            else if (node.Name?.StartsWith("Attractor") == true || node.Name?.StartsWith("eff") == true)
            {
                transform = AddAttractor(rdbMesh, node);
            }
            else if (node.Children != null && node.Children.Length > 0)
            {
                transform = AddEmptyTransform(rdbMesh, node);
            }

            if (node.Children != null)
            {
                foreach (int childIndex in node.Children)
                {
                    Transform childTransform = ProcessNode(gltf, childIndex, bufferData, rdbMesh, infoObject, mats, transform);

                    if (childTransform != null && transform != null)
                    {
                        transform.AddChild(rdbMesh.Members.IndexOf(childTransform));
                    }
                }
            }

            if (transform != null)
            {
                var matrix = transform.anim_matrix;
                matrix.values = GetNodeMatrix(node);
                transform.anim_matrix = matrix;
            }

            return transform;
        }

        private static void GetNodeTransform(Node node, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = Vector3.Zero;
            rotation = Quaternion.Identity;
            scale = Vector3.One;

            if (node.Translation != null && node.Translation.Length == 3)
            {
                position = new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
            }

            if (node.Rotation != null && node.Rotation.Length == 4)
            {
                rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            }

            if (node.Scale != null && node.Scale.Length == 3)
            {
                scale = new Vector3(node.Scale[0], node.Scale[1], node.Scale[2]);
            }

            if (node.Matrix != null && node.Matrix.Length == 16)
            {
                Matrix4x4 matrix = new Matrix4x4(
                    node.Matrix[0], node.Matrix[1], node.Matrix[2], node.Matrix[3],
                    node.Matrix[4], node.Matrix[5], node.Matrix[6], node.Matrix[7],
                    node.Matrix[8], node.Matrix[9], node.Matrix[10], node.Matrix[11],
                    node.Matrix[12], node.Matrix[13], node.Matrix[14], node.Matrix[15]
                );

                Matrix4x4.Decompose(matrix, out scale, out rotation, out position);
            }
        }

        private static float[,] GetNodeMatrix(Node node)
        {
            Matrix4x4 matrix;

            if (node.Matrix != null && node.Matrix.Length == 16)
            {
                matrix = new Matrix4x4(
                    node.Matrix[0], node.Matrix[1], node.Matrix[2], node.Matrix[3],
                    node.Matrix[4], node.Matrix[5], node.Matrix[6], node.Matrix[7],
                    node.Matrix[8], node.Matrix[9], node.Matrix[10], node.Matrix[11],
                    node.Matrix[12], node.Matrix[13], node.Matrix[14], node.Matrix[15]
                );
            }
            else
            {
                GetNodeTransform(node, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                matrix =
                    Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(position);
            }

            matrix = Matrix4x4.Transpose(matrix);

            return new float[4, 4]
            {
                { matrix.M11, matrix.M12, matrix.M13, matrix.M14 },
                { matrix.M21, matrix.M22, matrix.M23, matrix.M24 },
                { matrix.M31, matrix.M32, matrix.M33, matrix.M34 },
                { matrix.M41, matrix.M42, matrix.M43, matrix.M44 }
            };
        }

        private static Transform AddEmptyTransform(RDBMesh_t rdbMesh, Node node)
        {
            RRefFrame_t refFrame = new RRefFrame_t();
            rdbMesh.Members.Add(refFrame);
            return refFrame;
        }

        private static Transform AddAttractor(RDBMesh_t rdbMesh, Node node)
        {
            RRefFrame_t refFrame = new RRefFrame_t();
            rdbMesh.Members.Add(refFrame);

            RRefFrameConnector refFrameConnector = new RRefFrameConnector
            {
                name = node.Name ?? "Attractor",
                originator = rdbMesh.Members.IndexOf(refFrame)
            };

            rdbMesh.Members.Add(refFrameConnector);
            refFrame.conn = rdbMesh.Members.IndexOf(refFrameConnector);

            return refFrame;
        }

        private static RTriMesh_t AddTriMesh(RDBMesh_t rdbMesh, Node node, int nodeIndex)
        {
            GetNodeTransform(node, out Vector3 position, out Quaternion rotation, out Vector3 scale);

            RTriMesh_t triMesh = new RTriMesh_t()
            {
                prio = 3,
                local_pos = new AODB.Common.Structs.Vector3(position.X, position.Y, position.Z),
                local_rot = new AODB.Common.Structs.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W),
            };

            rdbMesh.Members.Add(triMesh);

            FAFTriMeshData_t triMeshData = new FAFTriMeshData_t()
            {
                name = node.Name ?? $"Mesh_{nodeIndex}",
            };

            rdbMesh.Members.Add(triMeshData);

            BVolume_t bVolume = new BVolume_t()
            {
                max_pos = new AODB.Common.Structs.Vector3(0.5f, 1.5f, 0.5f),
                min_pos = new AODB.Common.Structs.Vector3(-0.5f, -0.5f, -0.5f),
                sph_pos = new AODB.Common.Structs.Vector3(0, 0, 0),
                sph_radius = 1,
            };

            rdbMesh.Members.Add(bVolume);

            triMesh.data = rdbMesh.Members.IndexOf(triMeshData);
            triMeshData.bvol = rdbMesh.Members.IndexOf(bVolume);

            return triMesh;
        }

        private static List<SimpleMesh> AddMesh(RDBMesh_t rdbMesh, Gltf gltf, Mesh mesh, byte[] bufferData, InfoObject infoObject, Dictionary<int, FAFMaterial_t> materialMap, Dictionary<int, GltfMaterial> mats)
        {
            List<SimpleMesh> meshes = new List<SimpleMesh>();

            foreach (var primitive in mesh.Primitives)
            {
                var positionAccessor = gltf.Accessors[primitive.Attributes["POSITION"]];
                var vertices = ReadVector3Data(gltf, positionAccessor, bufferData);

                Vector3[] normals = null;
                if (primitive.Attributes.TryGetValue("NORMAL", out int normalAccessorIndex))
                {
                    var normalAccessor = gltf.Accessors[normalAccessorIndex];
                    normals = ReadVector3Data(gltf, normalAccessor, bufferData);
                }

                Vector2[] uvs = null;
                if (primitive.Attributes.TryGetValue("TEXCOORD_0", out int uvAccessorIndex))
                {
                    var uvAccessor = gltf.Accessors[uvAccessorIndex];
                    uvs = ReadVector2Data(gltf, uvAccessor, bufferData);
                }

                var indexAccessor = gltf.Accessors[primitive.Indices];
                var indices = ReadIndices(gltf, indexAccessor, bufferData);

                TriList triList = new TriList()
                {
                    triangles = BuildTriangleArray(indices)
                };
                rdbMesh.Members.Add(triList);

                FAFMaterial_t material = null;
                if (primitive.Material.HasValue)
                {
                    int matIndex = primitive.Material.Value;
                    if (!materialMap.TryGetValue(matIndex, out material))
                    {
                        material = AddMaterial(rdbMesh, gltf, matIndex, infoObject, mats);
                        materialMap.Add(matIndex, material);
                    }
                }

                SimpleMesh simpleMesh = new SimpleMesh()
                {
                    name = "",
                    vb_desc = BuildVertexDescriptor(vertices.Length),
                    vertices = BuildVertexArray(vertices, normals, uvs),
                    material = material != null ? rdbMesh.Members.IndexOf(material) : -1,
                    trilist = rdbMesh.Members.IndexOf(triList)
                };

                rdbMesh.Members.Add(simpleMesh);
                meshes.Add(simpleMesh);
            }

            return meshes;
        }

        private static FAFMaterial_t AddMaterial(RDBMesh_t rdbMesh, Gltf gltf, int materialIndex, InfoObject infoObject, Dictionary<int, GltfMaterial> mats)
        {
            var gltfMaterial = gltf.Materials[materialIndex];

            int textureId = 0;
            string texturePath = null;

            if (gltfMaterial.PbrMetallicRoughness?.BaseColorTexture != null)
            {
                int textureIndex = gltfMaterial.PbrMetallicRoughness.BaseColorTexture.Index;
                var texture = gltf.Textures[textureIndex];
                var image = gltf.Images[texture.Source];
                texturePath = image.Uri;
                textureId = GetMatId(texturePath, infoObject);
            }

            AnarchyTexCreator_t texCreator = new AnarchyTexCreator_t()
            {
                type = (uint)ResourceTypeId.Texture,
                inst = (uint)textureId
            };
            rdbMesh.Members.Add(texCreator);

            FAFTexture_t fafTexture = new FAFTexture_t()
            {
                name = "unnamed",
                version = 1,
                creator = rdbMesh.Members.IndexOf(texCreator)
            };
            rdbMesh.Members.Add(fafTexture);

            RDeltaState deltaState = new RDeltaState()
            {
                name = "noname",
                tch_count = 1,
                tch_type = new uint[] { (uint)TextureChannelType.Diffuse },
                tch_text = new int[] { rdbMesh.Members.IndexOf(fafTexture) }
            };

            if (gltfMaterial.PbrMetallicRoughness?.MetallicFactor > 0)
                deltaState.AddRenderStateType(D3DRenderStateType.D3DRS_SPECULARENABLE, 1);

            if (gltfMaterial.DoubleSided == true)
                deltaState.AddRenderStateType(D3DRenderStateType.D3DRS_CULLMODE, (int)D3DCULL.D3DCULL_NONE);

            if (gltfMaterial.AlphaMode == "BLEND" || gltfMaterial.AlphaMode == "MASK")
                deltaState.AddRenderStateType(D3DRenderStateType.D3DRS_ALPHABLENDENABLE, 1);

            rdbMesh.Members.Add(deltaState);

            var baseColor = gltfMaterial.PbrMetallicRoughness?.BaseColorFactor ?? new float[] { 0.8f, 0.8f, 0.8f, 1.0f };

            FAFMaterial_t aoMaterial = new FAFMaterial_t()
            {
                ambi = new AODB.Common.Structs.Color()
                {
                    R = baseColor[0],
                    G = baseColor[1],
                    B = baseColor[2],
                    A = baseColor[3]
                },
                diff = new AODB.Common.Structs.Color()
                {
                    R = baseColor[0],
                    G = baseColor[1],
                    B = baseColor[2],
                    A = baseColor[3]
                },
                emis = new AODB.Common.Structs.Color()
                {
                    R = gltfMaterial.EmissiveFactor?[0] ?? 0,
                    G = gltfMaterial.EmissiveFactor?[1] ?? 0,
                    B = gltfMaterial.EmissiveFactor?[2] ?? 0,
                    A = 1.0f
                },
                name = gltfMaterial.Name ?? "Material",
                opac = baseColor[3],
                shin = (1.0f - (gltfMaterial.PbrMetallicRoughness?.RoughnessFactor ?? 0.5f)) * 100,
                shin_str = gltfMaterial.PbrMetallicRoughness?.MetallicFactor ?? 0,
                spec = new AODB.Common.Structs.Color()
                {
                    R = gltfMaterial.PbrMetallicRoughness?.MetallicFactor ?? 0,
                    G = gltfMaterial.PbrMetallicRoughness?.MetallicFactor ?? 0,
                    B = gltfMaterial.PbrMetallicRoughness?.MetallicFactor ?? 0,
                    A = 1.0f
                },
                version = 1,
                delta_state = rdbMesh.Members.IndexOf(deltaState)
            };

            rdbMesh.Members.Add(aoMaterial);

            if (texturePath != null && !mats.ContainsKey(textureId))
            {
                mats.Add(textureId, new GltfMaterial { FilePath = texturePath });
            }

            return aoMaterial;
        }

        private static Vector3[] ReadVector3Data(Gltf gltf, Accessor accessor, byte[] bufferData)
        {
            var bufferView = gltf.BufferViews[accessor.BufferView];
            int offset = bufferView.ByteOffset + accessor.ByteOffset;
            int count = accessor.Count;

            Vector3[] data = new Vector3[count];

            using (var stream = new MemoryStream(bufferData))
            using (var reader = new BinaryReader(stream))
            {
                stream.Seek(offset, SeekOrigin.Begin);

                for (int i = 0; i < count; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    data[i] = new Vector3(x, y, z);
                }
            }

            return data;
        }

        private static Vector2[] ReadVector2Data(Gltf gltf, Accessor accessor, byte[] bufferData)
        {
            var bufferView = gltf.BufferViews[accessor.BufferView];
            int offset = bufferView.ByteOffset + accessor.ByteOffset;
            int count = accessor.Count;

            Vector2[] data = new Vector2[count];

            using (var stream = new MemoryStream(bufferData))
            using (var reader = new BinaryReader(stream))
            {
                stream.Seek(offset, SeekOrigin.Begin);

                for (int i = 0; i < count; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    data[i] = new Vector2(x, y);
                }
            }

            return data;
        }

        private static ushort[] ReadIndices(Gltf gltf, Accessor accessor, byte[] bufferData)
        {
            var bufferView = gltf.BufferViews[accessor.BufferView];
            int offset = bufferView.ByteOffset + accessor.ByteOffset;
            int count = accessor.Count;

            ushort[] indices = new ushort[count];

            using (var stream = new MemoryStream(bufferData))
            using (var reader = new BinaryReader(stream))
            {
                stream.Seek(offset, SeekOrigin.Begin);

                for (int i = 0; i < count; i++)
                {
                    if (accessor.ComponentType == 5123) // UNSIGNED_SHORT
                    {
                        indices[i] = reader.ReadUInt16();
                    }
                    else if (accessor.ComponentType == 5121) // UNSIGNED_BYTE
                    {
                        indices[i] = reader.ReadByte();
                    }
                    else if (accessor.ComponentType == 5125) // UNSIGNED_INT
                    {
                        indices[i] = (ushort)reader.ReadUInt32();
                    }
                }
            }

            return indices;
        }

        private static byte[] BuildVertexDescriptor(int numVertices)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(0x10);
                writer.Write(0x10000);
                writer.Write(0x112);
                writer.Write(numVertices);

                return stream.ToArray();
            }
        }

        private static byte[] BuildVertexArray(Vector3[] vertices, Vector3[] normals, Vector2[] uvs)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(vertices.Length * 32);

                for (int i = 0; i < vertices.Length; i++)
                {
                    writer.Write(-vertices[i].X); // TODO: Flip due to AO's coordinate system, this works for static meshes, but might cause issues for animated stuff?
                    writer.Write(vertices[i].Y);
                    writer.Write(vertices[i].Z);

                    if (normals != null && i < normals.Length)
                    {
                        writer.Write(normals[i].X);
                        writer.Write(normals[i].Y);
                        writer.Write(normals[i].Z);
                    }
                    else
                    {
                        writer.Write(0f);
                        writer.Write(1f);
                        writer.Write(0f);
                    }

                    if (uvs != null && i < uvs.Length)
                    {
                        writer.Write(uvs[i].X);
                        writer.Write(uvs[i].Y);
                    }
                    else
                    {
                        writer.Write(0f);
                        writer.Write(0f);
                    }
                }

                return stream.ToArray();
            }
        }

        private static byte[] BuildTriangleArray(ushort[] indices)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(indices.Length * 2);

                foreach (ushort index in indices)
                    writer.Write(index);

                return stream.ToArray();
            }
        }

        private static int GetMatId(string texturePath, InfoObject infoObject)
        {
            if (string.IsNullOrEmpty(texturePath))
                return 0;

            if (infoObject.Types[ResourceTypeId.Texture].TryGetKey(texturePath, out int key))
            {
                Console.WriteLine($"Texture {texturePath} found at key {key}");
                return key;
            }

            var keys = infoObject.Types[ResourceTypeId.Texture].Keys.ToArray();

            for (int i = 0; i < keys.Length - 1; i++)
            {
                int nextKey = keys[i] + 1;
                if (nextKey != keys[i + 1])
                {
                    Console.WriteLine($"Adding new InfoObject key. Texture:{nextKey} = {texturePath}");

                    if (infoObject.Types[ResourceTypeId.Texture].ContainsKey(nextKey))
                        continue;

                    infoObject.Types[ResourceTypeId.Texture].Add(nextKey, texturePath);

                    return nextKey;
                }
            }

            return 0;
        }
    }
}