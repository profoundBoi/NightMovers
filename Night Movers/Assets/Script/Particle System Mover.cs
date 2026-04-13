using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleWaypoints : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform waypoint1;
    public Transform waypoint2;
    public Transform waypoint3;

    [Header("Movement Settings")]
    public float speed = 2f;
    public float arrivalDistance = 0.05f;
    public bool loop = false;

    [Header("Optional")]
    public bool startAtFirstWaypoint = true;

    [Header("Curved Line Visual")]
    public LineRenderer lineRenderer;
    public int lineSegments = 20;
    public bool showCurvedLine = true;
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.1f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;
    private float[] progress;
    private int waypointCount = 3;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        // Validate waypoints
        if (waypoint1 == null || waypoint2 == null || waypoint3 == null)
        {
            Debug.LogError("ParticleWaypoints: All waypoints must be assigned!", this);
        }

        // Setup line renderer
        if (showCurvedLine)
        {
            SetupLineRenderer();
        }
    }

    void Update()
    {
        // Update the curved line every frame
        if (showCurvedLine && lineRenderer != null && waypoint1 != null && waypoint2 != null && waypoint3 != null)
        {
            DrawCurvedLine();
        }
    }

    void LateUpdate()
    {
        if (waypoint1 == null || waypoint2 == null || waypoint3 == null)
            return;

        int count = ps.particleCount;

        if (count == 0)
            return;

        // Resize arrays if needed
        if (particles == null || particles.Length < count)
        {
            particles = new ParticleSystem.Particle[count];
            progress = new float[count];
        }

        // Get current particles
        ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            // Skip dead particles
            if (particles[i].remainingLifetime <= 0)
            {
                progress[i] = 0f;
                continue;
            }

            // Initialize new particles that just spawned
            if (progress[i] == 0f && particles[i].remainingLifetime > 0)
            {
                if (startAtFirstWaypoint)
                {
                    particles[i].position = waypoint1.position;
                }
            }

            // Determine current target waypoint
            int currentWaypoint = GetCurrentWaypointIndex(progress[i]);
            Vector3 targetPosition = GetWaypointPosition(currentWaypoint);

            // Move toward target
            particles[i].position = Vector3.MoveTowards(
                particles[i].position,
                targetPosition,
                speed * Time.deltaTime
            );

            // Check if reached target waypoint
            if (Vector3.Distance(particles[i].position, targetPosition) < arrivalDistance)
            {
                if (loop)
                {
                    // Loop: go back to waypoint1 after waypoint3
                    if (currentWaypoint >= waypointCount - 1)
                        progress[i] = 0f;
                    else
                        progress[i] += 1f / waypointCount;
                }
                else
                {
                    // Normal: advance to next waypoint or kill
                    if (currentWaypoint >= waypointCount - 1)
                    {
                        // Reached final waypoint, kill particle
                        particles[i].remainingLifetime = 0f;
                        progress[i] = 0f;
                    }
                    else
                    {
                        progress[i] += 1f / waypointCount;
                    }
                }
            }

            // Clamp progress to valid range
            progress[i] = Mathf.Clamp01(progress[i]);
        }

        // Apply updated particles back to the system
        ps.SetParticles(particles, count);
    }

    void SetupLineRenderer()
    {
        // If no LineRenderer is assigned, try to get one from the same GameObject
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();

            // If still null, add a new LineRenderer component
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        // Configure LineRenderer
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Optional: Use a more visible material
        // lineRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
    }

    void DrawCurvedLine()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = lineSegments + 1;

        for (int i = 0; i <= lineSegments; i++)
        {
            float t = i / (float)lineSegments;

            // Quadratic Bezier curve formula
            Vector3 point = Mathf.Pow(1 - t, 2) * waypoint1.position +
                            2 * (1 - t) * t * waypoint2.position +
                            Mathf.Pow(t, 2) * waypoint3.position;

            lineRenderer.SetPosition(i, point);
        }
    }

    int GetCurrentWaypointIndex(float progressValue)
    {
        if (progressValue >= 1f)
            return waypointCount - 1;
        return Mathf.FloorToInt(progressValue * waypointCount);
    }

    Vector3 GetWaypointPosition(int index)
    {
        switch (index)
        {
            case 0: return waypoint1.position;
            case 1: return waypoint2.position;
            case 2: return waypoint3.position;
            default: return waypoint3.position;
        }
    }

    // Optional: Reset all particles to start
    public void ResetAllParticles()
    {
        if (progress != null)
        {
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = 0f;
            }
        }
    }

    // Public method to update line color dynamically
    public void SetLineColor(Color newColor)
    {
        lineColor = newColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }
    }

    // Public method to update line width
    public void SetLineWidth(float newWidth)
    {
        lineWidth = newWidth;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }
    }

    // Visualize waypoints in editor
    void OnDrawGizmos()
    {
        if (waypoint1 != null && waypoint2 != null && waypoint3 != null)
        {
            // Draw waypoints
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(waypoint1.position, 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(waypoint2.position, 0.3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(waypoint3.position, 0.3f);

            // Draw connections
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(waypoint1.position, waypoint2.position);
            Gizmos.DrawLine(waypoint2.position, waypoint3.position);

            // Draw curved path preview
            if (lineSegments > 0)
            {
                Gizmos.color = Color.cyan;
                Vector3 prevPoint = waypoint1.position;
                for (int i = 1; i <= lineSegments; i++)
                {
                    float t = i / (float)lineSegments;
                    Vector3 point = Mathf.Pow(1 - t, 2) * waypoint1.position +
                                    2 * (1 - t) * t * waypoint2.position +
                                    Mathf.Pow(t, 2) * waypoint3.position;
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }
        }
    }
}