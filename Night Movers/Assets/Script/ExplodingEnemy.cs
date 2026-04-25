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
    public float explodeRange = 2f;

    [Header("Explosion")]
    public float explosionDelay = 1f;
    public float explosionRadius = 3f;
    public int damage = 50;
    public GameObject explosionEffect;

    private Transform target;
    private NavMeshAgent agent;
    private bool isExploding = false;


    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing on ExplodingEnemy!");
            return;
        }

        agent.stoppingDistance = explodeRange * 0.8f;
        agent.updateRotation = true;
    }


    void Update()
    {
        if (!IsServer || isExploding) return;

        FindClosestPlayer();

        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= explodeRange && !isExploding)
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

            Transform playerTransform = client.PlayerObject.transform;

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = playerTransform;
            }
        }

        target = closest;
    }

    IEnumerator Explode()
    {
        isExploding = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

     
        Vector3 explosionPos = transform.position;

        HideEnemyClientRpc();

        yield return new WaitForSeconds(0.05f);

        TriggerExplosionClientRpc(explosionPos);

        yield return new WaitForSeconds(explosionDelay);

        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    void HideEnemyClientRpc()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    [ClientRpc]
    void TriggerExplosionClientRpc(Vector3 position)
    {
        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(
                explosionEffect,
                position,
                Quaternion.identity
            );

            Destroy(fx, 3f);
        }
        else
        {
            Debug.LogWarning("Explosion effect not assigned!");
        }
    }
}
