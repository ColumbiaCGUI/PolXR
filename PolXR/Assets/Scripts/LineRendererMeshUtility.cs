using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LineRendererMeshUtility
{
    public static Mesh CreateMeshFromLineRenderer(LineRenderer lineRenderer, float thickness = 0.1f)
    {
        Mesh mesh = new Mesh();

        int positionCount = lineRenderer.positionCount;
        if (positionCount < 2)
        {
            Debug.LogWarning("LineRenderer must have at least 2 points to create a mesh.");
            return mesh;
        }

        Vector3[] positions = new Vector3[positionCount];
        lineRenderer.GetPositions(positions);

        // Create vertices for the mesh
        Vector3[] vertices = new Vector3[positionCount * 2];
        int[] triangles = new int[(positionCount - 1) * 6]; // 2 triangles per segment

        for (int i = 0; i < positionCount; i++)
        {
            Vector3 offset = Vector3.Cross(Vector3.forward, (i < positionCount - 1 ? positions[i + 1] - positions[i] : positions[i] - positions[i - 1]).normalized) * thickness;

            vertices[i * 2] = positions[i] - offset; // Bottom vertex
            vertices[i * 2 + 1] = positions[i] + offset; // Top vertex

            if (i < positionCount - 1)
            {
                int index = i * 6;
                triangles[index] = i * 2;
                triangles[index + 1] = i * 2 + 1;
                triangles[index + 2] = i * 2 + 2;

                triangles[index + 3] = i * 2 + 2;
                triangles[index + 4] = i * 2 + 1;
                triangles[index + 5] = i * 2 + 3;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

