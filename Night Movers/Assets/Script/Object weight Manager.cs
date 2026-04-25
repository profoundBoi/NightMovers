using Unity.Netcode;
using UnityEngine;

public class ObjectweightManager : NetworkBehaviour
{
    [SerializeField]
    private bool isHeavyObject, isNormalObject;
    public bool canBePickedUp;
    public int PlayersHoldingObject;

    private void FixedUpdate()
    {
        performPickUp();
    }


    public void performPickUp()
    {
        if (isNormalObject)
        {
            canBePickedUp = true;
        }
        else if (isHeavyObject)
        {
            if (PlayersHoldingObject == 2)
            {
                canBePickedUp = true;
            }
        }
    }


}
