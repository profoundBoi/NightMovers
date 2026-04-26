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
        if (IsServer)
        {
            totalMoney.Value = 0;
        }
    }

    // Called when value changes
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
}
