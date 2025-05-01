using UnityEngine;

namespace LinePicking
{
    public static class GeometryUtils
    {
        public static float GetTriangleArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 v1 = p1 - p3;
            Vector2 v2 = p2 - p3;
            return (v1.x * v2.y - v1.y * v2.x) / 2;
        }

        public static Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            // Check if point is in vertex region outside A
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = point - a;

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;

            // Check if point is in vertex region outside B
            Vector3 bp = point - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;

            // Check if point is in edge region of AB, if so return projection of point onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab;
            }

            // Check if point is in vertex region outside C
            Vector3 cp = point - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;

            // Check if point is in edge region of AC, if so return projection of point onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac;
            }

            // Check if point is in edge region of BC, if so return projection of point onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }

            // Point is inside the triangle, compute barycentric coordinates
            float denom = 1.0f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            return a + ab * v2 + ac * w2;
        }

        public static Vector3 Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = b - a;
            Vector3 v1 = c - a;
            Vector3 v2 = p - a;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);

            float denominator = d00 * d11 - d01 * d01;

            // Check for near-zero denominator
            if (Mathf.Abs(denominator) < 1e-6f)
            {
                Debug.LogWarning($"Barycentric: Near-zero denominator ({denominator}). This may cause numerical instability.");
                Debug.LogWarning($"Barycentric: Input points - P:{p}, A:{a}, B:{b}, C:{c}");
                Debug.LogWarning($"Barycentric: Dot products - d00:{d00}, d01:{d01}, d11:{d11}, d20:{d20}, d21:{d21}");
                return Vector3.zero;
            }

            float v = (d11 * d20 - d01 * d21) / denominator;
            float w = (d00 * d21 - d01 * d20) / denominator;
            float u = 1.0f - v - w;

            // Check if barycentric coordinates are valid (sum to 1 and all between 0 and 1)
            if (Mathf.Abs(u + v + w - 1.0f) > 1e-4f || u < -1e-4f || v < -1e-4f || w < -1e-4f)
            {
                Debug.LogWarning($"Barycentric: Invalid coordinates (u:{u}, v:{v}, w:{w})");
                Debug.LogWarning($"Barycentric: Input points - P:{p}, A:{a}, B:{b}, C:{c}");
            }

            return new Vector3(u, v, w);
        }
    }
}