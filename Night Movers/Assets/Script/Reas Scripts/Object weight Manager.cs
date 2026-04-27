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
                    // Median point doesn't exist yet — create it and enable kinematic
                    medianPointObject = new GameObject("MedianHoldPoint");
                    medianPointObject.transform.position = medianPoint;

                    NetworkObject netObj = medianPointObject.AddComponent<NetworkObject>();
                    netObj.Spawn();

                    NetworkObject thisNetObj = GetComponent<NetworkObject>();
                    if (thisNetObj != null)
                        thisNetObj.TrySetParent(medianPointObject.transform, worldPositionStays: true);

                    // FIX: Object is now on the midpoint — disable physics so it
                    // doesn't fall or collide while being carried by two players
                    SetKinematicClientRpc(true);
                }
                else
                {
                    // Median point already exists — just update its position each frame
                    medianPointObject.transform.position = medianPoint;
                }
            }
            else if (playerHoldingPosition.Count < 2 && medianPointObject != null)
            {
                // Not enough players holding — tear down the median point
                NetworkObject thisNetObj = GetComponent<NetworkObject>();
                if (thisNetObj != null)
                    thisNetObj.TrySetParent((Transform)null, worldPositionStays: true);

                NetworkObject medianNetObj = medianPointObject.GetComponent<NetworkObject>();
                if (medianNetObj != null)
                    medianNetObj.Despawn(true);

                medianPointObject = null;

                // FIX: Object is no longer on the midpoint — restore physics so it
                // falls naturally after being released
                SetKinematicClientRpc(false);
            }
        }
    }

    // FIX: Changed SetKinematic to a ClientRpc so the kinematic/gravity state is
    // applied on ALL clients, not just the server. Previously the Rigidbody on
    // non-server clients was never updated, causing the object to fall through the
    // world or float on client machines even while being carried.
    [ClientRpc]
    private void SetKinematicClientRpc(bool isKinematic)
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

        // FIX: Kinematic ON when median hold point is created — object is now being carried
        SetKinematicClientRpc(true);
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

        // FIX: Kinematic OFF when median hold point is destroyed — object is no longer carried
        SetKinematicClientRpc(false);
    }

    [ClientRpc]
    public void ClearHoldPositionsClientRpc()
    {
        playerHoldingPosition.Clear();
    }
}