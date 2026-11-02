using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class OVRSelectableMover : MonoBehaviour
{
    [Header("Ray Settings")]
    public OVRCameraRig camerarig;
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
    private bool isCasting = false;

    public static bool controlsEnabled = false;

    private float buttonDownTime = 0f;
    private bool isButtonHeld = false;
    private float lastToggleTime = 0f;

    [System.Serializable]
    private class ObjectData
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
    }
    [System.Serializable]
    private class ObjectDataList
    {
        public List<ObjectData> objects = new List<ObjectData>();
    }
    private Vector3 referencePosition = Vector3.zero;
    private Quaternion referenceRotation = Quaternion.identity;
    private bool referenceInitialized = false;


    public OVRInput.Button castRayButton = OVRInput.Button.SecondaryIndexTrigger;

    public TextMeshProUGUI debugConsole;

    void Start()
    {

        moveSpeed = moveSpeed_START;

    }


    void Update()
    {
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

        handleInit();


        HandleToggle();

        HandleSelection();

        if (controlsEnabled && selectedObject != null)
        {
            HandleTranslation();
            HandleRotation();
        }
    }

    void handleInit()
    {
        //At the beginning
        OVRInput.Controller controller = OVRInput.GetActiveController();
        if (!referenceInitialized && controller != null && controller == OVRInput.Controller.Touch)
        {
            InitializeReferenceOrigin();
            ApplyAllSavedTransforms();
        }

        //When button is pressed
        if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger) && controller != null && controller == OVRInput.Controller.Touch)
        {
            InitializeReferenceOrigin();
            ApplyAllSavedTransforms();
        }
    }

    /*
    void HandleToggle()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            controlsEnabled = !controlsEnabled;
            Debug.Log("Controls Enabled: " + controlsEnabled);
        }
    }*/

    void HandleToggle()
    {
        // Wenn der Button gedr ckt wird
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            buttonDownTime = Time.time;
            isButtonHeld = true;
        }

        // Wenn der Button losgelassen wird
        if (OVRInput.GetUp(OVRInput.Button.Start) && isButtonHeld)
        {
            float heldDuration = Time.time - buttonDownTime;

            if (heldDuration >= 3f)
            {
                SaveSelectedObject();

                // Trigger a short vibration on the left controller
                OVRInput.SetControllerVibration(1f, 0.5f, OVRInput.Controller.LTouch);

                // Stop vibration after short delay (e.g. 0.2s)
                StartCoroutine(StopVibrationAfterDelay(0.2f));
            }
            else
            {
                controlsEnabled = !controlsEnabled;
            }

            isButtonHeld = false;
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

        UnityEngine.Debug.Log("Here in handle Raycast");
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
                UnityEngine.Debug.Log("Selected robot base: " + robotRoot.name);
            }
            else
            {
                selectedObject = currentlyHitObject;
                UnityEngine.Debug.Log("Selected object: " + selectedObject.name);
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

    void SaveSelectedObject()
    {
        if (selectedObject == null)
        {
            UnityEngine.Debug.LogWarning("No selected object to save.");
            return;
        }

        string json = PlayerPrefs.GetString("SavedObjects", "");
        ObjectDataList dataList = string.IsNullOrEmpty(json)
            ? new ObjectDataList()
            : JsonUtility.FromJson<ObjectDataList>(json);

        dataList.objects.RemoveAll(o => o.name == selectedObject.name);

        ObjectData data = new ObjectData
        {
            name = selectedObject.name,
            position = ToRelativePosition(selectedObject.transform.position),
            rotation = Quaternion.Inverse(referenceRotation) * selectedObject.transform.rotation
        };
        dataList.objects.Add(data);

        string newJson = JsonUtility.ToJson(dataList);
        PlayerPrefs.SetString("SavedObjects", newJson);
        PlayerPrefs.Save();

        UnityEngine.Debug.Log("Object saved (relative): " + data.name);
    }


    public void ApplyAllSavedTransforms()
    {
        string json = PlayerPrefs.GetString("SavedObjects", "");
        if (string.IsNullOrEmpty(json))
        {
            UnityEngine.Debug.LogWarning("No saved objects found.");
            return;
        }

        ObjectDataList dataList = JsonUtility.FromJson<ObjectDataList>(json);
        int appliedCount = 0;

        foreach (var data in dataList.objects)
        {
            GameObject obj = GameObject.Find(data.name);
            if (obj != null)
            {
                Vector3 worldPos = ToWorldPosition(data.position);
                Quaternion worldRot = referenceRotation * data.rotation;
                obj.transform.SetPositionAndRotation(worldPos, worldRot);

                UnityEngine.Debug.Log($"Applied relative transform to: {data.name}");
                appliedCount++;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"GameObject not found in scene: {data.name}");
            }
        }

        UnityEngine.Debug.Log($"Total transforms applied: {appliedCount}");
    }


    private IEnumerator StopVibrationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }

    public void InitializeReferenceOrigin()
    {
        Vector3 leftpos = camerarig.leftControllerAnchor.position;
        UnityEngine.Debug.Log($" init left controller: {leftpos}");

        OVRInput.Controller controller = OVRInput.GetActiveController();
        UnityEngine.Debug.Log($" init active controller: {controller}");

        referencePosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        referenceRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
        referenceInitialized = true;

        UnityEngine.Debug.Log($"Reference Origin initialized: pos={referencePosition}, rot={referenceRotation.eulerAngles}");
    }

    Vector3 ToRelativePosition(Vector3 worldPos)
    {
        if (!referenceInitialized) return worldPos;
        return Quaternion.Inverse(referenceRotation) * (worldPos - referencePosition);
    }

    Vector3 ToWorldPosition(Vector3 relativePos)
    {
        if (!referenceInitialized) return relativePos;
        return referencePosition + (referenceRotation * relativePos);
    }

}
