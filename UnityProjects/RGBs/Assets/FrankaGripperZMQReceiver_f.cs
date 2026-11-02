using NetMQ;
using NetMQ.Sockets;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;


public class FrankaGripperZMQReceiver_f : MonoBehaviour
{
    [Header("Finger Transforms")]
    public Transform leftFinger;
    public Transform rightFinger;

    [Header("Position Settings (Y Axis)")]
    public float leftOpenY = -0.0566f;
    public float leftClosedY = -0.0222f;

    public float rightOpenY = 0.0401f;
    public float rightClosedY = 0.0013f;

    public float moveSpeed = 0.05f;

    private string zmqAddress = "tcp://192.168.0.143:7782";
    //private string zmqAddress = "tcp://127.0.0.1:7782";

    public string topic = "GripperCommand";

    private float leftTargetY;
    private float rightTargetY;
    private bool moving = false;

    private SubscriberSocket subSocket;
    private NetMQPoller poller;
    private Thread zmqThread;
    private volatile bool shuttingDown = false;

    // Gripper width bounds
    private const float minWidth = 0.00116821f;
    private const float maxWidth = 0.0790397f;

    void Start()
    {
        SetFingerY(leftFinger, leftOpenY);
        SetFingerY(rightFinger, rightOpenY);
        leftTargetY = leftOpenY;
        rightTargetY = rightOpenY;

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
                poller.Run();
            }
            NetMQConfig.Cleanup();
        });

        zmqThread.IsBackground = true;
        zmqThread.Start();
    }

    private void Update()
    {
        if (moving)
        {
            Vector3 leftPos = leftFinger.localPosition;
            Vector3 rightPos = rightFinger.localPosition;

            leftPos.y = Mathf.MoveTowards(leftPos.y, leftTargetY, moveSpeed * Time.deltaTime);
            rightPos.y = Mathf.MoveTowards(rightPos.y, rightTargetY, moveSpeed * Time.deltaTime);

            leftFinger.localPosition = leftPos;
            rightFinger.localPosition = rightPos;

            if (Mathf.Approximately(leftPos.y, leftTargetY) &&
                Mathf.Approximately(rightPos.y, rightTargetY))
            {
                moving = false;
            }
        }
    }

    private void OnReceiveReady(object sender, NetMQSocketEventArgs e)
    {
        try
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            if (msg.FrameCount < 2) return;

            byte[] data = msg[1].ToByteArray();
            if (data.Length != 4)
            {
                UnityEngine.Debug.LogError("[GripperZMQ] Invalid float32 message");
                return;
            }

            float width = BitConverter.ToSingle(data, 0); // Read float32 (little endian)
            UnityMainThreadDispatcher.Instance().Enqueue(() => HandleWidthCommand(width));
        }
        catch (Exception ex)
        {
            if (!shuttingDown)
                UnityEngine.Debug.LogError("[GripperZMQ] Error: " + ex.Message);
        }
    }

    private void HandleWidthCommand(float width)
    {
        float clampedWidth = Mathf.Clamp(width, minWidth, maxWidth);
        float t = (clampedWidth - minWidth) / (maxWidth - minWidth); // Normalize to 0–1

        leftTargetY = Mathf.Lerp(leftClosedY, leftOpenY, t);
        rightTargetY = Mathf.Lerp(rightClosedY, rightOpenY, t);

        moving = true;
    }

    void SetFingerY(Transform finger, float y)
    {
        Vector3 pos = finger.localPosition;
        pos.y = y;
        finger.localPosition = pos;
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

        UnityEngine.Debug.Log("[GripperZMQ] Shutdown complete.");
    }
}
