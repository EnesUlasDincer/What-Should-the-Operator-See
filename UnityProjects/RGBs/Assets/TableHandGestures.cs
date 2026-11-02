using UnityEngine;
using TMPro;

public class TableHandGestures : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;                         // The hand being tracked (Left or Right)
    public OVRSkeleton handSkeleton;            // The skeleton for finger joint positions
    public GameObject targetObject;             // Object to activate/deactivate based on gesture

    void Update()
    {
        if (hand == null || handSkeleton == null) return;

        if (IsPeaceSign())
        {
            targetObject.SetActive(true);
        }
        else if (IsRockSign())
        {
            targetObject.SetActive(false);
        }
    }

    bool IsRockSign()
    {
        return
            IsFingerExtended(OVRSkeleton.BoneId.Hand_IndexTip, OVRSkeleton.BoneId.Hand_Index1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_MiddleTip, OVRSkeleton.BoneId.Hand_Middle1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_RingTip, OVRSkeleton.BoneId.Hand_Ring1) &&
            IsFingerExtended(OVRSkeleton.BoneId.Hand_PinkyTip, OVRSkeleton.BoneId.Hand_Pinky1);
    }

    bool IsPeaceSign()
    {
        return
            IsFingerExtended(OVRSkeleton.BoneId.Hand_IndexTip, OVRSkeleton.BoneId.Hand_Index1) &&
            IsFingerExtended(OVRSkeleton.BoneId.Hand_MiddleTip, OVRSkeleton.BoneId.Hand_Middle1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_RingTip, OVRSkeleton.BoneId.Hand_Ring1) &&
            IsFingerCurled(OVRSkeleton.BoneId.Hand_PinkyTip, OVRSkeleton.BoneId.Hand_Pinky1);
    }

    bool IsFingerExtended(OVRSkeleton.BoneId tip, OVRSkeleton.BoneId baseJoint)
    {
        if (handSkeleton.Bones.Count == 0) return false;

        Vector3 tipPos = handSkeleton.Bones[(int)tip].Transform.position;
        Vector3 basePos = handSkeleton.Bones[(int)baseJoint].Transform.position;

        float distance = Vector3.Distance(tipPos, basePos);

        return distance > 0.05f; // Threshold: larger distance = extended
    }

    bool IsFingerCurled(OVRSkeleton.BoneId tip, OVRSkeleton.BoneId baseJoint)
    {
        if (handSkeleton.Bones.Count == 0) return false;

        Vector3 tipPos = handSkeleton.Bones[(int)tip].Transform.position;
        Vector3 basePos = handSkeleton.Bones[(int)baseJoint].Transform.position;

        float distance = Vector3.Distance(tipPos, basePos);

        return distance < 0.045f; // Threshold: smaller distance = curled
    }

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
}

