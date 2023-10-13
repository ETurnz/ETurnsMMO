using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public LayerMask playerLayer;
    public LayerMask uiLayer;
    Camera camera;
    public PlayerMovement playerMovement;
    public Transform target;
    public GameMenu gameMenu;

    public float distance = 10f; // distance from the target
    public float height = 3f;   // height from the target's position
    public float smoothSpeed = 1f;
    public float zoomSpeed = 2.0f;
    public float maxZoom = 10.0f;
    public float minZoom = 2.0f;
    public float rotationSensitivity = 5f; // Adjust this value as needed
    private float currentAngle = 0f; // angle around the target
   
    private void Start()
    {
        camera = GetComponent<Camera>();
        //GameObject.Find("Canvas").GetComponent<Canvas>().worldCamera = this.gameObject.GetComponent<Camera>();
    }

    private void Update()
    {
        float scrollData = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scrollData * zoomSpeed, minZoom, maxZoom);

        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1)) // 0 is the left mouse button
        {
            float horizontalMovement = Input.GetAxis("Mouse X") * rotationSensitivity;
            currentAngle += horizontalMovement;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastLeftClick();
        }
        if (Input.GetMouseButtonDown(1))
        {
            //RaycastRightClick();
        }
    }

    private void LateUpdate()
    {
        
        if ((playerMovement.isRunning && !IsCameraBehindPlayer() && !Input.GetMouseButton(0) && !Input.GetMouseButton(1)) || 
            (Input.GetMouseButton(0) && Input.GetMouseButton(1) && !IsCameraBehindPlayer()))
        {
            // Calculate target angle based on the player's forward direction
            float targetAngle = Mathf.Atan2(playerMovement.playerForwardDirection.x, playerMovement.playerForwardDirection.z) * Mathf.Rad2Deg;

            // Define the portion of the rotation to apply each frame
            float t = 0.1f;  // 10% of the rotation towards the target applied each frame. Adjust this value as needed.

            // Interpolate currentAngle towards targetAngle
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, t);
        }

        // Convert angle and distance to position offset. 
        // Note that we are subtracting the currentAngle from 180 to put the camera behind the player.
        Vector3 offset = new Vector3(
            Mathf.Sin((currentAngle - 180) * Mathf.Deg2Rad) * distance,
            height,
            Mathf.Cos((currentAngle - 180) * Mathf.Deg2Rad) * distance
        );

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target.position + Vector3.up);
    }

    public void TurnCamera(float turn)
    {
        currentAngle += turn;
    }

    private bool IsCameraBehindPlayer()
    {
        float cameraDirectionAngle = currentAngle - 180; // Convert from your "behind" logic to a forward direction
        float playerDirectionAngle = Mathf.Atan2(playerMovement.playerForwardDirection.x, playerMovement.playerForwardDirection.z) * Mathf.Rad2Deg;

        float angleDifference = Mathf.DeltaAngle(cameraDirectionAngle, playerDirectionAngle);

        // Check if the angle difference is within a threshold and not around 0
        return Mathf.Abs(angleDifference) < 5.0f && Mathf.Abs(angleDifference) > 5.0f;
    }

    void RaycastLeftClick()
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        // Perform raycast
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, playerLayer))
        {
            // Check if we hit a player
            NetworkCharacter networkCharacter = hitInfo.collider.GetComponent<NetworkCharacter>();
            if (networkCharacter != null)
            {
                gameMenu.EnableTargetFrame(networkCharacter.charName);  
            } 
        }
    }

    void RaycastRightClick()
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        // Perform raycast
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, uiLayer))
        {
            Debug.LogError(hitInfo.transform.name);
            //if (hitInfo.transform.name == "")
        }
    }
}
