using UnityEngine;
using TMPro;
using Unity.Netcode;


public class MoneyUI : NetworkBehaviour
{
    public TextMeshProUGUI moneyText;

    void Start()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.totalMoney.OnValueChanged += UpdateUI;
            UpdateUI(0, MoneyManager.Instance.totalMoney.Value);
        }
    }

    void UpdateUI(int oldValue, int newValue)
    {
        moneyText.text = "Money: $" + newValue;
    }
}
