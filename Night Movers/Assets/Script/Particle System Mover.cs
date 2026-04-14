using UnityEngine;

public class ParticleBezierPath : MonoBehaviour
{
    public ParticleSystem ps;

    [Header("Bezier Points")]
    public Transform point1;
    public Transform point2;
    public Transform point3;

    [Header("Offset")]
    public float heightOffset = 5f;
    public float frontOffset = 0.8f;

    [Header("Speed Settings")]
    public float speed = 1f; // 👈 MAIN SPEED CONTROL

    private ParticleSystem.Particle[] particles;
    private float[] progress; // stores t for each particle

    void Update()
    {
        if (particles == null || particles.Length < ps.main.maxParticles)
        {
            particles = new ParticleSystem.Particle[ps.main.maxParticles];
            progress = new float[ps.main.maxParticles];
        }

        int count = ps.GetParticles(particles);

        for (int i = 0; i < count; i++)
        {
            // Increase progress manually
            progress[i] += Time.deltaTime * speed;

            // Clamp so it doesn't go past the end
            progress[i] = Mathf.Clamp01(progress[i]);

            float t = progress[i];

            Vector3 p0 = point1.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
            Vector3 p1 = point2.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;
            Vector3 p2 = point3.position + Vector3.up * heightOffset + Vector3.forward * frontOffset;

            Vector3 pos = Bezier(t, p0, p1, p2);

            particles[i].position = pos;

            // OPTIONAL: kill particle when it reaches end
            if (t >= 1f)
            {
                particles[i].remainingLifetime = 0;
                progress[i] = 0f;
            }
        }

        ps.SetParticles(particles, count);
    }

    Vector3 Bezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 +
               2 * (1 - t) * t * p1 +
               Mathf.Pow(t, 2) * p2;
    }
}