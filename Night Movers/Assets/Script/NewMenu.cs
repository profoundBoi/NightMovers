using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class NewMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button hostButton;
    public Button joinButton;
    public Button serverButton;

    private void Start()
    {
        // Assign button listeners
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);

        if (serverButton != null)
            serverButton.onClick.AddListener(StartServer);
    }

    public void StartHost()
    {
        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        Debug.Log("Starting Client...");
        NetworkManager.Singleton.StartClient();
    }

    public void StartServer()
    {
        Debug.Log("Starting Server...");
        NetworkManager.Singleton.StartServer();
    }
}
