using UnityEngine;
using Unity.Netcode.Components;
using Unity.Netcode;

public class StartCubeRotateAnim : NetworkBehaviour
{
    private NetworkAnimator netAnim;

   public override void OnNetworkSpawn()
    {
        netAnim = GetComponent<NetworkAnimator>();

        if (IsServer)
        {
            netAnim.SetTrigger("StartRotate");
        }
    }

}
