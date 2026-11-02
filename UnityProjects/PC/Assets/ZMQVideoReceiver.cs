using UnityEngine;
using TMPro;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using PimDeWitte.UnityMainThreadDispatcher;
using System;

public class ZMQCameraReceiver : MonoBehaviour
{
    public Renderer displayRenderer;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI fpsText;

    private Texture2D videoTexture;


    //public string zmqAddress = "tcp://192.168.0.208:7780";
    public string zmqAddress = "tcp://192.168.0.143:7780";
    //public string zmqAddress = "tcp://127.0.0.1:7780";
    public string topic = "VideoStream";

    private SubscriberSocket subSocket;
    private NetMQPoller poller;
    private Thread zmqThread;

    private float lastUpdateTime = 0f;
    private int frameCount = 0;

    private volatile bool shuttingDown = false;

    void Start()
    {
        UpdateStatus("Connecting...");
        videoTexture = new Texture2D(2, 2);  // dummy initial size
        displayRenderer.material.mainTexture = videoTexture;


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
                try
                {
                    poller.Run(); // blocks until Stop is called
                }
                catch (Exception e)
                {
                    Debug.LogError("[ZMQReceiver] Poller exception: " + e.Message);
                }
            }

            NetMQConfig.Cleanup(); // must be called once per process/thread
        });

        zmqThread.IsBackground = true;
        zmqThread.Start();
    }

    private void OnReceiveReady(object sender, NetMQSocketEventArgs e)
    {
        try
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            if (msg.FrameCount < 2) return;

            byte[] imageBytes = msg[1].ToByteArray();

            //UnityMainThreadDispatcher.Instance().Enqueue(() =>
            //{
            //    // Texture2D tex = new Texture2D(2, 2);
            //    // tex.filterMode = FilterMode.Point;  // ← Key to avoid blur
            //    if (videoTexture.LoadImage(imageBytes))
            //    {
            //        float aspect = (float)videoTexture.width / videoTexture.height;
            //        displayRenderer.material.mainTexture = videoTexture;

            //        Vector3 newScale = displayRenderer.transform.localScale;
            //        newScale.x = aspect;
            //        displayRenderer.transform.localScale = newScale;


            //        statusText.transform.localScale = new Vector3(1 / aspect, 1f, 1f);
            //        fpsText.transform.localScale = new Vector3(1 / aspect, 1f, 1f);

            //        statusText.text = $"Received {videoTexture.width}x{videoTexture.height}";

            //        frameCount++;
            //        float now = Time.realtimeSinceStartup;
            //        if (now - lastUpdateTime >= 1.0f)
            //        {
            //            float fps = frameCount / (now - lastUpdateTime);
            //            fpsText.text = $"FPS: {fps:F1}";
            //            lastUpdateTime = now;
            //            frameCount = 0;
            //        }
            //    }
            //});


            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (videoTexture.LoadImage(imageBytes))
                {
                    float aspect = (float)videoTexture.width / videoTexture.height;
                    displayRenderer.material.mainTexture = videoTexture;

                    // Get current scale and adjust only X to preserve aspect ratio
                    Vector3 currentScale = displayRenderer.transform.localScale;
                    float newX = currentScale.y * aspect; // X depends on Y and aspect
                    displayRenderer.transform.localScale = new Vector3(newX, currentScale.y, currentScale.z);

                    // Adjust UI text to remain readable
                    Vector3 inverseAspectScale = new Vector3(1 / aspect, 1f, 1f);
                    statusText.transform.localScale = inverseAspectScale;
                    fpsText.transform.localScale = inverseAspectScale;

                    statusText.text = $"Received {videoTexture.width}x{videoTexture.height}";

                    frameCount++;
                    float now = Time.realtimeSinceStartup;
                    if (now - lastUpdateTime >= 1.0f)
                    {
                        float fps = frameCount / (now - lastUpdateTime);
                        fpsText.text = $"FPS: {fps:F1}";
                        lastUpdateTime = now;
                        frameCount = 0;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            if (!shuttingDown)
                Debug.LogError("[ZMQReceiver] Receive error: " + ex.Message);
        }
    }

    void OnDestroy()
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

        Debug.Log("[ZMQReceiver] Clean shutdown complete.");
    }

    private void UpdateStatus(string msg)
    {
        Debug.Log("[ZMQReceiver] " + msg);
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (statusText != null)
                statusText.text = msg;
        });
    }
}
