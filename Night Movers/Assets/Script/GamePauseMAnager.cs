using UnityEngine;
using Unity.Netcode;

public class IntroManager : NetworkBehaviour
{
    public GameObject[] panels;
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    private int currentIndex = 0;

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
        // Safety check
        if (index < 0 || index >= panels.Length) return;

        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
        }

        // Play matching audio
        if (index < audioClips.Length && audioSource != null)
        {
            audioSource.clip = audioClips[index];
            audioSource.Play();
        }
    }

    public void OnNextPressed()
    {
        SubmitNextServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitNextServerRpc()
    {
        currentIndex++;

        if (currentIndex >= panels.Length)
        {
            StartGameClientRpc();
        }
        else
        {
            UpdatePanelClientRpc(currentIndex);
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

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}