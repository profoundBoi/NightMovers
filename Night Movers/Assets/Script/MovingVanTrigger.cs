using UnityEngine;

public class MovingVanTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pickup"))
            return;

        int index = GetItemIndex(other.gameObject.name);

        if (index == -1) return;

        MovingVanUI.Instance.SetItemDelivered(index);

        Destroy(other.gameObject);
    }

    private int GetItemIndex(string itemName)
    {
        switch (itemName)
        {
            case "Table": return 0;
            case "Necklace": return 1;
            case "Mirror": return 2;
            case "SideTable": return 3;
            case "Lamp": return 4;
        }

        return -1;
    }
}