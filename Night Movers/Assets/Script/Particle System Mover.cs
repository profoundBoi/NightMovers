using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BezierPathLine : NetworkBehaviour
{
    [Header("Bezier Points")]
    public Transform point1;
    public Transform point2;
    public Transform point3;
    public Transform medianPoint;

    [Header("Offset")]
    public float heightOffset = 5f;
    public float frontOffset = 0.8f;

    [Header("Line Settings")]
    public int resolution = 30;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = resolution + 1;
    }

    void Update()
    {
        if (point1 == null || point2 == null || point3 == null) return;

        Vector3 p0 = point1.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
        Vector3 p1 = point2.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
        Vector3 p2 = point3.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            lr.SetPosition(i, Bezier(t, p0, p1, p2));
        }
    }

    Vector3 Bezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 +
               2 * (1 - t) * t * p1 +
               Mathf.Pow(t, 2) * p2;
    }
}