//using UnityEngine;

//public class MoveQuadWithKeys : MonoBehaviour
//{
//    public float speed = 1f; // Movement speed

//    void Update()
//    {
//        // Get input from arrow keys or WASD
//        float moveX = Input.GetAxis("Horizontal"); // Left (-1) / Right (+1) → A/D or Left/Right Arrow
//        float moveZ = Input.GetAxis("Vertical");   // Forward (+1) / Backward (-1) → W/S or Up/Down Arrow

//        // Move inside/outside using Q & E keys (Y-axis movement)
//        float moveY = 0;
//        if (Input.GetKey(KeyCode.E)) moveY = 1;  // Move Up (inside)
//        if (Input.GetKey(KeyCode.Q)) moveY = -1; // Move Down (outside)

//        // Update the position
//        transform.position += new Vector3(moveX, moveY, moveZ) * speed * Time.deltaTime;
//    }
//}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveQuadWithController : MonoBehaviour
{
    float speed = 2f;  // Movement speed

    void Update()
    {
        // Get controller thumbstick input (Oculus Touch/Quest controllers)
        float moveX = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x; // Left/Right
        float moveZ = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y; // Forward/Backward

        // Move up/down using grip buttons
        float moveY = 0;
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)) moveY = 1;  // Grip Button (Up)
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger)) moveY = -1; // Grip Button (Down)

        // Apply movement
        transform.position += new Vector3(moveX, moveY, moveZ) * speed * Time.deltaTime;
    }
}

