using UnityEngine;
using Unity.Netcode;

public class IntroManager : NetworkBehaviour
{
    public GameObject[] panels;
    private int currentPanel = 0;

    private void Start()
    {
        if (IsClient)
        {
            Time.timeScale = 0f;
            ShowPanel(0);
        }
    }

    void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
        }
    }

    // Called by button
    public void OnNextPressed()
    {
        SubmitNextServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitNextServerRpc()
    {
        currentPanel++;

        if (currentPanel >= panels.Length)
        {
            StartGameClientRpc();
        }
        else
        {
            UpdatePanelClientRpc(currentPanel);
        }
    }

    [ClientRpc]
    void UpdatePanelClientRpc(int index)
    {
        ShowPanel(index);
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        Time.timeScale = 1f;

        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }
    }
}