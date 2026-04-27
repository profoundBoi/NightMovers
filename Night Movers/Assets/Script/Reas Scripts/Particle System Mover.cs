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
    public PlayerController3D playerController;

    private LineRenderer lr;
    private Transform heldObjectTransform;
    private Vector3[] linePositions;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = resolution + 1;
        linePositions = new Vector3[resolution + 1];

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController3D>();
    }

    void Update()
    {
        // Only the owner calculates and broadcasts the line
        if (!IsOwner) return;

        UpdateHeldObjectReference();

        if (ShouldDrawLine())
        {
            CalculateLine();
            // Send positions to all clients
            UpdateLineClientRpc(linePositions, true);
        }
        else
        {
            UpdateLineClientRpc(linePositions, false);
        }
    }

    void CalculateLine()
    {
        Vector3 p0 = point1.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
        Vector3 p1 = point2.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
        Vector3 p2 = point3.position;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            linePositions[i] = Bezier(t, p0, p1, p2);
        }
    }

    [ClientRpc]
    void UpdateLineClientRpc(Vector3[] positions, bool visible)
    {
        lr.enabled = visible;
        if (visible)
        {
            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
        }
    }

    void UpdateHeldObjectReference()
    {
        if (playerController != null && playerController.heldObject != null)
        {
            heldObjectTransform = playerController.heldObject.transform;
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
        if (point1 == null || point2 == null) return false;

        if (heldObjectTransform != null)
        {
            point3 = heldObjectTransform;
            return true;
        }

        return point3 != null;
    }

    Vector3 Bezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 +
               2 * (1 - t) * t * p1 +
               Mathf.Pow(t, 2) * p2;
    }

    public void SetTarget(Transform target)
    {
        heldObjectTransform = target;
        if (target != null)
            point3 = target;
    }

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