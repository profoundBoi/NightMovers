using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : NetworkBehaviour
{
    [SerializeField]
    private Vector3 moveInput;
    private Vector2 lookInput;

    private Rigidbody rb;
    private PlayerInput playerInput;
    private Animator animator;

    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public float SpeedMultiplier;
    private float baseSpeed;
    private float RunSpeed;

    [Header("Look")]
    public float lookSensitivity = 120f;
    [SerializeField]
    private Transform PlayerCamera;
    public float minLookX = -60f;
    public float maxLookX = 60f;
    private float xRotation;
    [SerializeField]
    private float maxDistance, minDistance;

    // Interactions
    private GameObject InteractableObject;
    public LayerMask Interact;
    [SerializeField]
    private int Interactrange;
    private GameObject CurrentInteractableObject;
    public Transform HoldPosition;
    public Transform MidlePoint;
    [SerializeField]
    public GameObject heldObject;
    private ObjectweightManager currentHeavyObject;
    public Transform MedianPoint;


    // Attack
    [SerializeField] private bool isChargingWeapon;
    [SerializeField] private float attackPower;
    [SerializeField] private GameObject heldWeapon;
    [SerializeField] private int AimDistance;
    private bool isShooting;

    // Sniper UI
    public GameObject SniperScopeUi;

    // UI controls
    [Header("UI controls")]
    [SerializeField] private GameObject PausePanel;
    [SerializeField] private Canvas PauseCanvas, InventoryCanvas;
    [SerializeField] private GameObject InventoryPanel;

    // Player Input Manager
    private PlayerInputManager playerInputManager;
    private GameObject playerInputmNagerHolder;
    [SerializeField] private MultiplayerEventSystem eventSystem;
    [SerializeField] private GameObject PauseFirstSelect, InventoryFirstSelect;

    // Player Animations
    [Header("Animations")]
    [SerializeField] private Animator playerAnimations;
    private bool isJumping;
    [SerializeField] private List<string> AnimationBools;

    // Enemy Stats
    [Header("Enemy Stats")]
    public GameObject OtherPlayer;
    private PlayerController3D otherPlayersScript;

    // Slide Stats
    [Header("Slide Stats")]
    public float forceAmount = 20f;
    public float forceDuration = 1f;
    private bool isRunning;
    private bool isSliding;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;

    private PlayerInput playerinput;

    [Header("Map Settings")]
    [SerializeField] private Transform PlayerMiniMap;
    [SerializeField] private Transform OpenMapCamera;
    [SerializeField] private Vector3 OpenMap, ClosedMap;
    [SerializeField] private Vector3 OpenMapPosition, ClosedMapPosition;
    [SerializeField] private Vector3 OpenMapCameraPosition, ClosedMapCameraPosition;

    [Header("Carry Object Settings")]
    [SerializeField] private ObjectweightManager ObjectScript;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            PlayerInput pi = GetComponent<PlayerInput>();
            if (pi != null) pi.enabled = false;

            if (PlayerCamera != null) PlayerCamera.gameObject.SetActive(false);
            return;
        }

        playerinput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        baseSpeed = speed;
        RunSpeed = speed * SpeedMultiplier;

        Debug.Log($"[PlayerController3D] I own this player. OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton.LocalClientId}");
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        PlayerInput pi = GetComponent<PlayerInput>();
        if (pi != null) pi.enabled = false;
    }

    // ── INPUT CALLBACKS ────────────────────────────────────────────────────────

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Vector2 input = context.ReadValue<Vector2>();
        moveInput = new Vector3(input.x, 0f, input.y);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.performed && IsGrounded())
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.performed)
        {
            isRunning = true;
            speed = RunSpeed;
        }
        else if (context.canceled)
        {
            isRunning = false;
            speed = baseSpeed;
        }
    }

    public void OnMapOpen(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.performed)
        {
            PlayerMiniMap.localScale = OpenMap;
            PlayerMiniMap.GetComponent<RectTransform>().position = OpenMapPosition;
            OpenMapCamera.localPosition = OpenMapCameraPosition;
        }
        else if (context.canceled)
        {
            PlayerMiniMap.localScale = ClosedMap;
            PlayerMiniMap.GetComponent<RectTransform>().position = ClosedMapPosition;
            OpenMapCamera.localPosition = ClosedMapCameraPosition;
        }
    }

    public void OnOpenInventorysystem(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (InventoryPanel.activeSelf)
        {
            InventoryPanel.SetActive(false);
            speed = baseSpeed;
        }
        else
        {
            eventSystem.firstSelectedGameObject = InventoryFirstSelect;
            eventSystem.playerRoot = InventoryCanvas.gameObject;
            InventoryPanel.SetActive(true);
            speed = 0;
        }
    }

    public void PauseandPlay(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (PausePanel.activeSelf)
        {
            PausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
        else
        {
            PausePanel.SetActive(true);
            Time.timeScale = 0f;
            eventSystem.playerRoot = PauseCanvas.gameObject;
            eventSystem.firstSelectedGameObject = PauseFirstSelect;
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
        {
            Ray ray = new Ray(PlayerCamera.position, PlayerCamera.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Interactrange, Interact))
            {
                if (hit.collider != null)
                {
                    GameObject target = hit.collider.gameObject;
                    ObjectweightManager objScript = target.GetComponent<ObjectweightManager>();
                    NetworkObject netObj = target.GetComponent<NetworkObject>();

                    if (objScript != null && objScript.canBePickedUp && netObj != null)
                    {
                        heldObject = target;
                        PickUpObjectServerRpc(netObj.NetworkObjectId);
                    }
                    else if (objScript != null && !objScript.canBePickedUp && netObj != null)
                    {
                        currentHeavyObject = objScript;
                        objScript.AddHoldPositionServerRpc(NetworkObject.NetworkObjectId);
                        
                    }
                }
            }
        }
        else if (context.canceled)
        {
            // Clear heavy object hold positions on release
            if (currentHeavyObject != null)
            {
                currentHeavyObject.ClearHoldPositionsServerRpc();
                currentHeavyObject = null;
            }

            // Drop held object if carrying one
            if (heldObject != null)
            {
                NetworkObject netObj = heldObject.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    DropObjectServerRpc(netObj.NetworkObjectId);
                }
                heldObject = null;
            }
        }
    }

    // ── SERVER RPCS ────────────────────────────────────────────────────────────

    [ServerRpc]
    private void PickUpObjectServerRpc(ulong networkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
            return;

        // Destroy Rigidbody on pickup — re-added fresh on drop
        Rigidbody objRb = netObj.GetComponent<Rigidbody>();
        if (objRb != null) Destroy(objRb);

        SetHeldObjectClientRpc(networkObjectId);
    }

    [ServerRpc]
    private void DropObjectServerRpc(ulong networkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
            return;

        Rigidbody objRb = netObj.GetComponent<Rigidbody>();
        if (objRb == null) objRb = netObj.gameObject.AddComponent<Rigidbody>();
        objRb.isKinematic = false;
        objRb.useGravity = true;

        ObjectweightManager owm = netObj.GetComponent<ObjectweightManager>();
        if (owm != null)
        {
            owm.ClearHoldPositionsClientRpc();
        }

        ClearHeldObjectClientRpc();
    }

    // ── CLIENT RPCS ────────────────────────────────────────────────────────────

    [ClientRpc]
    private void SetHeldObjectClientRpc(ulong networkObjectId)
    {
        if (!IsOwner) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
            heldObject = netObj.gameObject;
    }

    [ClientRpc]
    private void ClearHeldObjectClientRpc()
    {
        if (!IsOwner) return;
        heldObject = null;
    }

    // ── PHYSICS UPDATE ─────────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector3 moveDirection = transform.TransformDirection(moveInput).normalized;
        Vector3 targetVelocity = moveDirection * speed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        if (heldObject != null)
        {
            ObjectweightManager owm = heldObject.GetComponent<ObjectweightManager>();

            // Only manually move if it's a normal object
            // Heavy objects are parented to the median point and moved by ObjectweightManager
            if (owm == null || owm.isNormalObject)
            {
                heldObject.transform.position = HoldPosition.position;
                heldObject.transform.rotation = HoldPosition.rotation;
            }
        }

        checkForInteraction();
    }

    // ── CAMERA & ANIMATIONS ────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (!IsOwner) return;

        float mouseX = lookInput.x * lookSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = lookInput.y * lookSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookX, maxLookX);
        PlayerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        float t = Mathf.InverseLerp(minLookX, maxLookX, xRotation);
        float distance = Mathf.Lerp(minDistance, maxDistance, t);
        PlayerCamera.localPosition = new Vector3(0f, 0f, distance);

        bool moving = moveInput.x != 0 || moveInput.z != 0;
        bool grounded = IsGrounded();


    }

    // ── INTERACTION / OUTLINE ──────────────────────────────────────────────────

    private void checkForInteraction()
    {
        if (!IsOwner) return;

        Ray ray = new Ray(PlayerCamera.position, PlayerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Interactrange, Interact))
        {
            if (hit.collider != null)
            {
                GameObject hitObject = hit.collider.gameObject;

                if (hitObject != CurrentInteractableObject)
                {
                    if (CurrentInteractableObject != null)
                    {
                        Outline old = CurrentInteractableObject.GetComponent<Outline>();
                        if (old != null) Destroy(old);
                    }

                    CurrentInteractableObject = hitObject;

                    if (CurrentInteractableObject.GetComponent<Outline>() == null)
                        CurrentInteractableObject.AddComponent<Outline>();
                }
            }
        }
        else
        {
            if (CurrentInteractableObject != null)
            {
                Outline old = CurrentInteractableObject.GetComponent<Outline>();
                if (old != null) Destroy(old);
                CurrentInteractableObject = null;
            }
        }
    }





    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}