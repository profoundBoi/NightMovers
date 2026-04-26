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

    [Header("References")]
    public PlayerController3D playerController; // Drag your player controller here

    private LineRenderer lr;
    private Transform heldObjectTransform;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = resolution + 1;

        // Auto-find player controller if not assigned
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController3D>();
    }

    void Update()
    {
        // Update held object reference every frame
        UpdateHeldObjectReference();

        // If we have all required points and a held object, draw the line
        if (ShouldDrawLine())
        {
            DrawBezierLine();
        }
        else
        {
            // Hide line when not needed
            if (lr.enabled)
                lr.enabled = false;
        }
    }

    void UpdateHeldObjectReference()
    {
        if (playerController != null && playerController.heldObject != null)
        {
            heldObjectTransform = playerController.heldObject.transform;

            // If point3 isn't assigned and we have a medianPoint, use that
            if (point3 == null && medianPoint != null)
                point3 = medianPoint;
        }
        else
        {
            heldObjectTransform = null;
        }
    }

    bool ShouldDrawLine()
    {
        // Need at least point1, point2, and something to aim at (point3 or medianPoint or held object)
        if (point1 == null || point2 == null)
            return false;

        // Can draw if we have a held object or a valid point3/medianPoint
        if (heldObjectTransform != null)
        {
            point3 = heldObjectTransform; // Dynamically set point3 to held object
            return true;
        }

        return point3 != null;
    }

    void DrawBezierLine()
    {
        if (!lr.enabled)
            lr.enabled = true;

        // Calculate points with offsets
        Vector3 p0 = point1.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
        Vector3 p1 = point2.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
        Vector3 p2 = point3.position; // No offset for the target point (held object)

        // Optional: Add offset to target if needed
        // p2 += Vector3.up * heightOffset + Vector3.forward * frontOffset;

        // Generate bezier curve points
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

    // Optional: Public method to manually set the target
    public void SetTarget(Transform target)
    {
        heldObjectTransform = target;
        if (target != null)
            point3 = target;
    }

    // Visual feedback in editor
    void OnDrawGizmosSelected()
    {
        if (point1 != null && point2 != null && (point3 != null || heldObjectTransform != null))
        {
            Gizmos.color = Color.green;
            Vector3 p0 = point1.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
            Vector3 p1 = point2.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
            Vector3 p2 = heldObjectTransform != null ? heldObjectTransform.position : point3.position;

            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Gizmos.DrawSphere(Bezier(t, p0, p1, p2), 0.1f);
            }
        }
    }
}