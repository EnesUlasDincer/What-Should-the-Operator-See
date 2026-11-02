using UnityEngine;

public class PointCloudMover : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotSpeed =  10f;

    private bool controlsEnabled = true;

    void Update()
    {
        HandleToggle();

        if (controlsEnabled)
        {
            HandleTranslation();
            HandleRotation();
        }
    }

    void HandleToggle()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            controlsEnabled = !controlsEnabled;
            Debug.Log("Controls Enabled: " + controlsEnabled);
        }
    }

    void HandleRotation()
    {
        // Rotate around X axis
        if (OVRInput.Get(OVRInput.Button.Two))  // Y button
            transform.Rotate(Vector3.right * -rotSpeed * Time.deltaTime, Space.World);
        if (OVRInput.Get(OVRInput.Button.One))  // B button
            transform.Rotate(Vector3.right * rotSpeed * Time.deltaTime, Space.World);

        // Rotate around Y axis
        if (OVRInput.Get(OVRInput.Button.Three))  // X button
            transform.Rotate(Vector3.up * -rotSpeed * Time.deltaTime, Space.World);
        if (OVRInput.Get(OVRInput.Button.Four))   // A button
            transform.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);

        // Rotate around Z axis
        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick)) // Right stick press
            transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime, Space.World);
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick))   // Left stick press
            transform.Rotate(Vector3.forward * -rotSpeed * Time.deltaTime, Space.World);
    }

    void HandleTranslation()
    {
        // Move in XY plane using Secondary (right) thumbstick
        Vector2 thumb = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector3 move = new Vector3(thumb.x, 0, thumb.y);

        // Move up/down using Secondary controller (right)
        if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger)) // Index trigger = up
            move.y += 1;
        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))  // Grip button = down
            move.y -= 1;

        // Apply movement
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
