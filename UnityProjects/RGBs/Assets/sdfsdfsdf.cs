using UnityEngine;

public class CheckRotatiodfn : MonoBehaviour
{
    void Update()
    {
        Debug.Log("Local EulerAngles: " + transform.localEulerAngles);
        Debug.Log("World EulerAngles: " + transform.eulerAngles);
        Debug.Log("Local Z: " + transform.localEulerAngles.z);
    }
}
