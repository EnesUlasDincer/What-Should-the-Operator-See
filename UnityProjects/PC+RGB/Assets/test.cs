using UnityEngine;

public class DebugTransform : MonoBehaviour
{
    public Vector3 manualEuler;

    void Start()
    {
        manualEuler = new Vector3(-90f, -130f, 110f);
        transform.localEulerAngles = manualEuler;
    }

    void Update()
    {
        Debug.Log("✅ Manual Euler Angles: " + manualEuler);
        Debug.Log("⛔ Current localEulerAngles: " + transform.localEulerAngles);
    }

}
