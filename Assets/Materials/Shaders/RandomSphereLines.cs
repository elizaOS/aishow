using UnityEngine;

public class RandomSphereLines : MonoBehaviour
{
    public int numberOfLines = 50;        // Number of lines to draw
    public float lineWidth = 0.01f;      // Width of the lines
    public Color lineColor = Color.cyan; // Line color
    public Material lineMaterial;        // Material for rendering lines

    private Vector3[] randomPoints;      // Randomly generated points

    void Start()
    {
        // Generate random points on the surface of the sphere
        randomPoints = GenerateRandomPoints(numberOfLines * 2);
    }

    void OnRenderObject()
    {
        if (lineMaterial == null) return;

        // Apply the material
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        // Draw lines between random points
        for (int i = 0; i < numberOfLines; i++)
        {
            Vector3 p1 = randomPoints[i * 2];
            Vector3 p2 = randomPoints[i * 2 + 1];

            GL.Vertex(transform.position + transform.TransformDirection(p1));
            GL.Vertex(transform.position + transform.TransformDirection(p2));
        }

        GL.End();
        GL.PopMatrix();
    }

    Vector3[] GenerateRandomPoints(int count)
    {
        Vector3[] points = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            // Random point on a unit sphere
            points[i] = Random.onUnitSphere;
        }

        return points;
    }
}
