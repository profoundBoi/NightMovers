using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectweightManager : NetworkBehaviour
{
    [SerializeField]
    public bool isHeavyObject, isNormalObject;
    public bool canBePickedUp;

    public List<Transform> playerHoldingPosition = new List<Transform>();

    private GameObject medianPointObject;

    private void FixedUpdate()
    {
        performPickUp();
        if (!IsServer) return;

        if (isHeavyObject)
        {
            if (playerHoldingPosition.Count == 2 &&
                playerHoldingPosition[0] != null &&
                playerHoldingPosition[1] != null)
            {
                Vector3 medianPoint = (playerHoldingPosition[0].position + playerHoldingPosition[1].position) / 2f;

                if (medianPointObject == null)
                {
                    medianPointObject = new GameObject("MedianHoldPoint");
                    medianPointObject.transform.position = medianPoint;

                    NetworkObject netObj = medianPointObject.AddComponent<NetworkObject>();
                    netObj.Spawn();

                    NetworkObject thisNetObj = GetComponent<NetworkObject>();
                    if (thisNetObj != null)
                        thisNetObj.TrySetParent(medianPointObject.transform, worldPositionStays: true);

                    SetKinematic(true); // 👈 disable physics when held
                }
                else
                {
                    medianPointObject.transform.position = medianPoint;
                }
            }
            else if (playerHoldingPosition.Count < 2 && medianPointObject != null)
            {
                NetworkObject thisNetObj = GetComponent<NetworkObject>();
                if (thisNetObj != null)
                    thisNetObj.TrySetParent((Transform)null, worldPositionStays: true);

                NetworkObject medianNetObj = medianPointObject.GetComponent<NetworkObject>();
                if (medianNetObj != null)
                    medianNetObj.Despawn(true);

                medianPointObject = null;
                SetKinematic(false); // 👈 re-enable physics when released
            }
        }
    }

    private void SetKinematic(bool isKinematic)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = isKinematic;
        rb.useGravity = !isKinematic;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClearHoldPositionsServerRpc()
    {
        playerHoldingPosition.Clear();
        ClearHoldPositionsClientRpc();
    }

    public void performPickUp()
    {
        if (isNormalObject)
        {
            canBePickedUp = true;
        }
        else if (isHeavyObject)
        {
            canBePickedUp = playerHoldingPosition.Count == 2 &&
                            playerHoldingPosition[0] != null &&
                            playerHoldingPosition[1] != null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddHoldPositionServerRpc(ulong playerNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
            return;

        PlayerController3D playerScript = playerNetObj.GetComponent<PlayerController3D>();
        if (playerScript == null) return;

        if (playerHoldingPosition.Count < 2)
        {
            playerHoldingPosition.Add(playerScript.HoldPosition);
            Debug.Log($"Hold position added. Total: {playerHoldingPosition.Count}");
        }

        // Once both players are holding, create the median point and parent the object
        if (playerHoldingPosition.Count == 2)
            CreateMedianHoldPoint();
    }


    public void CreateMedianHoldPoint()
    {
        if (!IsServer) return;
        if (playerHoldingPosition.Count < 2) return;
        if (playerHoldingPosition[0] == null || playerHoldingPosition[1] == null) return;

        Vector3 medianPoint = (playerHoldingPosition[0].position + playerHoldingPosition[1].position) / 2f;
        canBePickedUp = true;

        medianPointObject = new GameObject("MedianHoldPoint");
        medianPointObject.transform.position = medianPoint;

        NetworkObject netObj = medianPointObject.AddComponent<NetworkObject>();
        netObj.Spawn();

        NetworkObject thisNetObj = GetComponent<NetworkObject>();
        if (thisNetObj != null)
            thisNetObj.TrySetParent(medianPointObject.transform, worldPositionStays: true);
    }

    public void DestroyMedianHoldPoint()
    {
        if (!IsServer) return;

        NetworkObject thisNetObj = GetComponent<NetworkObject>();
        if (thisNetObj != null)
            thisNetObj.TrySetParent((Transform)null, worldPositionStays: true);

        if (medianPointObject != null)
        {
            NetworkObject medianNetObj = medianPointObject.GetComponent<NetworkObject>();
            if (medianNetObj != null)
                medianNetObj.Despawn(true);
            medianPointObject = null;
        }
    }

    [ClientRpc]
    public void ClearHoldPositionsClientRpc()
    {
        playerHoldingPosition.Clear();
    }


}