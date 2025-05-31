using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;
    
    [Header("Camera Settings")]
    public Transform cameraTransform; // Drag your Main Camera here
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;
    public LayerMask obstacleLayerMask = -1; // What objects can block the camera
    
    // Private variables
    private CharacterController characterController;
    private Animator animator;
    private Vector3 moveDirection;
    private bool isRunning;
    private bool isWalking;
    
    // Camera variables
    private float mouseX;
    private float mouseY;
    private float currentCameraDistance;
    
    // Animation hashes
    private int isWalkingHash;
    private int isRunningHash;
    
    void Start()
    {
        // Get components
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Check if required components exist
        if (characterController == null)
        {
            Debug.LogError("CharacterController component is missing! Please add a CharacterController to " + gameObject.name);
            enabled = false;
            return;
        }
        
        if (animator == null)
        {
            Debug.LogError("Animator component is missing! Please add an Animator to " + gameObject.name);
            enabled = false;
            return;
        }
        
        // Get animation hashes
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        
        // Auto-find camera if not assigned
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        
        // Lock cursor and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize camera distance
        currentCameraDistance = cameraDistance;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleAnimations();
        HandleCameraPosition();
        HandleCursorToggle();
    }
    
    void HandleMouseLook()
    {
        // Get mouse input
        float mouseXInput = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseYInput = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Update mouse look values
        mouseX += mouseXInput;
        mouseY -= mouseYInput; // Inverted for standard FPS controls
        
        // Clamp vertical look
        mouseY = Mathf.Clamp(mouseY, -verticalLookLimit, verticalLookLimit);
    }
    
    void HandleCameraPosition()
    {
        if (cameraTransform == null) return;
        
        // Calculate desired camera position
        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight;
        
        // Calculate camera rotation
        Quaternion cameraRotation = Quaternion.Euler(mouseY, mouseX, 0);
        
        // Calculate camera position behind character
        Vector3 desiredCameraPosition = targetPosition - (cameraRotation * Vector3.forward * cameraDistance);
        
        // Check for obstacles between character and camera
        RaycastHit hit;
        Vector3 direction = desiredCameraPosition - targetPosition;
        
        if (Physics.Raycast(targetPosition, direction.normalized, out hit, cameraDistance, obstacleLayerMask))
        {
            currentCameraDistance = Mathf.Lerp(currentCameraDistance, hit.distance - 0.2f, Time.deltaTime * 5f);
        }
        else
        {
            currentCameraDistance = Mathf.Lerp(currentCameraDistance, cameraDistance, Time.deltaTime * 2f);
        }
        
        // Apply final camera position and rotation
        Vector3 finalCameraPosition = targetPosition - (cameraRotation * Vector3.forward * currentCameraDistance);
        cameraTransform.position = finalCameraPosition;
        cameraTransform.LookAt(targetPosition);
    }
    
    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool runPressed = Input.GetKey(KeyCode.LeftShift);
        
        // Calculate camera-relative direction using the horizontal rotation only
        Vector3 cameraForward = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        Vector3 cameraRight = new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z).normalized;
        
        // Calculate movement direction relative to camera
        moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        // Check if character is moving
        bool movementPressed = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        
        // Determine movement state
        isWalking = movementPressed;
        isRunning = movementPressed && runPressed;
        
        // Move the character
        if (movementPressed)
        {
            // Choose speed based on running state
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            
            // Apply movement
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
            
            // Rotate character to face movement direction
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        // Apply gravity
        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }
    
    void HandleAnimations()
    {
        // Get current animation states
        bool currentlyWalking = animator.GetBool(isWalkingHash);
        bool currentlyRunning = animator.GetBool(isRunningHash);
        
        // Update walking animation
        if (!currentlyWalking && isWalking)
            animator.SetBool(isWalkingHash, true);
        if (currentlyWalking && !isWalking)
            animator.SetBool(isWalkingHash, false);
        
        // Update running animation
        if (!currentlyRunning && isRunning)
            animator.SetBool(isRunningHash, true);
        if (currentlyRunning && !isRunning)
            animator.SetBool(isRunningHash, false);
    }
    
    void HandleCursorToggle()
    {
        // Press Escape to toggle cursor lock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        // Click to lock cursor again when it's unlocked
        if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}