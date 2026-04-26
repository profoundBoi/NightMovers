using UnityEngine;
using Unity.Netcode;


public class MoneyManager : NetworkBehaviour
{
    public static MoneyManager Instance;

    public NetworkVariable<int> totalMoney = new NetworkVariable<int>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (totalMoney.Value == 0)
        {
            totalMoney.Value = 0;
        }
    }

    public void AddMoney(int amount)
    {
        if (!IsServer) return;

        totalMoney.Value += amount;
    }

    public void RemoveMoney(int amount)
    {
        if (!IsServer) return;

        totalMoney.Value -= amount;
        totalMoney.Value = Mathf.Max(0, totalMoney.Value);
    }

    public void RecalculateTotal()
    {
        if (!IsServer) return;

        int total = 0;

        foreach (var obj in FindObjectsOfType<ValuableObject>())
        {
            total += obj.GetValue();
        }

        totalMoney.Value = total;
    }
}
