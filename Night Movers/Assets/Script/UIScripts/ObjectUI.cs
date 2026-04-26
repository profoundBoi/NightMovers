using UnityEngine;
using TMPro;

public class ObjectUI : MonoBehaviour
{
    public ValuableObject target;
    public TextMeshProUGUI valueText;

    void Update()
    {
        if (target != null)
        {
            valueText.text = "$" + target.GetValue();
        }

        transform.forward = Camera.main.transform.forward;
    }
}
