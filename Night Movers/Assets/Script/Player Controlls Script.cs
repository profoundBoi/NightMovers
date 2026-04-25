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
    private float baseSpeed;      // FIX: store the original speed so run/stop is reliable
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
    private GameObject heldObject;

    // Attack
    [SerializeField]
    private bool isChargingWeapon;
    [SerializeField]
    private float attackPower;
    [SerializeField]
    private GameObject heldWeapon;
    [SerializeField]
    private int AimDistance;
    private bool isShooting;

    // Sniper UI
    public GameObject SniperScopeUi;

    // UI controls
    [Header("UI controls")]
    [SerializeField] private GameObject PausePanel;
    [SerializeField] private Canvas PauseCanvas, InventoryCanvas;
    [SerializeField] private GameObject InventoryPanel;

    // Player Assortment Manager
    private PlayerInputManager playerInputManager;
    private GameObject playerInputmNagerHolder;
    [SerializeField]
    private MultiplayerEventSystem eventSystem;
    [SerializeField] private GameObject PauseFirstSelect, InventoryFirstSelect;

    // Player Animations
    [Header("Animations")]
    [SerializeField]
    private Animator playerAnimations;
    private bool isJumping;
    [SerializeField]
    private List<string> AnimationBools;

    // Player Outline
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

    // Seagull Settings
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
        // FIX: All setup guarded behind IsOwner — non-owners do nothing here
        if (!IsOwner)
        {
            // Disable input and camera on non-owner instances so they don't interfere
            PlayerInput pi = GetComponent<PlayerInput>();
            if (pi != null) pi.enabled = false;

            if (PlayerCamera != null) PlayerCamera.gameObject.SetActive(false);
            return;
        }

        playerinput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        // FIX: Only lock cursor once — the original set Locked then immediately overwrote with None
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // FIX: Cache base speed before any multipliers are applied
        baseSpeed = speed;
        RunSpeed = speed * SpeedMultiplier;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        // FIX: Disable input component on despawn to prevent ghost inputs
        PlayerInput pi = GetComponent<PlayerInput>();
        if (pi != null) pi.enabled = false;
    }

    // MOVEMENT
    public void OnMove(InputAction.CallbackContext context)
    {
        // FIX: Guard all input callbacks — without this every client moves every player
        if (!IsOwner) return;

        Vector2 input = context.ReadValue<Vector2>();
        moveInput = new Vector3(input.x, 0f, input.y);
    }

    // Map Settings
    public void OnMapOpen(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
        {
            PlayerMiniMap.localScale = OpenMap;
            RectTransform mapPosition = PlayerMiniMap.GetComponent<RectTransform>();
            mapPosition.position = OpenMapPosition;
            OpenMapCamera.localPosition = OpenMapCameraPosition;
        }
        else if (context.canceled)
        {
            PlayerMiniMap.localScale = ClosedMap;
            RectTransform mapPosition = PlayerMiniMap.GetComponent<RectTransform>();
            mapPosition.position = ClosedMapPosition;
            OpenMapCamera.localPosition = ClosedMapCameraPosition;
        }
    }

    // Inventory System
    public void OnOpenInventorysystem(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (InventoryPanel.activeSelf)
        {
            InventoryPanel.SetActive(false);
            speed = baseSpeed; // FIX: restore to base speed, not a hardcoded 5
        }
        else
        {
            eventSystem.firstSelectedGameObject = InventoryFirstSelect;
            eventSystem.playerRoot = InventoryCanvas.gameObject;
            InventoryPanel.SetActive(true);
            speed = 0;
        }
    }

    // LOOK
    public void OnLook(InputAction.CallbackContext context)
    {
        // FIX: Guard — without this every client rotates every player's camera
        if (!IsOwner) return;

        lookInput = context.ReadValue<Vector2>();
    }

    // Pause/Play
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

    // JUMP
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            // PlayJump();
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
        {
            isRunning = true;
            speed = RunSpeed; // FIX: set to cached RunSpeed instead of multiplying current speed (which compounds each press)
        }
        else if (context.canceled)
        {
            isRunning = false;
            speed = baseSpeed; // FIX: restore to cached base speed instead of dividing (which could drift)
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
                        // Ask the server to reparent this NetworkObject under our HoldPosition
                        PickUpObjectServerRpc(netObj.NetworkObjectId);
                    }
                }
            }
        }
        else if (context.canceled)
        {
            if (heldObject != null)
            {
                NetworkObject netObj = heldObject.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    // Ask the server to drop the object (unparent and restore physics)
                    DropObjectServerRpc(netObj.NetworkObjectId);
                }
                heldObject = null;
            }
        }
    }

    [ServerRpc]
    private void PickUpObjectServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            // Make kinematic so physics doesn't fight the movement
            Rigidbody objRb = netObj.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.isKinematic = true;
                objRb.useGravity = false;
            }

            // We do NOT call TrySetParent to HoldPosition — HoldPosition is a plain Transform
            // child, not a NetworkObject. Netcode forbids parenting a NetworkObject under a
            // non-NetworkObject parent. Instead we leave it unparented and follow HoldPosition
            // every frame in FixedUpdate on all clients via the ClientRpc below.
            SetHeldObjectClientRpc(networkObjectId);
        }
    }

    [ClientRpc]
    private void SetHeldObjectClientRpc(ulong networkObjectId)
    {
        // Store the reference on every client so FixedUpdate can move it to HoldPosition
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            heldObject = netObj.gameObject;
        }
    }

    [ServerRpc]
    private void DropObjectServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            // Re-enable physics on drop
            Rigidbody objRb = netObj.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.isKinematic = false;
                objRb.useGravity = true;
            }

            // Clear the held reference on all clients
            ClearHeldObjectClientRpc();
        }
    }

    [ClientRpc]
    private void ClearHeldObjectClientRpc()
    {
        heldObject = null;
    }

    private void checkForInteraction()
    {
        // FIX: Only the owner should be raycasting and modifying outlines
        if (!IsOwner) return;

        Ray ray = new Ray(PlayerCamera.position, PlayerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Interactrange, Interact))
        {
            if (hit.collider != null)
            {
                GameObject hitObject = hit.collider.gameObject;

                // FIX: Only update outline when the target actually changes — prevents AddComponent spam every frame
                if (hitObject != CurrentInteractableObject)
                {
                    // Remove outline from previous object
                    if (CurrentInteractableObject != null)
                    {
                        Outline oldOutline = CurrentInteractableObject.GetComponent<Outline>();
                        if (oldOutline != null) Destroy(oldOutline);
                    }

                    CurrentInteractableObject = hitObject;

                    // Add outline only if it doesn't already have one
                    if (CurrentInteractableObject.GetComponent<Outline>() == null)
                        CurrentInteractableObject.AddComponent<Outline>();
                }
            }
        }
        else
        {
            if (CurrentInteractableObject != null)
            {
                Outline outline = CurrentInteractableObject.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
                CurrentInteractableObject = null;
            }
        }
    }

    void FixedUpdate()
    {
        // FIX: Non-owners must not have their physics driven by this client
        if (!IsOwner) return;

        Vector3 moveDirection = transform.TransformDirection(moveInput).normalized;
        Vector3 targetVelocity = moveDirection * speed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        // Follow HoldPosition every frame while carrying an object.
        // We can't parent a NetworkObject to a plain child Transform,
        // so instead we continuously move it to match HoldPosition's world position.
        if (heldObject != null)
        {
            heldObject.transform.position = HoldPosition.position;
            heldObject.transform.rotation = HoldPosition.rotation;
        }

        checkForInteraction();
    }

    void LateUpdate()
    {
        // FIX: Non-owners must not have their camera/rotation driven by this client
        if (!IsOwner) return;

        // Horizontal rotation (player body)
        float mouseX = lookInput.x * lookSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation (camera)
        float mouseY = lookInput.y * lookSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookX, maxLookX);

        PlayerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        float pitch = xRotation;
        float t = Mathf.InverseLerp(minLookX, maxLookX, pitch);
        float distance = Mathf.Lerp(minDistance, maxDistance, t);
        PlayerCamera.localPosition = new Vector3(0f, 0f, distance);

        // Animations
        if (moveInput.x > 0 || moveInput.z > 0 || moveInput.x < 0 || moveInput.z < 0)
        {
            if (IsGrounded())
            {
                if (speed == RunSpeed)
                {
                    // PlayRun();
                }
                else
                {
                    // PlayWalk();
                }
            }
            else
            {
                // PlayJump();
            }
        }
        else if (moveInput.x == 0 && moveInput.z == 0)
        {
            if (IsGrounded())
            {
                // PlayIdle();
            }
            else
            {
                // PlayJump();
            }
        }
    }

    void PlayJump()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
            playerAnimations.SetBool(AnimationBools[i], false);
        playerAnimations.SetBool(AnimationBools[2], true);
    }

    void PlayWalk()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
            playerAnimations.SetBool(AnimationBools[i], false);
        playerAnimations.SetBool(AnimationBools[0], true);
    }

    void PlayRun()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
            playerAnimations.SetBool(AnimationBools[i], false);
        playerAnimations.SetBool(AnimationBools[1], true);
    }

    void PlayIdle()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
            playerAnimations.SetBool(AnimationBools[i], false);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}