using Unity.Netcode;
using UnityEngine;

public class DelayedFollow : NetworkBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothTime = 1.2f;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target != null)
            transform.position = target.position;
    }

    void Update()
    {
        if (target == null) return;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            target.position,
            ref velocity,
            smoothTime
        );
    }
}