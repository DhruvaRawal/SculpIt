using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    [Header("Settings")]
    public float lineWidth = 0.02f;
    public float minDistance = 0.01f;

    [Header("References")]
    public Transform rightHandAnchor;
    public GameObject linePrefab;

    private List<List<Vector3>> allLines = new();
    private List<Vector3> currentLine = null;
    private LineRenderer activeRenderer = null;
    private bool drawing = false;

    void Start()
    {
        if (rightHandAnchor == null)
        {
            rightHandAnchor = GameObject.Find("RightHandAnchor")?.transform;
            if (rightHandAnchor == null)
            {
                Debug.LogError("RightHandAnchor not found. Assign it manually.");
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        // Toggle drawing with A
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            drawing = !drawing;
            Debug.Log("Drawing mode: " + drawing);

            if (drawing)
                StartNewLine();
            else
                FinishCurrentLine();
        }

        // Export with B
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            ExportAllMeshes();
        }

        if (drawing && rightHandAnchor != null && activeRenderer != null)
        {
            Vector3 pos = rightHandAnchor.position;

            if (currentLine.Count == 0 || Vector3.Distance(currentLine[^1], pos) > minDistance)
            {
                currentLine.Add(pos);
                UpdateActiveLine();
            }
        }
    }

    void StartNewLine()
    {
        if (linePrefab == null)
        {
            Debug.LogError("Line Prefab not assigned!");
            return;
        }

        GameObject newLine = Instantiate(linePrefab, transform);
        activeRenderer = newLine.GetComponent<LineRenderer>();

        if (activeRenderer == null)
        {
            Debug.LogError("Line prefab must have a LineRenderer!");
            return;
        }

        activeRenderer.positionCount = 0;
        activeRenderer.startWidth = lineWidth;
        activeRenderer.endWidth = lineWidth;
        activeRenderer.useWorldSpace = true;

        currentLine = new List<Vector3>();
        allLines.Add(currentLine);
    }

    void FinishCurrentLine()
    {
        activeRenderer = null;
        currentLine = null;
    }

    void UpdateActiveLine()
    {
        if (activeRenderer != null && currentLine != null)
        {
            activeRenderer.positionCount = currentLine.Count;
            activeRenderer.SetPositions(currentLine.ToArray());
        }
    }

    // ---------- EXPORT ----------
    void ExportAllMeshes()
    {
        if (allLines.Count == 0)
        {
            Debug.LogWarning("No lines to export!");
            return;
        }

        List<Mesh> meshes = new();
        foreach (var line in allLines)
        {
            if (line.Count >= 2)
                meshes.Add(ConvertLineToMesh(line));
        }

        Mesh combined = CombineMeshes(meshes);
        SaveMeshAsOBJ(combined, "ExportedDrawing");

        Debug.Log("Export complete! All lines merged into one OBJ file.");
    }

    Mesh ConvertLineToMesh(List<Vector3> points)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new();
        List<int> triangles = new();

        float radius = lineWidth / 2f;
        int segments = 12;

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

        return mesh;
    }

    Mesh CombineMeshes(List<Mesh> meshes)
    {
        List<CombineInstance> combine = new();

        foreach (var m in meshes)
        {
            CombineInstance ci = new();
            ci.mesh = m;
            ci.transform = Matrix4x4.identity;
            combine.Add(ci);
        }

        Mesh combined = new Mesh();
        combined.CombineMeshes(combine.ToArray(), true, false);
        return combined;
    }

    void SaveMeshAsOBJ(Mesh mesh, string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".obj");

        using (StreamWriter writer = new StreamWriter(path, false, System.Text.Encoding.ASCII))
        {
            writer.WriteLine("# Exported from Unity LineDrawer");

            foreach (Vector3 v in mesh.vertices)
                writer.WriteLine($"v {v.x} {v.y} {v.z}");

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                writer.WriteLine($"f {triangles[i] + 1} {triangles[i + 1] + 1} {triangles[i + 2] + 1}");
            }
        }

        Debug.Log($"Saved OBJ to: {path}");
    }
}
