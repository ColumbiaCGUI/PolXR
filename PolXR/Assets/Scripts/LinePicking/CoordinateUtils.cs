using UnityEngine;

namespace LinePicking
{
    public static class CoordinateUtils
    {
        /// Converts a single UV coordinate to a world coordinate
        public static Vector3 UvTo3D(Vector2 uv, Mesh mesh, Transform transform)
        {
            int[] tris = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector2 u1 = uvs[tris[i]];
                Vector2 u2 = uvs[tris[i + 1]];
                Vector2 u3 = uvs[tris[i + 2]];

                // Calculate triangle area - if zero, skip it
                float a = GeometryUtils.GetTriangleArea(u1, u2, u3);
                if (a == 0)
                    continue;

                // Calculate barycentric coordinates of u1, u2, and u3
                // If any is negative, point is outside the triangle: skip it
                float a1 = GeometryUtils.GetTriangleArea(u2, u3, uv) / a;
                if (a1 < 0)
                    continue;

                float a2 = GeometryUtils.GetTriangleArea(u3, u1, uv) / a;
                if (a2 < 0)
                    continue;

                float a3 = GeometryUtils.GetTriangleArea(u1, u2, uv) / a;
                if (a3 < 0)
                    continue;

                // Point inside the triangle - find mesh position by interpolation
                Vector3 p3D = a1 * verts[tris[i]] + a2 * verts[tris[i + 1]] + a3 * verts[tris[i + 2]];

                // Return it in world coordinates
                return transform.TransformPoint(p3D);
            }

            // Point outside any UV triangle
            return Vector3.zero;
        }

        /// Converts a 3D world coordinate to UV coordinates on a mesh
        public static Vector2 WorldToUV(Vector3 worldPoint, Mesh mesh, Transform transform)
        {
            int[] tris = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v1 = verts[tris[i]];
                Vector3 v2 = verts[tris[i + 1]];
                Vector3 v3 = verts[tris[i + 2]];

                // Calculate triangle area in 3D space
                Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                float area = Vector3.Dot(Vector3.Cross(v2 - v1, v3 - v1), normal) / 2f;
                if (Mathf.Abs(area) < 1e-6f)
                    continue;

                // Calculate barycentric coordinates in 3D space
                float a1 = Vector3.Dot(Vector3.Cross(v2 - worldPoint, v3 - worldPoint), normal) / (2f * area);
                if (a1 < -1e-6f || a1 > 1f + 1e-6f)
                    continue;

                float a2 = Vector3.Dot(Vector3.Cross(v3 - worldPoint, v1 - worldPoint), normal) / (2f * area);
                if (a2 < -1e-6f || a2 > 1f + 1e-6f)
                    continue;

                float a3 = Vector3.Dot(Vector3.Cross(v1 - worldPoint, v2 - worldPoint), normal) / (2f * area);
                if (a3 < -1e-6f || a3 > 1f + 1e-6f)
                    continue;

                // Point is inside this triangle - use same barycentric coordinates to interpolate UV
                Vector2 uv1 = uvs[tris[i]];
                Vector2 uv2 = uvs[tris[i + 1]];
                Vector2 uv3 = uvs[tris[i + 2]];

                return a1 * uv1 + a2 * uv2 + a3 * uv3;
            }

            // Point not found in any triangle
            return Vector2.zero;
        }
    }
}