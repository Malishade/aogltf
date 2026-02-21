using gltf;
using static AODB.Common.DbClasses.RDBMesh_t;
#nullable disable

namespace aogltf
{
    public static class AOGltfExtensions
    {
        public static void BindMaterials(this Gltf gltf, MaterialBuilder materialBuilder)
        {
            if (materialBuilder.Materials?.Count > 0) gltf.Materials = [.. materialBuilder.Materials];
            if (materialBuilder.Textures?.Count > 0) gltf.Textures = [.. materialBuilder.Textures];
            if (materialBuilder.Images?.Count > 0) gltf.Images = [.. materialBuilder.Images];
            if (materialBuilder.Samplers?.Count > 0) gltf.Samplers = [.. materialBuilder.Samplers];
        }
        public static bool TryGetKey<K, V>(this IDictionary<K, V> instance, V value, out K key)
        {
            foreach (var entry in instance)
            {
                if (entry.Value.Equals(value))
                {
                    key = entry.Key;
                    return true;
                }
            }
            key = default(K);
            return false;
        }

        public static void AddChild(this Transform transform, int idx)
        {
            transform.chld_cnt++;

            if (transform.chld == null)
                transform.chld = [idx];
            else
                transform.chld = [.. transform.chld, idx];
        }

        public static void AddMesh(this FAFTriMeshData_t triMeshData, int idx)
        {
            triMeshData.num_meshes++;
            if (triMeshData.mesh == null)
                triMeshData.mesh = [idx];
            else
                triMeshData.mesh = [.. triMeshData.mesh, idx];
        }

        public static void AddRenderStateType(this RDeltaState deltaState, D3DRenderStateType renderStateType, uint value)
        {

            if (deltaState.rst_type == null)
                deltaState.rst_type = [(uint)renderStateType];
            else
                deltaState.rst_type = [.. deltaState.rst_type, (uint)renderStateType];


            if (deltaState.rst_value == null)
                deltaState.rst_value = [value];
            else
                deltaState.rst_value = [.. deltaState.rst_value, value];

            deltaState.rst_count++;
        }
    }
}