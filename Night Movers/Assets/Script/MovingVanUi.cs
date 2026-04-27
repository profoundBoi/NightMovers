using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MovingVanUI : NetworkBehaviour
{
    public static MovingVanUI Instance;

    [Header("Item Blocks")]
    [SerializeField] private Image TableBlock;
    [SerializeField] private Image NecklaceBlock;
    [SerializeField] private Image MirrorBlock;
    [SerializeField] private Image SideTableBlock;
    [SerializeField] private Image LampBlock;

    [Header("End Panel")]
    [SerializeField] private GameObject endPanel;

    private Image[] blocks;
    private bool[] delivered;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        blocks = new Image[]
        {
            TableBlock,
            NecklaceBlock,
            MirrorBlock,
            SideTableBlock,
            LampBlock
        };

        delivered = new bool[blocks.Length];

        if (endPanel != null)
            endPanel.SetActive(false);
    }

    public void SetItemDelivered(int index)
    {
        if (!IsServer)
        {
            SetItemDeliveredServerRpc(index);
            return;
        }

        HandleDelivery(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetItemDeliveredServerRpc(int index)
    {
        HandleDelivery(index);
    }

    private void HandleDelivery(int index)
    {
        if (index < 0 || index >= delivered.Length) return;
        if (delivered[index]) return;

        delivered[index] = true;

        UpdateBlockClientRpc(index);

        CheckAllDelivered();
    }

    [ClientRpc]
    private void UpdateBlockClientRpc(int index)
    {
        if (blocks[index] != null)
            blocks[index].color = new Color(0.18f, 0.80f, 0.44f);
    }

    private void CheckAllDelivered()
    {
        for (int i = 0; i < delivered.Length; i++)
        {
            if (!delivered[i])
                return;
        }

        ShowEndPanelClientRpc();
    }

    [ClientRpc]
    private void ShowEndPanelClientRpc()
    {
        if (endPanel != null)
            endPanel.SetActive(true);
    }
}