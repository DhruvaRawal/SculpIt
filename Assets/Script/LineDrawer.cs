using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float lineWidth = 0.1f; // Thickness of the tube
    private List<Vector3> points = new List<Vector3>();
    private Vector3 currentPosition = Vector3.zero;
    private Vector3 lastPosition = Vector3.zero;
    private bool drawing = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("No LineRenderer found! Add one to the GameObject.");
            return;
        }

        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        points.Add(currentPosition);
        UpdateLine();
    }

    void Update()
    {
        // Get controller position
        Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

        // Start/Stop Drawing with 'A' Button
        if (OVRInput.GetDown(OVRInput.Button.One)) // A Button on Right Controller
        {
            drawing = !drawing; // Toggle drawing mode
            Debug.Log("Drawing Mode: " + drawing);
        }

        // Export with 'B' Button
        if (OVRInput.GetDown(OVRInput.Button.Two)) // B Button on Right Controller
        {
            ExportMesh();
        }

        // Move Drawing Position Based on Controller
        if (drawing)
        {
            currentPosition = controllerPosition;

            if (currentPosition != lastPosition)
            {
                AddPoint();
                lastPosition = currentPosition;
            }
        }
    }

    void AddPoint()
    {
        points.Add(currentPosition);
        UpdateLine();
    }

    void UpdateLine()
    {
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    void ExportMesh()
    {
        Mesh mesh = ConvertToMesh();
        if (mesh != null)
        {
            SaveMeshAsOBJ(mesh, "ExportedMesh");
        }
    }

    Mesh ConvertToMesh()
    {
        if (points.Count < 2)
        {
            Debug.LogError("Not enough points to form a 3D object.");
            return null;
        }

        GameObject meshObject = new GameObject("GeneratedMesh");
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Standard"));

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float radius = lineWidth / 2f;
        int segments = 20;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward = Vector3.zero;

            if (i > 0)
                forward = (points[i] - points[i - 1]).normalized;
            else if (i < points.Count - 1)
                forward = (points[i + 1] - points[i]).normalized;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;

            if (forward == Vector3.up || forward == Vector3.down)
            {
                right = Vector3.right;
                up = Vector3.forward;
            }

            for (int j = 0; j < segments; j++)
            {
                float angle = (j / (float)segments) * Mathf.PI * 2;
                Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                vertices.Add(points[i] + offset);
            }
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int nextJ = (j + 1) % segments;
                int index0 = i * segments + j;
                int index1 = i * segments + nextJ;
                int index2 = (i + 1) * segments + j;
                int index3 = (i + 1) * segments + nextJ;

                triangles.Add(index0);
                triangles.Add(index2);
                triangles.Add(index1);

                triangles.Add(index1);
                triangles.Add(index2);
                triangles.Add(index3);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        return mesh;
    }

    void SaveMeshAsOBJ(Mesh mesh, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".obj");
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("# Exported from Unity");

            // Write vertices
            foreach (Vector3 vertex in mesh.vertices)
            {
                writer.WriteLine($"v {vertex.x} {vertex.y} {vertex.z}");
            }

            // Write faces (OBJ indices start from 1, so we add +1)
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int v1 = mesh.triangles[i] + 1;
                int v2 = mesh.triangles[i + 1] + 1;
                int v3 = mesh.triangles[i + 2] + 1;
                writer.WriteLine($"f {v1} {v2} {v3}");
            }
        }

        Debug.Log("Mesh saved to: " + path);
    }
}