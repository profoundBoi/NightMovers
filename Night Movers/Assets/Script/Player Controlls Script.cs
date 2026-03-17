using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
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
    private float RunSpeed;
    [Header("Look")]
    public float lookSensitivity = 120f;
    [SerializeField]
    private Transform CameraHolder;
    [SerializeField]
    private Transform PlayerCamera;
    public float minLookX = -60f;
    public float maxLookX = 60f;
    [SerializeField]
    private Camera playerCam;
    private float xRotation;
    [SerializeField]
    private float maxDistance, minDistance;

    //Interactions
    private GameObject InteractableObject;
    public LayerMask Interact;


    //Attack
    [SerializeField]
    private bool isChargingWeapon;
    [SerializeField]
    private float attackPower;
    [SerializeField]
    private GameObject heldWeapon;
    [SerializeField]
    private Transform HoldingPosition;
    [SerializeField]
    private int AimDistance;
    private ShootManager ShootScript;
    private bool isShooting;

    //Sniper UI
    public GameObject SniperScopeUi;


    //UI controls
    [Header("UI controls")]
    [SerializeField] private GameObject PausePanel;
    [SerializeField] private Canvas PauseCanvas, InventoryCanvas;
    [SerializeField] private GameObject InventoryPanel;

    //Player Assortment Manager
    private PlayerInputManager playerInputManager;
    private GameObject playerInputmNagerHolder;
    [SerializeField]
    private MultiplayerEventSystem eventSystem;
    [SerializeField] private GameObject PauseFirstSelect, InventoryFirstSelect;

    //PLayer Animations
    [Header("Animations")]
    [SerializeField]
    private Animator playerAnimations;
    private bool isJumping;
    [SerializeField]
    private List<string> AnimationBools;

    //Player Outline
    [Header("Enemy Stats")]
    public Outline OtherPlayersOutline;
    public GameObject OtherPlayer;
    private PlayerController3D otherPlayersScript;
    public Outline myOutline;

    //Slide Stats
    [Header("Slide Stats")]
    public float forceAmount = 20f;
    public float forceDuration = 1f;
    private bool isRunning;
    private bool isSliding;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;

    //Seagull Settings
    [SerializeField] private SeagulScript seagullScript;
    private PlayerInput playerinput;


    [Header("Map Settings")]
    [SerializeField] private Transform PlayerMiniMap;
    [SerializeField] private Transform OpenMapCamera;
    [SerializeField] private Vector3 OpenMap, ClosedMap;
    [SerializeField] private Vector3 OpenMapPosition, ClosedMapPosition;
    [SerializeField] private Vector3 OpenMapCameraPosition, ClosedMapCameraPosition;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
        playerInput.defaultActionMap = "UI";
        Cursor.lockState = CursorLockMode.None;

        RunSpeed = speed * SpeedMultiplier;
        ShootScript = GetComponent<ShootManager>();
        // PausePanel.SetActive(false);

        SetSpawnPoint();
        playerinput = GetComponent<PlayerInput>();

       

    }

    // MOVEMENT
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        moveInput = new Vector3(input.x, 0f, input.y);
    }


    //Map Settings
    public void OnMapOpen(InputAction.CallbackContext context)
    {
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
    //Inventory System
    public void OnOpenInventorysystem(InputAction.CallbackContext context)
    {
        if (InventoryPanel.activeSelf)
        {
            InventoryPanel.SetActive(false);
            speed = 5;
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
        lookInput = context.ReadValue<Vector2>();
    }


    // Pause/Play
    public void PauseandPlay(InputAction.CallbackContext context)
    {
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
        if (context.performed && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            PlayJump();
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isRunning = true;
            speed = speed * SpeedMultiplier;
        }
        else if (context.canceled)
        {
            isRunning = false;
            speed = speed / SpeedMultiplier;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ShootScript.isRunning = true;
            ShootScript.isShooting = true;
            isShooting = true;
            ShootScript.OnShoot();

        }
        else if (context.canceled)
        {
            ShootScript.isRunning = false;
            ShootScript.isShooting = false;
            isShooting = false;
        }
    }

    public void OnScope(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerCam.fieldOfView = 10;
            lookSensitivity = 40;
            SniperScopeUi.SetActive(true);
        }
        else if (context.canceled)
        {
            playerCam.fieldOfView = 60;
            lookSensitivity = 120;
            SniperScopeUi.SetActive(false);

        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            Ray ray = new Ray(PlayerCamera.position, PlayerCamera.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 5f, Interact))
            {

            }
        }
    }

    public void OnSlide(InputAction.CallbackContext context)
    {
        if (isRunning)
        {
            if (context.canceled)
            {
                StartCoroutine(SlideAnimation());
            }
        }
    }

    IEnumerator SlideAnimation()
    {
        isSliding = true;
        if (playerinput.playerIndex == 0)
        {
            seagullScript.canAttckPlayer[0] = false;

        }
        else
        {
            seagullScript.canAttckPlayer[1] = false;

        }
        rb.AddForce(transform.forward * forceAmount, ForceMode.Force);
        yield return new WaitForSeconds(forceDuration);
       
        isSliding = false;

    }


    private void checkForOtherPlayer()
    {
        Ray ray = new Ray(PlayerCamera.position, PlayerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            if (hit.collider.CompareTag("Player1") || hit.collider.CompareTag("Player2"))
            {
                OtherPlayer = hit.collider.gameObject;
                if (OtherPlayer != null)
                {
                    otherPlayersScript = OtherPlayer.GetComponent<PlayerController3D>();
                    otherPlayersScript.myOutline.OutlineWidth = 10;
                }
                else if (OtherPlayer == null)
                {
                    OtherPlayersOutline.OutlineWidth = 10;
                }
            }
            else
            {
                otherPlayersScript = OtherPlayer.GetComponent<PlayerController3D>();
                otherPlayersScript.myOutline.OutlineWidth = 0;
                otherPlayersScript = null;
                OtherPlayer = null;
            }
        }
    }




    void FixedUpdate()
    {
        // FIXED: Changed from MovePosition to velocity-based movement for proper collision detection
        Vector3 moveDirection = transform.TransformDirection(moveInput).normalized;
        Vector3 targetVelocity = moveDirection * speed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity for jumping/gravity
        rb.linearVelocity = targetVelocity;

    }

    void LateUpdate()
    {
        // Horizontal rotation (player body)
        float mouseX = lookInput.x * lookSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation (camera)
        float mouseY = lookInput.y * lookSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookX, maxLookX);

        CameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);


        float pitch = xRotation;

        // Normalize pitch to 0–1
        float t = Mathf.InverseLerp(minLookX, maxLookX, pitch);

        // Zoom camera based on pitch
        float distance = Mathf.Lerp(minDistance, maxDistance, t);

        // Apply zoom
        PlayerCamera.localPosition = new Vector3(0f, 0f, distance);
        //Animations
        if (moveInput.x > 0 || moveInput.z > 0 || moveInput.x < 0 || moveInput.z < 0)
        {
            if (IsGrounded())
            {
                if (speed == RunSpeed)
                {
                    if (isSliding)
                    {
                        PlaySlide();
                    }
                    else if (!isSliding)
                    {
                        PlayRun();
                    }
                }
                else if (speed != RunSpeed)
                {

                    if (!isShooting)
                    {
                        PlayWalk();
                    }
                    else if (isShooting)
                    {
                        PlayWalkAndShoot();
                    }
                }
            }
            else if (!IsGrounded())
            {
                PlayJump();

            }
        }
        else if (moveInput.x == 0 && moveInput.z == 0)
        {
            if (IsGrounded())
            {

                if (!isShooting)
                {
                    PlayIdle();
                }
                else if (isShooting)
                {
                    playShoot();
                }
            }
            else if (!IsGrounded())
            {
                PlayJump();

            }
        }


    }

    void PlayJump()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
        playerAnimations.SetBool(AnimationBools[2], true);

    }

    void PlayWalk()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
        playerAnimations.SetBool(AnimationBools[0], true);
    }

    void PlaySlide()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
        playerAnimations.SetBool(AnimationBools[4], true);
    }

    void PlayRun()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
        playerAnimations.SetBool(AnimationBools[1], true);
    }

    void playShoot()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
        playerAnimations.SetBool(AnimationBools[3], true);
    }

    void PlayWalkAndShoot()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
        playerAnimations.SetBool(AnimationBools[5], true);
    }


    void PlayIdle()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            playerAnimations.SetBool(AnimationBools[i], false);
        }
    }
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    void SetSpawnPoint()
    {
        string spawnTag = "";

        if (playerInput.playerIndex == 0)
        {
            spawnTag = "P1Spawn";
        }
        else if (playerInput.playerIndex == 1)
        {
            spawnTag = "P2Spawn";
        }

        GameObject spawnPoint = GameObject.FindGameObjectWithTag(spawnTag);

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogWarning("Spawn point not found for tag: " + spawnTag);
        }
    }

}