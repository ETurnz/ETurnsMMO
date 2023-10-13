using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;
    CameraController cameraController;

    NetworkManager networkManager;

    private int currentZoneID = -1;
    public bool isRunning = false;

    private float movementSpeed = 5f;
    private float turnSpeed = 15f;
    private float rollSpeed = 5.0f;
    private float rollDuration;
    private float mouseTurnSpeed = 4.0f;

    public float forwardBackward = 0f;
    public float leftRight = 0f;

    private Animator animator;
    private Rigidbody rb;
    private Vector3 moveDirection;
    public Vector3 lastMoveDirection = Vector3.zero;
    public Vector3 playerForwardDirection = Vector3.forward;
    private Quaternion targetRotation;
    private float rotationSpeed = 5f; // Speed of rotation. Adjust as needed.
    private bool shouldRotate = false;
    private bool isRolling = false;
    private bool isTurning = false;
    private bool isStrafingLeft = false;
    private bool isStrafingRight = false;
    private bool isSendingData = false;
    private bool rightClick = false;
    private bool mouseRunning = false;
    private float rollStartTime;
    private bool hasSentStopSignal = false;
    private bool firstFrame = true;

    private const float ZONE_UPDATE_DELAY = 0.5f; // Delay of 0.5 seconds
    private Coroutine zoneUpdateCoroutine;
    private bool isZoneUpdatePending = false;
    private bool shouldRoll = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        cameraController = cameraTransform.GetComponent<CameraController>();
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();

        rollDuration = animator.runtimeAnimatorController.animationClips.FirstOrDefault(clip => clip.name == "RollForward").length;
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Lock the Rigidbody rotation on the x and z axes
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        forwardBackward = 0;
        leftRight = 0;
        isTurning = false;
        isStrafingLeft = false;
        isStrafingRight = false;
        mouseRunning = false;
        rightClick = false;

        if (!Input.GetMouseButton(1))
        {
            firstFrame = true;
        }
        if (Input.GetMouseButton(1) && !Input.GetMouseButton(0) && !isRolling) // Right mouse button is held down
        {
            rightClick = true;
            TurnWithMouse();
        }
        else if (Input.GetMouseButton(0) && Input.GetMouseButton(1) &&!isRolling)
        {
            if (firstFrame)
            {
                Vector3 cameraForwardFlat = cameraTransform.forward;
                cameraForwardFlat.y = 0;
                playerForwardDirection = cameraForwardFlat.normalized;
                firstFrame = false;
            }

            mouseRunning = true;
            forwardBackward = 1f;
            TurnWithMouse();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            shouldRoll = true;
        }

        HandleInput();
    }

    private void FixedUpdate()
    {
        SmoothRotation();
        UpdateOrientation();
        HandleMovement();
        HandleRoll();
    }

    private void HandleInput()
    {
        if (isRolling)
        {
            return;
        }

        if (Input.GetKey(KeyCode.W))
        {
            forwardBackward += 1f;
        }
        if (Input.GetKey(KeyCode.S) && !mouseRunning)
        {
            forwardBackward -= 1f;
        }

        if (Input.GetKey(KeyCode.A) && !mouseRunning)
        {
            leftRight -= 1f;
        }
        if (Input.GetKey(KeyCode.D) && !mouseRunning)
        {
            leftRight += 1f;
        }

        moveDirection = playerForwardDirection * forwardBackward + Vector3.Cross(Vector3.up, playerForwardDirection) * leftRight;
        moveDirection.Normalize();

        if (moveDirection != Vector3.zero)
        {
            lastMoveDirection = moveDirection;
        }
    }

    private void TurnWithMouse()
    {
        isTurning = true;
        float mouseX = Input.GetAxis("Mouse X");
        float turnAmount = mouseX * mouseTurnSpeed;
        playerForwardDirection = Quaternion.Euler(0, turnAmount, 0) * playerForwardDirection;
        
        cameraController.TurnCamera(turnAmount);
    }

    private void UpdateOrientation()
    {
        Vector3 desiredFacingDirection;

        // Determine if we're only strafing (only A or D is pressed without W or S)
        bool isStrafing = (leftRight != 0) && (forwardBackward == 0);

        if ((isStrafing) || (rightClick && forwardBackward == 0 && leftRight == 0))
        {
            // Face the playerForwardDirection when strafing
            desiredFacingDirection = playerForwardDirection;
        }
        else
        {
            // If not strafing, face the movement direction
            desiredFacingDirection = playerForwardDirection * forwardBackward + Vector3.Cross(Vector3.up, playerForwardDirection) * leftRight;
            desiredFacingDirection.y = 0;
        }

        if (desiredFacingDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredFacingDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            isStrafingLeft = (leftRight < 0) && (forwardBackward == 0);
            isStrafingRight = (leftRight > 0) && (forwardBackward == 0);

            // Check if player is moving forward or backward (either W or S pressed)
            bool isMovingForwardOrBackward = forwardBackward != 0;

            animator.SetBool("IsRunningForward", isMovingForwardOrBackward);

            // Set Strafing Animations
            animator.SetBool("IsStrafingLeft", isStrafingLeft);
            animator.SetBool("IsStrafingRight", isStrafingRight);

            // Update isRunning variable based on movement
            isRunning = (forwardBackward != 0) || (leftRight != 0);

            Vector3 movementDirection = playerForwardDirection * forwardBackward + Vector3.Cross(Vector3.up, playerForwardDirection) * leftRight;

            movementDirection.Normalize();
            Vector3 movement = movementDirection * movementSpeed;
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
            hasSentStopSignal = false;
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);

            // Reset animations if no movement
            animator.SetBool("IsRunningForward", false);
            animator.SetBool("IsStrafingLeft", false);
            animator.SetBool("IsStrafingRight", false);

            if (isRunning && !hasSentStopSignal)
            {
                isRunning = false;

                networkManager.SendPositionUpdate(transform.position, transform.rotation, isRunning, isRolling, isStrafingLeft, isStrafingRight);
                hasSentStopSignal = true;
            }
        }

        if ((isRunning || isTurning || isRolling) && !isSendingData)
        {
            StartCoroutine(SendPositionUpdates());
        }
    }

    private void HandleRoll()
    {
        Vector3 rollDirection = lastMoveDirection;

        // If no movement was done recently, use the player's forward direction as a fallback
        if (rollDirection == Vector3.zero)
        {
            rollDirection = transform.forward;
        }

        // Ensure the direction is normalized
        rollDirection.Normalize();

        // Only proceed if the roll key is pressed, player is not currently rolling, and a valid roll direction was chosen
        if (shouldRoll)
        {
            if (!isRolling && rollDirection != Vector3.zero)
            {
                // Check if player is strafing, and if so, update their orientation immediately to match the strafing direction
                if (isStrafingLeft)
                {
                    targetRotation = Quaternion.LookRotation(-Vector3.Cross(Vector3.up, playerForwardDirection));
                }
                else if (isStrafingRight)
                {
                    targetRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, playerForwardDirection));
                }
                else
                {
                    targetRotation = Quaternion.LookRotation(rollDirection);
                }

                shouldRotate = true;

                animator.SetBool("IsRolling", true);
                isRolling = true;
                rollStartTime = Time.time;

                // Update lastMoveDirection with the current roll direction
                lastMoveDirection = rollDirection;
            }

            shouldRoll = false;
        }
        

        if (isRolling)
        {
            Vector3 rollVelocity = lastMoveDirection * rollSpeed;
            rb.velocity = new Vector3(rollVelocity.x, rb.velocity.y, rollVelocity.z);

            if (Time.time > rollStartTime + rollDuration)
            {
                isRolling = false;
                animator.SetBool("IsRolling", false);

                networkManager.SendPositionUpdate(transform.position, transform.rotation, isRunning, isRolling, isStrafingLeft, isStrafingRight);
            }
        }
    }

    private void SmoothRotation()
    {
        if (shouldRotate)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f) // Close enough to the target rotation
            {
                transform.rotation = targetRotation;  // Snap to the target rotation
                shouldRotate = false;
            }
        }
    }

    IEnumerator SendPositionUpdates()
    {
        isSendingData = true;

        while (isRunning || isTurning || isRolling)
        {
            networkManager.SendPositionUpdate(transform.position, transform.rotation, isRunning, isRolling, isStrafingLeft, isStrafingRight);
            yield return new WaitForSeconds(1f / 30f); // 30Hz
        }

        isSendingData = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check the name of the loaded scene
        if (scene.name == "game")
        {
            networkManager.SendPositionUpdate(transform.position, transform.rotation, isRunning, isRolling, isStrafingLeft, isStrafingRight);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Zone zone = other.GetComponent<Zone>();
        if (zone != null && currentZoneID != zone.zoneID)
        {
            currentZoneID = zone.zoneID;
            // If an update is already pending, just update the current zone ID and the coroutine will handle it
            if (!isZoneUpdatePending)
            {
                if (zoneUpdateCoroutine != null)
                {
                    StopCoroutine(zoneUpdateCoroutine);
                }
                zoneUpdateCoroutine = StartCoroutine(DelayedZoneUpdate());
            }
        }
        else
        {
            Debug.LogError(currentZoneID);
        }
    }

    private IEnumerator DelayedZoneUpdate()
    {
        isZoneUpdatePending = true;
        yield return new WaitForSeconds(ZONE_UPDATE_DELAY);
        networkManager.UpdateZone(currentZoneID);
        Debug.Log("Updated to zone " + currentZoneID);
        isZoneUpdatePending = false;
    }

    private void OnTriggerExit(Collider other)
    {
        Zone zone = other.GetComponent<Zone>();
        if (zone != null)
        {
            networkManager.LeaveZone(zone.zoneID);
            Debug.Log("Leaving zone " + zone.zoneID);
        }
    }

    /*private void SetDirectionInstantly(Vector3 direction)
    {
        if (direction.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, targetAngle, 0);
        }
    }*/
}
