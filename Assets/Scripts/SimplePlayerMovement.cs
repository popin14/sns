using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerMovement : MonoBehaviour
{
    public static bool isMenuOpen = false;
    public float walkSpeed = 6f;
    public float mouseSensitivity = 0.15f;
    public float jumpHeight = 1.6f;
    public float gravity = -19.62f;
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalVelocity;
    private float xRotation = 0f;
    private float groundedTimer = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
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

        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * walkSpeed;

        bool hitGround = controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, 1.15f);
        if (hitGround)
        {
            groundedTimer = 0.2f;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            groundedTimer -= Time.deltaTime;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && groundedTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            groundedTimer = 0f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }
}
