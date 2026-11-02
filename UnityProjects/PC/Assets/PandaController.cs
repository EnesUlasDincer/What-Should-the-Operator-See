using UnityEngine;

public class FrankaStepwiseRotator : MonoBehaviour
{
    public Transform[] joints;             // Assign your 7 joint Transforms in the Inspector
    public float[] targetAngles;           // Target angles for each joint (in degrees)
    public bool[] activeJoints = new bool[7];           // Active joints
    [Header("Rotation Settings")]
    public float transitionDuration = 1f;  // Duration for each joint's motion
    private Vector3[] jointAxes = new Vector3[7]
    {
        Vector3.forward, // panda_joint1: 0 0 1
        Vector3.forward, // panda_joint2: 0 0 1
        Vector3.forward, // panda_joint3: 0 0 1
        Vector3.forward, // panda_joint4: 0 0 1
        Vector3.forward, // panda_joint5: 0 0 1
        Vector3.forward, // panda_joint6: 0 0 1
        Vector3.forward  // panda_joint7: 0 0 1
    };

    //private float[] jointAngles_offset = { 0.0f, 0.0f, 0.0f, -3.92699f, 0.0f, 2.35619f, -0.942167f };
    private float[] jointAngles_offset = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.436332f, 0.0f };

    private Quaternion[] targetRotations;
    public float rotationSpeed = 10.0f;   // Degrees per second
    void Start()
    {
        if (joints.Length != 7 || targetAngles.Length != 7 || jointAxes.Length != 7)
        {
            Debug.LogError("All joint-related arrays must contain exactly 7 elements.");
            return;
        }

        // Precompute target rotations
        targetRotations = new Quaternion[7];
        for (int i = 0; i < 7; i++)
        {
            jointAngles_offset[i] = jointAngles_offset[i] * Mathf.Rad2Deg * 1;
            targetAngles[i] = -targetAngles[i] - 0*jointAngles_offset[i];

            Debug.Log("target");
            Debug.Log(i);
            Debug.Log(targetAngles[i]);
            
            targetRotations[i] = joints[i].localRotation * Quaternion.AngleAxis(targetAngles[i], jointAxes[i].normalized);
        }
    }

    void Update()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            if (activeJoints[i])
            {
                joints[i].localRotation = Quaternion.RotateTowards(
                    joints[i].localRotation,
                    targetRotations[i],
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

}