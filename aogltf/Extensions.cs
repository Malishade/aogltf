using System.Numerics;

namespace aogltf
{
    internal static class Extensions
    {
        internal static float[] ToArray(this Vector3 v) => [v.X, v.Y, v.Z];
       
        internal static float[] ToArray(this Quaternion q) => [q.X, q.Y, q.Z, q.W];

        internal static Vector3 ToNumerics(this AODB.Common.Structs.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
       
        internal static Quaternion ToNumerics(this AODB.Common.Structs.Quaternion q) => new Quaternion(q.X, q.Y, q.Z, q.W);
       
        internal static Matrix4x4 ToNumerics(this AODB.Common.Structs.Matrix m)
        {
            return new Matrix4x4(
                m.values[0, 0], m.values[0, 1], m.values[0, 2], m.values[0, 3],
                m.values[1, 0], m.values[1, 1], m.values[1, 2], m.values[1, 3],
                m.values[2, 0], m.values[2, 1], m.values[2, 2], m.values[2, 3],
                m.values[3, 0], m.values[3, 1], m.values[3, 2], m.values[3, 3]
            );
        }

        internal static Vector3 MultiplyPoint(this Matrix4x4 matrix, Vector3 point)
        {
            return new Vector3(
                point.X * matrix.M11 + point.Y * matrix.M21 + point.Z * matrix.M31 + matrix.M41,
                point.X * matrix.M12 + point.Y * matrix.M22 + point.Z * matrix.M32 + matrix.M42,
                point.X * matrix.M13 + point.Y * matrix.M23 + point.Z * matrix.M33 + matrix.M43
            );
        }
    }
}