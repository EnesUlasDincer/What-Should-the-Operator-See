using System.Diagnostics;
using TMPro;
using UnityEngine;

public class videoScreenHandGesture: MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;                   // The hand being tracked (Left or Right)
    public OVRSkeleton handSkeleton;      // The skeleton for finger joint positions
    public GameObject targetObject;       // Object to activate/deactivate based on gesture
    public TextMeshProUGUI gestureDebugText;  // On-screen text showing finger states

    public PointCloudTest pointcloud_renderEnabled;

    // Counter variables
    private int thumbsUpCount = 0;
    private int thumbsDownCount = 0;

    void Update()
    {
        if (hand == null || handSkeleton == null || gestureDebugText == null) return;

        if (IsThumbsUp())
        {
            targetObject.SetActive(true);

            if (pointcloud_renderEnabled != null)
            {
                // Enable point cloud rendering
                pointcloud_renderEnabled.renderEnabled = true;
            }

            thumbsUpCount++;  // Increment the thumbs up count

            // Take action if thumbs up count exceeds 5
            if (thumbsUpCount > 5)
            {
                TakeActionForThumbsUp();
            }

            gestureDebugText.text = $"Gesture: Thumbs Up ({thumbsUpCount} times)\n" + GetFingerStatesText();
        }
        else if (IsThumbsDown())
        {
            targetObject.SetActive(false);

            if (pointcloud_renderEnabled != null)
            {
                // Disable point cloud rendering
                pointcloud_renderEnabled.renderEnabled = false;
            }

            thumbsDownCount++;  // Increment the thumbs down count

            // Take action if thumbs down count exceeds 5
            if (thumbsDownCount > 5)
            {
                TakeActionForThumbsDown();
            }

            gestureDebugText.text = $"Gesture: Thumbs Down ({thumbsDownCount} times)\n" + GetFingerStatesText();
        }
        else
        {
            gestureDebugText.text = "Gesture: Unknown\n" + GetFingerStatesText();
        }
    }

    bool IsThumbsUp()
    {
        return IsFist() && IsThumbExtended() &&
               Vector3.Dot(GetThumbDirection().normalized, Vector3.up) > 0.6f;
    }

    bool IsThumbsDown()
    {
        return IsFist() && IsThumbExtended() &&
               Vector3.Dot(GetThumbDirection().normalized, Vector3.down) > 0.6f;
    }

    Vector3 GetThumbDirection()
    {
        var thumbTip = handSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
        var thumbBase = handSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Thumb1].Transform.position;
        return thumbTip - thumbBase; // Vector pointing from base to tip
    }

    bool IsThumbExtended()
    {
        var tip = handSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
        var baseJoint = handSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Thumb1].Transform.position;

        float distance = Vector3.Distance(tip, baseJoint);
        return distance > 0.04f; // Adjust threshold based on hand scale
    }

    // Returns true if the index finger is pinching (thumb touching index)
    bool IsIndexPinch()
    {
        return hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
    }

    // Returns true if all fingers are curled (a fist)
    // This uses bone distances between fingertip and base joint
    bool IsFist()
    {
        return
            IsFingerCurled(OVRSkeleton.BoneId.Hand_IndexTip, OVRSkeleton.BoneId.Hand_Index1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_MiddleTip, OVRSkeleton.BoneId.Hand_Middle1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_RingTip, OVRSkeleton.BoneId.Hand_Ring1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_PinkyTip, OVRSkeleton.BoneId.Hand_Pinky1);
    }

    // Helper method to check if a finger is curled (based on distance between tip and base)
    bool IsFingerCurled(OVRSkeleton.BoneId tip, OVRSkeleton.BoneId baseJoint)
    {
        if (handSkeleton.Bones.Count == 0) return false;

        Vector3 tipPos = handSkeleton.Bones[(int)tip].Transform.position;
        Vector3 basePos = handSkeleton.Bones[(int)baseJoint].Transform.position;

        float distance = Vector3.Distance(tipPos, basePos);

        return distance < 0.05f; // Threshold: smaller distance = more curled
    }

    // Returns a string with the state of each finger for debug display
    string GetFingerStatesText()
    {
        string text = "Finger States:\n";

        foreach (OVRHand.HandFinger finger in System.Enum.GetValues(typeof(OVRHand.HandFinger)))
        {
            bool isPinching = hand.GetFingerIsPinching(finger);
            OVRHand.TrackingConfidence confidence = hand.GetFingerConfidence(finger);

            text += $"{finger}: {(isPinching ? "Pinching" : "Not Pinching")} (Confidence: {confidence})\n";
        }

        return text;
    }

    // Action when Thumbs Up gesture count exceeds 5
    void TakeActionForThumbsUp()
    {
        // Example action: Log a message to console
        //Debug.Log("Thumbs Up Gesture performed more than 5 times!");

        // Add any other action you want here, such as triggering an animation, event, etc.
    }

    // Action when Thumbs Down gesture count exceeds 5
    void TakeActionForThumbsDown()
    {
        // Example action: Log a message to console
        //Debug.Log("Thumbs Down Gesture performed more than 5 times!");

        // Add any other action you want here, such as triggering an animation, event, etc.
    }
}
