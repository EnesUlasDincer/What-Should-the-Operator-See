using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using TMPro;
using NetMQ;
using NetMQ.Sockets;
using System.Text;
using Newtonsoft.Json;

public class ZMQHeadPublisher : MonoBehaviour
{
    public class HeadTrackingMsg
    {
        public float[] position;
        public float[] rotation;
    }

    public TextMeshProUGUI debugConsole;

    private InputDevice headDevice;
    private PublisherSocket pubSocket;
    private int count;

    void Start()
    {
        // Get head device
        headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        // Initialize ZMQ publisher
        AsyncIO.ForceDotNet.Force(); // Required for Unity
        pubSocket = new PublisherSocket();
        pubSocket.Bind("tcp://*:5560");

        // Init count
        count = 0;
        if (debugConsole != null)
            debugConsole.text = $"Started. Count: {count}";
    }

    void Update()
    {
        if (!headDevice.isValid)
        {
            headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return;
        }

        if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
            headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            var msg = new HeadTrackingMsg
            {
                position = new float[] { pos.x, pos.y, pos.z },
                rotation = new float[] { rot.eulerAngles.x, rot.eulerAngles.y, rot.eulerAngles.z }
            };

            // Serialize and send via ZMQ
            string json = JsonConvert.SerializeObject(msg);
            pubSocket.SendMoreFrame("HEAD").SendFrame(json);

            if (debugConsole != null)
                debugConsole.text = $"Count: {count}\nPos: {pos:F2}\nRot: {rot.eulerAngles:F1}";
        }

        count++;
    }

    private void OnDestroy()
    {
        // Properly close ZMQ socket
        pubSocket?.Close();
        NetMQConfig.Cleanup();
    }
}
