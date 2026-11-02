using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveQuadWithController : MonoBehaviour
{
    float speed = 2f;         // Movement speed
    bool isLocked = true;     // Start locked

    void Update()
    {
        // Toggle lock/unlock when A button is pressed
        if (OVRInput.GetDown(OVRInput.Button.One)) // A button on right controller
        {
            isLocked = !isLocked;
            Debug.Log(isLocked ? "Movement Locked" : "Movement Unlocked");
        }

        // If locked, ignore movement input
        if (isLocked) return;

        // Get controller thumbstick input
        Vector2 stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        float moveX = stickInput.x; // Left/Right
        float moveZ = stickInput.y; // Forward/Backward

        // Move up/down using grip buttons
        float moveY = 0;
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)) moveY = 1;  // Up
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger)) moveY = -1; // Down

        // Apply movement
        transform.position += new Vector3(moveX, moveY, moveZ) * speed * Time.deltaTime;
    }
}
