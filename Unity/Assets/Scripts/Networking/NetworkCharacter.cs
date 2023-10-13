using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCharacter : MonoBehaviour
{
    Animator animator;
    public string charName;
    public int zoneID;

    private float lerpSpeed = 5.0f;
    private float turnSpeed = 180f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private bool shouldLerp = false;
    bool IsRunning = false;
    bool IsRolling = false;
    bool IsStrafingLeft = false;
    bool IsStrafingRight = false;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldLerp)
        {
            // Lerp position and rotation towards the target values
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

            // Check if the current position and rotation are close enough to the target values
            float positionDifference = Vector3.Distance(transform.position, targetPosition);
            float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);

            if (positionDifference < 0.01f && rotationDifference < 0.1f)
            {
                // Set position and rotation directly to the target values to avoid tiny discrepancies
                transform.position = targetPosition;
                transform.rotation = targetRotation;

                // Stop lerping
                shouldLerp = false;
            }
        }
        else
        {
            //Debug.LogError("No movement target");
        }
    }

    public void SetTarget(Vector3 position, Quaternion rotation)
    {
        
        targetPosition = position;
        targetRotation = rotation;
        shouldLerp = true;
        //transform.position = position;
        //transform.rotation = rotation;
    }

    public void SetRunning(bool run)
    {
        if ((run && !IsRunning) || (!run && IsRunning))
        {
            animator.SetBool("IsRunningForward", run);
            IsRunning = run;
        }
    }

    public void SetRolling(bool roll)
    {
        if ((roll && !IsRolling) || (!roll && IsRolling))
        {
            animator.SetBool("IsRolling", roll);
            IsRolling = roll;
        }
    }

    public void SetStrafing(bool left, bool right)
    {
        if ((left && !IsStrafingLeft) || (!left && IsStrafingLeft))
        {
            animator.SetBool("IsStrafingLeft", left);
            IsStrafingLeft = left;
        }

        if ((right && !IsStrafingRight) || (!right && IsStrafingRight))
        {
            animator.SetBool("IsStrafingRight", right);
            IsStrafingRight = right;
        }
    }
}
