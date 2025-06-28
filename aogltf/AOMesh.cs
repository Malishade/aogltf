using AODB.Common.DbClasses;
using aogltf;
using System.Numerics;
using static AODB.Common.DbClasses.RDBMesh_t;
using static AODB.Common.DbClasses.RDBMesh_t.FAFAnim_t;
using static AODB.Common.RDBObjects.RDBCatMesh;

namespace AODB.Encoding
{
    internal class AbiffExporter
    {
        private RDBMesh_t _rdbMesh;
        private Dictionary<string, int> _nameCache = new Dictionary<string, int>();

        public AbiffExporter(RDBMesh_t rdbMesh)
        {
            _rdbMesh = rdbMesh;
            BuildScene();
        }

        private string GetUniqueNodeName(string name)
        {
            if (!_nameCache.ContainsKey(name))
            {
                _nameCache[name] = 1;
                return name;
            }

            return $"{name}.{(_nameCache[name]++).ToString("D3")}";
        }

        public ObjectNode BuildScene()
        {
            Dictionary<int, ObjectNode> sceneObjects = new Dictionary<int, ObjectNode>();

            for (int i = 0; i < _rdbMesh.Members.Count; i++)
            {
                ObjectNode sceneObject;
                switch (_rdbMesh.Members[i])
                {
                    case RTriMesh_t triMeshClass:
                        sceneObject = BuildTriMesh(triMeshClass);
                        break;
                    case RRefFrame_t refFrameClass:
                        sceneObject = BuildRefFrame(refFrameClass);
                        break;
                    default:
                        continue;
                }

                if (_rdbMesh.Members[i] is Transform transform && transform.conn != -1)
                    sceneObject.Name = ((RRefFrameConnector)_rdbMesh.Members[transform.conn]).name;

                sceneObjects.Add(i, sceneObject);
            }

            List<Transform> transforms = _rdbMesh.GetMembers<Transform>();
       
            for (int i = 0; i < transforms.Count; i++)
            {
                if (transforms[i].chld_cnt == 0)
                    continue;

                if (!sceneObjects.TryGetValue(i, out ObjectNode? parent))
                    continue;

                foreach (int childIdx in transforms[i].chld)
                {
                    if (sceneObjects.TryGetValue(childIdx, out ObjectNode? childNode))
                    {
                        parent.Children.Add(childNode);
                    }
                }
            }

            var rootCandidates = sceneObjects.Values
                .Where(obj => !transforms.Any(t => t.chld != null && t.chld.Contains(sceneObjects.First(kvp => kvp.Value == obj).Key))).ToList();

            if (rootCandidates.Count == 1)
            {
                return rootCandidates.First();
            }
            else
            {
                return new ObjectNode
                {
                    Name = "Scene Root",
                    Children = rootCandidates.ToList()
                };
            }
        }


        private ObjectNode BuildRefFrame(RRefFrame_t refFrameClass)
        {
            ObjectNode refFrame = new ObjectNode();
            refFrame.Name = GetUniqueNodeName("RRefFrame");
            //Vector3 scale = new Vector3(refFrameClass.scale, refFrameClass.scale, refFrameClass.scale);
            //Quaternion rotation = refFrameClass.local_rot.ToNumerics();
            //Vector3 position = refFrameClass.local_pos.ToNumerics();

            if (refFrameClass.anim_matrix.values != null)
            {
                //This will definitely not work for all cases, but it should be fine for most of the meshes
                var matrix = refFrameClass.anim_matrix.ToNumerics() * ((Transform)_rdbMesh.Members[0]).anim_matrix.ToNumerics();
                Matrix4x4.Decompose(matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

                refFrame.Translation = translation;
                refFrame.Rotation = rotation;
                refFrame.Scale = scale;
            }

            return refFrame;
        }

        private ObjectNode BuildTriMesh(RTriMesh_t triMeshClass)
        {
            ObjectNode node = new ObjectNode();

            if (_rdbMesh.Members[triMeshClass.data] is not FAFTriMeshData_t triMeshDataClass)
            {
                Console.WriteLine("Null mesh");
                return node;
            }

            node.Name = GetUniqueNodeName(triMeshDataClass.name);
            var hasAnims = false;

            BuildMeshes(node, triMeshClass, triMeshDataClass, hasAnims);

            node.Translation = triMeshClass.local_pos.ToNumerics();
            node.Rotation = triMeshClass.local_rot.ToNumerics();
            node.Scale = new Vector3(triMeshClass.scale, triMeshClass.scale, triMeshClass.scale);

            return node;
        }

        private void BuildMeshes(ObjectNode node, RTriMesh_t triMeshClass, FAFTriMeshData_t triMeshDataClass, bool hasAnims)
        {
            foreach (int meshIdx in triMeshDataClass.mesh)
            {
                if (_rdbMesh.Members[meshIdx] is not SimpleMesh simpleMeshClass || _rdbMesh.Members[simpleMeshClass.trilist] is not TriList triListClass)
                {
                    Console.WriteLine("Null mesh");
                    continue;
                }

                Matrix4x4 transformMatrix = Matrix4x4.Identity;

                if (!hasAnims)
                {
                    Quaternion rot = triMeshDataClass.anim_rot.ToNumerics();
                    Vector3 pos = triMeshDataClass.anim_pos.ToNumerics();

                    var rotationMatrix = Matrix4x4.CreateFromQuaternion(rot);
                    var translationMatrix = Matrix4x4.CreateTranslation(pos);

                    transformMatrix = rotationMatrix * translationMatrix;
                }

                var vertices = simpleMeshClass.Vertices.Select(x => transformMatrix.MultiplyPoint(x.Position.ToNumerics())).ToArray();
                var indices = triListClass.Triangles.Select(i => checked((ushort)i)).ToArray();
                node.MeshData = new StaticMeshData(vertices, indices);
            }
        }
    }
}
