using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI moneyText;
    private bool subscribed = false;

    void Update()
    {
        if (!subscribed && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.totalMoney.OnValueChanged += UpdateUI;
            UpdateUI(0, MoneyManager.Instance.totalMoney.Value);
            subscribed = true;
        }
    }

    void UpdateUI(int oldValue, int newValue)
    {
        moneyText.text = "Money: $" + newValue;
    }
}