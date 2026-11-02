using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using PimDeWitte.UnityMainThreadDispatcher;

public class JointAngleReceiver : MonoBehaviour
{
    private string zmqAddress = "tcp://192.168.0.143:7781";
    //private string zmqAddress = "tcp://192.168.0.208:7781";
    //private string zmqAddress = "tcp://127.0.0.1:7781";
    public string topic = "JointAngles";
    public Transform[] robotJoints = new Transform[7]; // Assign in Inspector

    private float rotationSpeed = 30.0f; // Degrees per second

    private Vector3[] jointAxes = new Vector3[7]
    {
        Vector3.forward, Vector3.forward, Vector3.forward,
        Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
    };

    private float[] targetAngles = new float[7]; // in degrees
    private Quaternion[] targetRotations = new Quaternion[7];
    private Quaternion[] initialRotations = new Quaternion[7];

    private float[] jointAngles_offset = { 0.0f, 0.0f, 0.0f, -3.92699f, 0.0f, 2.35619f, -0.942167f };

    private SubscriberSocket subSocket;
    private NetMQPoller poller;
    private Thread zmqThread;
    private volatile bool shuttingDown = false;

    private const int numJoints = 7;

    private void Start()
    {
        for (int i = 0; i < numJoints; i++)
        {
            if (robotJoints[i] != null)
            {
                initialRotations[i] = robotJoints[i].localRotation;
                targetRotations[i] = robotJoints[i].localRotation;
                targetAngles[i] = 0f;
            }
        }

        zmqThread = new Thread(() =>
        {
            AsyncIO.ForceDotNet.Force();

            using (subSocket = new SubscriberSocket())
            using (poller = new NetMQPoller())
            {
                subSocket.Connect(zmqAddress);
                subSocket.Subscribe(topic);
                subSocket.ReceiveReady += OnReceiveReady;

                poller.Add(subSocket);
                try { poller.Run(); }
                catch (Exception e)
                {
                    Debug.LogError("[JointAngleReceiver] Poller exception: " + e.Message);
                }
            }

            NetMQConfig.Cleanup();
        });

        zmqThread.IsBackground = true;
        zmqThread.Start();
    }

    private void Update()
    {
        for (int i = 0; i < numJoints; i++)
        {
            if (robotJoints[i] != null)
            {
                robotJoints[i].localRotation = Quaternion.RotateTowards(
                    robotJoints[i].localRotation,
                    targetRotations[i],
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    private void OnReceiveReady(object sender, NetMQSocketEventArgs e)
    {
        try
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            if (msg.FrameCount < 2) return;

            byte[] angleBytes = msg[1].ToByteArray();
            float[] angles = ByteArrayToFloatArray(angleBytes);

            if (angles.Length != numJoints)
            {
                Debug.LogError($"Expected {numJoints} joint angles, got {angles.Length}");
                return;
            }

            UnityMainThreadDispatcher.Instance().Enqueue(() => UpdateTargetAngles(angles));
        }
        catch (Exception ex)
        {
            if (!shuttingDown)
                Debug.LogError("[JointAngleReceiver] Receive error: " + ex.Message);
        }
    }

    private void UpdateTargetAngles(float[] anglesInRadians)
    {
        for (int i = 0; i < numJoints; i++)
        {
            targetAngles[i] = -anglesInRadians[i] - 0*jointAngles_offset[i];
            targetAngles[i] = targetAngles[i] * Mathf.Rad2Deg;


            targetRotations[i] = initialRotations[i] * Quaternion.AngleAxis(targetAngles[i], jointAxes[i].normalized);
        }
    }

    public static float[] ByteArrayToFloatArray(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length % 4 != 0)
            throw new ArgumentException("Invalid byte array length");

        ReadOnlySpan<byte> byteSpan = byteArray;
        ReadOnlySpan<float> floatSpan = MemoryMarshal.Cast<byte, float>(byteSpan);
        return floatSpan.ToArray();
    }

    private void OnDestroy()
    {
        shuttingDown = true;

        if (poller != null && poller.IsRunning)
        {
            poller.Stop();
        }

        if (zmqThread != null && zmqThread.IsAlive)
        {
            zmqThread.Join();
        }

        Debug.Log("[JointAngleReceiver] Clean shutdown complete.");
    }
}
