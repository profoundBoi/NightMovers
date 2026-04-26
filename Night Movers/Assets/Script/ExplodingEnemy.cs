using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;


[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
public class ExplodingEnemy : NetworkBehaviour
{
    [Header("Movement")]
    public float detectionRange = 20f;
    public float explodeRange = 2.5f;

    [Header("Explosion")]
    public float explosionDelay = 1f;
    public float explosionRadius = 3f;
    public int damage = 50;
    public LayerMask damageLayer;
    public GameObject explosionEffect;

    private Transform target;
    private NavMeshAgent agent;
    private bool isExploding = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = explodeRange * 0.9f;
    }

    void Update()
    {
        if (!IsServer || isExploding) return;

        FindClosestPlayer();
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= explodeRange)
        {
            StartCoroutine(Explode());
        }
        else if (distance <= detectionRange)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            if (agent.hasPath)
                agent.ResetPath();
        }
    }

    void FindClosestPlayer()
    {
        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            Collider col = client.PlayerObject.GetComponentInChildren<Collider>();
            if (col == null) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col.transform;
            }
        }

        target = closest;
    }

    IEnumerator Explode()
    {
        isExploding = true;

        agent.isStopped = true;
        agent.ResetPath();

        Vector3 explosionPos = transform.position;

        HideEnemyClientRpc();

        yield return new WaitForSeconds(0.05f);

        TriggerExplosionClientRpc(explosionPos);

        DealDamage(explosionPos);

        yield return new WaitForSeconds(explosionDelay);

        GetComponent<NetworkObject>().Despawn();
    }

    void DealDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, damageLayer);

        foreach (Collider hit in hits)
        {
            // Try any health script
          //  var health = hit.GetComponentInParent<IDamageable>();
          //  if (health != null)
//{
          //      health.TakeDamage(damage);
///}
        }
    }

    [ClientRpc]
    void HideEnemyClientRpc()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        if (agent != null)
            agent.enabled = false;
    }

    [ClientRpc]
    void TriggerExplosionClientRpc(Vector3 position)
    {
        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, position, Quaternion.identity);
            Destroy(fx, 3f);
        }
    }
}
