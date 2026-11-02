using UnityEngine;

public class RaycastObjectController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotSpeed = 10f;
    public Transform rightHand; // Assign the right hand/controller transform in inspector

    private GameObject selectedObject;
    private bool controlsEnabled = false;

    void Update()
    {
        HandleSelection();

        if (controlsEnabled && selectedObject != null)
        {
            HandleTranslation();
            HandleRotation();
        }

        HandleToggle();
    }

    void HandleSelection()
    {
        // Raycast when trigger is pressed
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            Ray ray = new Ray(rightHand.position, rightHand.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                selectedObject = hit.collider.gameObject;
                Debug.Log("Selected: " + selectedObject.name);
                if(selectedObject.name == "CubeSelection")
                    selectedObject = GameObject.Find("PointCloudTest");
            }
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

    void HandleTranslation()
    {
        Vector2 thumb = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        Vector3 move = new Vector3(thumb.x, 0, thumb.y);

        if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger)) move.y += 1;
        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger)) move.y -= 1;

        selectedObject.transform.position += move * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        Transform obj = selectedObject.transform;

        if (OVRInput.Get(OVRInput.Button.Two)) obj.Rotate(Vector3.right * -rotSpeed * Time.deltaTime, Space.World);
        if (OVRInput.Get(OVRInput.Button.One)) obj.Rotate(Vector3.right * rotSpeed * Time.deltaTime, Space.World);

        if (OVRInput.Get(OVRInput.Button.Three)) obj.Rotate(Vector3.up * -rotSpeed * Time.deltaTime, Space.World);
        if (OVRInput.Get(OVRInput.Button.Four)) obj.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);

        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick)) obj.Rotate(Vector3.forward * rotSpeed * Time.deltaTime, Space.World);
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick)) obj.Rotate(Vector3.forward * -rotSpeed * Time.deltaTime, Space.World);
    }

}
