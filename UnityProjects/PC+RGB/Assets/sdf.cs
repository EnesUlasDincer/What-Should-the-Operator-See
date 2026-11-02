using UnityEngine;

public class sdf : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("AQ EulerAngles: " + transform.localEulerAngles);
        Debug.Log("AQ EulerAngles: " + transform.eulerAngles);
        Debug.Log("AQ Z: " + transform.localEulerAngles.z);
    }

}
