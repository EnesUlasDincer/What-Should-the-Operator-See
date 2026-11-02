using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading;
using PimDeWitte.UnityMainThreadDispatcher;

public class FrankaGripperZMQReceiver : MonoBehaviour
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

    // public string zmqAddress = "tcp://127.0.0.1:7782";
    //private string zmqAddress = "tcp://192.168.0.208:7782";
    private string zmqAddress = "tcp://192.168.0.143:7782";

    public string topic = "GripperCommand";

    private float leftTargetY;
    private float rightTargetY;
    private bool moving = false;

    private SubscriberSocket subSocket;
    private NetMQPoller poller;
    private Thread zmqThread;
    private volatile bool shuttingDown = false;

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
                Debug.LogError("[GripperZMQ] Invalid int32 message");
                return;
            }

            int cmd = BitConverter.ToInt32(data, 0); // Read as little-endian int
            UnityMainThreadDispatcher.Instance().Enqueue(() => HandleCommand(cmd));
        }
        catch (Exception ex)
        {
            if (!shuttingDown)
                Debug.LogError("[GripperZMQ] Error: " + ex.Message);
        }
    }

    private void HandleCommand(int cmd)
    {
        if (cmd == 0)
        {
            // Open
            leftTargetY = leftOpenY;
            rightTargetY = rightOpenY;
            moving = true;
        }
        else if (cmd == 1)
        {
            // Close
            leftTargetY = leftClosedY;
            rightTargetY = rightClosedY;
            moving = true;
        }
        else
        {
            Debug.LogWarning("[GripperZMQ] Unknown int command: " + cmd);
        }
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

        Debug.Log("[GripperZMQ] Shutdown complete.");
    }
}
