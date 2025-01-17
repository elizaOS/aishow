using System.Collections.Generic;
using UnityEngine;

public class MagneticFieldLines : MonoBehaviour
{
    public int numberOfFieldLines = 50;      // Number of magnetic lines
    public int segmentsPerLine = 50;         // Number of segments per curve (increase for smoother lines)
    public float curveHeight = 0.5f;         // How far the curve bows outward
    public float speed = 0.1f;               // Speed of the curve expansion
    public Material lineMaterial;            // Material for the lines
    public float lineWidth = 0.01f;          // Width of the lines
    public float scaleFactor = 1f;           // Overall scaling of the magnetic field lines

    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    private int prevNumberOfFieldLines;
    private int prevSegmentsPerLine;
    private float prevCurveHeight;
    private float prevScaleFactor;

    void Start()
    {
        prevNumberOfFieldLines = numberOfFieldLines;
        prevSegmentsPerLine = segmentsPerLine;
        prevCurveHeight = curveHeight;
        prevScaleFactor = scaleFactor;
        GenerateFieldLines();  // Generate field lines once when the game starts
    }

    void Update()
    {
        // If any of the parameters change, regenerate the lines
        if (numberOfFieldLines != prevNumberOfFieldLines ||
            segmentsPerLine != prevSegmentsPerLine ||
            curveHeight != prevCurveHeight ||
            scaleFactor != prevScaleFactor)  // Check if scale factor changes
        {
            GenerateFieldLines();
        }

        // Update previous values for next frame comparison
        prevNumberOfFieldLines = numberOfFieldLines;
        prevSegmentsPerLine = segmentsPerLine;
        prevCurveHeight = curveHeight;
        prevScaleFactor = scaleFactor;
    }

    void GenerateFieldLines()
    {
        // Clear old lines if regenerating
        foreach (var line in lineRenderers)
        {
            Destroy(line.gameObject);
        }
        lineRenderers.Clear();

        // Create new field lines
        for (int i = 0; i < numberOfFieldLines; i++)
        {
            // Generate random start and end points on the sphere in local space
            Vector3 startPoint = Random.onUnitSphere;
            Vector3 endPoint = Random.onUnitSphere;

            // Create a new line renderer
            GameObject lineObj = new GameObject("MagneticFieldLine_" + i);
            lineObj.transform.SetParent(transform); // Set the parent of the line object
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

            // Set line properties
            lineRenderer.positionCount = segmentsPerLine + 1;  // +1 because we include the start and end points
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = lineWidth;  // Dynamic width
            lineRenderer.endWidth = lineWidth;    // Dynamic width
            lineRenderer.useWorldSpace = false;  // Use local space for positioning
            lineRenderer.numCapVertices = 10;  // Smooth line ends
            lineRenderer.numCornerVertices = 10; // Smooth line joints

            // Generate curve points
            Vector3[] curvePoints = GenerateCurve(startPoint, endPoint, curveHeight);
            lineRenderer.SetPositions(curvePoints);

            // Add to list for cleanup
            lineRenderers.Add(lineRenderer);
        }
    }

    Vector3[] GenerateCurve(Vector3 start, Vector3 end, float height)
    {
        Vector3[] points = new Vector3[segmentsPerLine + 1];

        // Midpoint for bowing the curve outward
        Vector3 midPoint = (start + end) / 2;
        midPoint += (midPoint.normalized * height); // Push outward

        // Generate curve using linear interpolation
        for (int i = 0; i <= segmentsPerLine; i++)
        {
            float t = i / (float)segmentsPerLine;
            Vector3 point = Mathf.Pow(1 - t, 2) * start +  // Start point
                            2 * (1 - t) * t * midPoint +  // Control point
                            Mathf.Pow(t, 2) * end;       // End point
            points[i] = point; // Keep points in local space first
        }

        // Apply scaling and rotation relative to the parent
        ApplyTransformations(ref points);

        return points;
    }

    void ApplyTransformations(ref Vector3[] points)
    {
        // Apply scaling and rotation relative to the parent
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = Vector3.Scale(points[i], Vector3.one * scaleFactor);  // Apply scaling in local space
            points[i] = transform.TransformPoint(points[i]);  // Apply the parent object's position and rotation
        }
    }
}
