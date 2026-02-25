using System.Numerics;

namespace aogltf
{
    [Flags]
    public enum ExportMirror
    {
        NoMirror = 0,
        MirrorX = 1 << 0,
        MirrorY = 1 << 1,
        MirrorZ = 1 << 2,
    }

    public static class SceneTransformHelper
    {
        public static void Apply(SceneData sceneData, ExportMirror transforms)
        {
            if (transforms.HasFlag(ExportMirror.MirrorX)) ApplyAxisMirror(sceneData, x: true, y: false, z: false);
            if (transforms.HasFlag(ExportMirror.MirrorY)) ApplyAxisMirror(sceneData, x: false, y: true, z: false);
            if (transforms.HasFlag(ExportMirror.MirrorZ)) ApplyAxisMirror(sceneData, x: false, y: false, z: true);
        }

        private static void ApplyAxisMirror(SceneData sceneData, bool x, bool y, bool z)
        {
            foreach (var mesh in sceneData.Meshes)
                foreach (var prim in mesh.Primitives)
                    MirrorPrimitive(prim, x, y, z);

            foreach (var node in sceneData.Nodes)
                MirrorNode(node, x, y, z);

            foreach (var skin in sceneData.Skins)
                for (int i = 0; i < skin.InverseBindMatrices.Length; i++)
                    skin.InverseBindMatrices[i] = MirrorMatrix(skin.InverseBindMatrices[i], x, y, z);

            foreach (var anim in sceneData.Animations)
                foreach (var channel in anim.Channels)
                    MirrorAnimationChannel(channel, x, y, z);
        }

        private static void MirrorPrimitive(PrimitiveData prim, bool x, bool y, bool z)
        {
            for (int i = 0; i < prim.Vertices.Length; i++)
                prim.Vertices[i] = MirrorVector3(prim.Vertices[i], x, y, z);

            for (int i = 0; i < prim.Normals.Length; i++)
                prim.Normals[i] = MirrorVector3(prim.Normals[i], x, y, z);

            int mirroredAxes = (x ? 1 : 0) + (y ? 1 : 0) + (z ? 1 : 0);
            if ((mirroredAxes % 2) == 1)
                ReverseTriangleWinding(prim.Indices);

            prim.Bounds = Bounds.FromVertices(prim.Vertices);
        }

        private static void ReverseTriangleWinding(ushort[] indices)
        {
            for (int i = 0; i + 2 < indices.Length; i += 3)
                (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
        }

        private static void MirrorNode(NodeData node, bool x, bool y, bool z)
        {
            if (node.Translation.HasValue)
                node.Translation = MirrorVector3(node.Translation.Value, x, y, z);

            if (node.Rotation.HasValue)
                node.Rotation = MirrorQuaternion(node.Rotation.Value, x, y, z);
        }

        private static void MirrorAnimationChannel(AnimationChannelData channel, bool x, bool y, bool z)
        {
            switch (channel.Path)
            {
                case "translation":
                    foreach (var kf in channel.Keyframes)
                    {
                        if (kf.Value.Length < 3) continue;
                        if (x) kf.Value[0] = -kf.Value[0];
                        if (y) kf.Value[1] = -kf.Value[1];
                        if (z) kf.Value[2] = -kf.Value[2];
                    }
                    break;

                case "rotation":
                    foreach (var kf in channel.Keyframes)
                    {
                        if (kf.Value.Length < 4) continue;
                        var q = MirrorQuaternion(new Quaternion(kf.Value[0], kf.Value[1], kf.Value[2], kf.Value[3]), x, y, z);
                        kf.Value[0] = q.X;
                        kf.Value[1] = q.Y;
                        kf.Value[2] = q.Z;
                        kf.Value[3] = q.W;
                    }
                    break;
            }
        }

        private static Matrix4x4 MirrorMatrix(Matrix4x4 m, bool x, bool y, bool z)
        {
            float sx = x ? -1f : 1f;
            float sy = y ? -1f : 1f;
            float sz = z ? -1f : 1f;
            const float sw = 1f;

            float[] s = { sx, sy, sz, sw };

            return new Matrix4x4(
                m.M11 * s[0] * s[0], m.M12 * s[0] * s[1], m.M13 * s[0] * s[2], m.M14 * s[0] * s[3],
                m.M21 * s[1] * s[0], m.M22 * s[1] * s[1], m.M23 * s[1] * s[2], m.M24 * s[1] * s[3],
                m.M31 * s[2] * s[0], m.M32 * s[2] * s[1], m.M33 * s[2] * s[2], m.M34 * s[2] * s[3],
                m.M41 * s[3] * s[0], m.M42 * s[3] * s[1], m.M43 * s[3] * s[2], m.M44 * s[3] * s[3]
            );
        }

        private static Vector3 MirrorVector3(Vector3 v, bool x, bool y, bool z)
            => new(x ? -v.X : v.X, y ? -v.Y : v.Y, z ? -v.Z : v.Z);

        private static Quaternion MirrorQuaternion(Quaternion q, bool x, bool y, bool z)
        {
            float sx = q.X, sy = q.Y, sz = q.Z, sw = q.W;
            if (x) { sy = -sy; sz = -sz; }
            if (y) { sx = -sx; sz = -sz; }
            if (z) { sx = -sx; sy = -sy; }
            return new Quaternion(sx, sy, sz, sw);
        }
    }
}