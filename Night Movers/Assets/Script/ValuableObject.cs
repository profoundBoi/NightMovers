using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class ValuableObject : NetworkBehaviour
{
    [Header("Value")]
    public int maxValue = 100;
    private NetworkVariable<int> currentValue = new NetworkVariable<int>();

    [Header("Durability")]
    public float maxDurability = 100f;
    private NetworkVariable<float> durability = new NetworkVariable<float>();

    [Header("Damage Settings")]
    public float minImpactVelocity = 2f;
    public float damageMultiplier = 10f;

    [Header("Break Effect")]
    public GameObject breakEffect;

    private bool isBroken = false;
    private int lastValue;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentValue.Value = maxValue;
            durability.Value = maxDurability;

            lastValue = maxValue;

            MoneyManager.Instance.AddMoney(maxValue);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || isBroken) return;

        float impact = collision.relativeVelocity.magnitude;

        if (impact < minImpactVelocity) return;

        float damage = impact * damageMultiplier;

        ApplyDamage(damage);
    }

    void ApplyDamage(float amount)
    {
        durability.Value -= amount;

        float percent = Mathf.Clamp01(durability.Value / maxDurability);
        int newValue = Mathf.RoundToInt(maxValue * percent);

        currentValue.Value = newValue;

        int difference = lastValue - newValue;

        if (difference > 0)
        {
            MoneyManager.Instance.RemoveMoney(difference);
        }

        lastValue = newValue;

        if (durability.Value <= 0)
        {
            Break();
        }
    }

    void Break()
    {
        isBroken = true;

        // Remove remaining value
        if (lastValue > 0)
        {
            MoneyManager.Instance.RemoveMoney(lastValue);
        }

        Vector3 pos = transform.position;
        BreakClientRpc(pos);

        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    void BreakClientRpc(Vector3 position)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }

        if (breakEffect != null)
        {
            GameObject fx = Instantiate(breakEffect, position, Quaternion.identity);
            Destroy(fx, 3f);
        }
    }

    public int GetValue()
    {
        return currentValue.Value;
    }
}
