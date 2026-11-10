using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerMovement : MonoBehaviour
{
    public static bool isMenuOpen = false;
    public float walkSpeed = 6f;
    public float sprintSpeed = 10.5f;
    public float crouchSpeed = 3.2f;
    public float sprintStaminaDrain = 22f;
    public float mouseSensitivity = 0.15f;
    public float jumpHeight = 1.6f;
    public float gravity = -19.62f;
    public Transform cameraTransform;

    [Header("Crouch Settings (Left Ctrl)")]
    public float standingHeight = 2.0f;
    public float crouchingHeight = 1.1f;
    public float standingCamY = 0.6f;
    public float crouchingCamY = 0.15f;

    [HideInInspector] public bool isSprinting = false;
    [HideInInspector] public bool isCrouching = false;

    private CharacterController controller;
    private PlayerInventory inventory;
    private PlayerCombatAndInteract combat;
    private float verticalVelocity;
    private float xRotation = 0f;
    private float groundedTimer = 0f;
    private int airJumpCount = 0;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<PlayerInventory>();
        combat = GetComponent<PlayerCombatAndInteract>();
        controller.minMoveDistance = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isSprinting = false;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Mouse.current != null && cameraTransform != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation - mouseDelta.y, -85f, 85f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseDelta.x);
        }

        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
        }

        /* Obsluga kucania na Left Ctrl (oraz Right Ctrl) */
        bool wantsToCrouch = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
        if (wantsToCrouch)
        {
            isCrouching = true;
        }
        else if (isCrouching)
        {
            /* Sprawdz czy nad glowa nie ma niskiego sufitu przed wstaniem */
            bool ceilingBlocked = Physics.Raycast(transform.position, Vector3.up, 1.1f);
            if (!ceilingBlocked) isCrouching = false;
        }

        /* Plynna zmiana wysokosci kapsuly i kamery */
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        float prevHeight = controller.height;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 12f);
        transform.position += new Vector3(0f, (controller.height - prevHeight) * 0.5f, 0f);

        if (cameraTransform != null)
        {
            float targetCamY = isCrouching ? crouchingCamY : standingCamY;
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * 12f);
            cameraTransform.localPosition = camPos;
        }

        /* Predkosc ruchu: Kucanie vs Sprint vs Chodzenie */
        bool wantsToSprint = !isCrouching && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && moveInput.sqrMagnitude > 0.01f;
        isSprinting = false;
        float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;

        if (wantsToSprint && combat != null && combat.currentStamina > 1f)
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
            combat.UseStamina(sprintStaminaDrain * Time.deltaTime);
        }

        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * currentSpeed;

        float groundRayDist = (controller.height * 0.5f) + 0.2f;
        bool hitGround = controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, groundRayDist);
        if (hitGround)
        {
            groundedTimer = 0.2f;
            airJumpCount = 0;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            groundedTimer -= Time.deltaTime;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (groundedTimer > 0f)
            {
                isCrouching = false;
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                groundedTimer = 0f;
            }
            else if (inventory != null && inventory.CanDoubleJump() && airJumpCount < 1)
            {
                verticalVelocity = Mathf.Sqrt((jumpHeight * 1.15f) * -2f * gravity);
                airJumpCount++;
                if (combat != null) combat.ShowCombatText("DOUBLE JUMP! (Sandaly Pijaka)");
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }
}
