using UnityEngine;

public class FrankaGripperLinearMover : MonoBehaviour
{
    [Header("Finger Transforms")]
    public Transform leftFinger;   // Assign panda_finger_joint1
    public Transform rightFinger;  // Assign panda_finger_joint2

    [Header("Position Settings (X Axis)")]
    public float leftOpenX = -0.0566f;
    public float leftClosedX = -0.0222f;

    public float rightOpenX = 0.0401f;
    public float rightClosedX = 0.0013f;

    public float moveSpeed = 0.05f; // meters per second

    private float leftTargetX;
    private float rightTargetX;
    private bool moving = false;

    void Start()
    {
        // Start in open position
        SetFingerX(leftFinger, leftOpenX);
        SetFingerX(rightFinger, rightOpenX);

        leftTargetX = leftOpenX;
        rightTargetX = rightOpenX;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            leftTargetX = leftOpenX;
            rightTargetX = rightOpenX;
            moving = true;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            leftTargetX = leftClosedX;
            rightTargetX = rightClosedX;
            moving = true;
        }

        if (moving)
        {
            // Move both fingers toward targets
            Vector3 leftPos = leftFinger.localPosition;
            Vector3 rightPos = rightFinger.localPosition;

            leftPos.y = Mathf.MoveTowards(leftPos.y, leftTargetX, moveSpeed * Time.deltaTime);
            rightPos.y = Mathf.MoveTowards(rightPos.y, rightTargetX, moveSpeed * Time.deltaTime);

            leftFinger.localPosition = leftPos;
            rightFinger.localPosition = rightPos;

            // Stop when both fingers reach target
            if (Mathf.Approximately(leftPos.y, leftTargetX) && Mathf.Approximately(rightPos.y, rightTargetX))
                moving = false;
        }
    }

    void SetFingerX(Transform finger, float x)
    {
        Vector3 pos = finger.localPosition;
        pos.y = x;
        finger.localPosition = pos;
    }
}
