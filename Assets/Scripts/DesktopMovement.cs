using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DesktopMovement : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("State")]
    public bool controlsEnabled = false;   // <-- VARIABILA LIPSEA

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 2.0f;
    public float maxLookUp = 85f;

    CharacterController _cc;
    float _verticalVelocity;
    float _pitch;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;

        // Start in MENU mode
        SetCursorLocked(false);
    }

    public void EnableControls(bool enable)
    {
        controlsEnabled = enable;
        SetCursorLocked(enable);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        // Debug toggle (ESC)
        if (Input.GetKeyDown(KeyCode.Escape))
            EnableControls(!controlsEnabled);

        if (!controlsEnabled || cameraTransform == null)
            return;

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -maxLookUp, maxLookUp);
        cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // WASD move
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized * moveSpeed;

        // Gravity
        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = new Vector3(move.x, _verticalVelocity, move.z);
        _cc.Move(velocity * Time.deltaTime);
    }
}
