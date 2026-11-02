using TMPro;
using UnityEngine;

public class OVRSelectableMover : MonoBehaviour
{
    [Header("Ray Settings")]
    public Transform rayOrigin;
    public float rayLength = 20f;
    public LineRenderer lineRenderer;
    public LayerMask interactableLayer;
    public string[] targetTags = { "Rotatable", "robot", "PointCloud" };

    [Header("Movement Settings")]
    private float moveSpeed_START = 0.2f;
    private float moveSpeed_PC = 0.2f;
    private float moveSpeed;
    private float rotSpeed = 30f;

    [Header("References")]
    public GameObject robotRoot; // Assign "panda 1" GameObject in Inspector

    private GameObject selectedObject = null;
    private GameObject currentlyHitObject = null;
    private bool controlsEnabled = false;
    private bool isCasting = false;

    public OVRInput.Button castRayButton = OVRInput.Button.SecondaryIndexTrigger;

    public TextMeshProUGUI debugConsole;

    void Start()
    {

        moveSpeed = moveSpeed_START;
    }


    void Update()
    {
        HandleToggle();

        if (OVRInput.Get(castRayButton))
        {
            isCasting = true;
            HandleRaycast();
        }
        else
        {
            isCasting = false;
            currentlyHitObject = null;
            HideRay();
        }

        HandleSelection();

        if (controlsEnabled && selectedObject != null)
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

    void HideRay()
    {
        lineRenderer.SetPosition(0, rayOrigin.position);
        lineRenderer.SetPosition(1, rayOrigin.position);
    }

    void HandleRaycast()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Vector3 start = ray.origin;
        Vector3 end = start + ray.direction * rayLength;
        Color rayColor = Color.red;

        currentlyHitObject = null;

        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, interactableLayer))
        {
            end = hit.point;
            GameObject hitObj = hit.transform.gameObject;
            GameObject rootObj = hit.transform.root.gameObject;

            // Fix 1: check both hit object and its root
            //if (IsTagAllowed(hitObj.tag) || IsTagAllowed(rootObj.tag))
            //{
            //    currentlyHitObject = hitObj;
            //    rayColor = Color.blue;
            //    //debugConsole.text = $"currentlyHitObject: {currentlyHitObject} \n rootObj: {rootObj}";
            //}

            Transform t = hit.transform;
            while (t != null)
            {
                if (IsTagAllowed(t.tag))
                {
                    currentlyHitObject = t.gameObject;
                    rayColor = Color.blue;
                    break;
                }
                t = t.parent;
            }

        }

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
    }

    void HandleSelection()
    {
        if (currentlyHitObject != null)
        {
            moveSpeed = moveSpeed_START;

            //if (currentlyHitObject.CompareTag("robot") || currentlyHitObject.transform.root.CompareTag("robot"))
            if (currentlyHitObject != null && robotRoot != null &&
                (currentlyHitObject == robotRoot || currentlyHitObject.transform.IsChildOf(robotRoot.transform)))
            {
                selectedObject = robotRoot;
                Debug.Log("Selected robot base: " + robotRoot.name);
            }
            else
            {
                selectedObject = currentlyHitObject;
                Debug.Log("Selected object: " + selectedObject.name);
                if (selectedObject.name == "CubeSelection")
                    moveSpeed = moveSpeed_PC;
                    selectedObject = GameObject.Find("PointCloudTest");
            }
            //debugConsole.text += $"\nSelected object: {selectedObject}";
        }
    }

    void HandleTranslation()
    {
        Vector2 thumb = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector3 move = new Vector3(thumb.x, 0, thumb.y);

        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)) move.y += 1;
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger)) move.y -= 1;

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

    bool IsTagAllowed(string tag)
    {
        foreach (string allowedTag in targetTags)
        {
            if (tag == allowedTag)
                return true;
        }
        return false;
    }
}
