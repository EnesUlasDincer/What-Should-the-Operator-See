## Main Loop

The `Streamer` represents the main loop of the system. It orchestrates multiple `Source` instances, processes the acquired 3D data, and transmits it to the Meta Quest headset or other clients via ZeroMQ.


### Overview

- Central runtime component of the pipeline
- Connects multiple `Source` instances
- Applies spatial alignment and filtering
- Performs voxel downsampling
- Publishes the final point cloud via ZeroMQ
- All point cloud processing is GPU-accelerated when available


### Processing Pipeline

| Step                    | Description                                                                 |
|-------------------------|-----------------------------------------------------------------------------|
| **Read**                | Reads the Pointclouds from the cameras, waits until every camera has  returned a pointcloud |
| **Transform**           | Align point clouds using calibration extrinsics                             |
| **Merge**               | Concatenate point clouds into one unified representation                    |
| **Crop**                | Apply 3D bounding box to remove irrelevant regions                          |
| **Downsample**          | Reduce number of points via voxel-based downsampling                        |
| **Publish**             | Send via ZMQ using a PUB socket                                             |

 All these Steps are clearly marked in the [Streamer.py](../streamer/PC_Streamer.py), which contains the mainloop


 ### Publish

To send point cloud data from the streaming system to the Meta Quest, the class **`ZMQPublishPointCloudAction`** from *[Action.py](../streamer/Action.py)* is used.  
In the code, this publisher is created and used as follows:

```python
sending = a.ZMQPublishPointCloudAction()

...

# Inside the main loop:
sending.set_pointcloud(xyz, rgb)
sending.execute()
```

The method execute() is responsible for transmitting the point cloud to the headset.
It has the following signature:

```
def execute(self, num_points = 100000):
    ...
```

The parameter **`num_points`** is critical for correct operation.  
It defines the **maximum number of 3D points** the Meta Quest will expect and allocate GPU buffers for.  
In other words, it tells the renderer how large the point array should be — effectively determining the maximum number of points that can be displayed per frame.

#### Guidelines and Considerations

- Set **`num_points`** to the **maximum possible number of points** you may send in a frame.  
- **Example:** Two cameras at 640×480 produce roughly 1,000,000 raw samples.  
  After cropping and downsampling you might have around **50,000–60,000 points**.  
  Choose a safe upper bound (e.g., **60,000**) that covers peak cases plus some margin.
- The value should reflect the **worst-case output** from your preprocessing pipeline  
  (cropping, filtering, downsampling), because the headset allocates buffers for this declared maximum.
- **Keep `num_points` relatively constant.**  
  If the value changes frequently, the Meta Quest may repeatedly allocate new GPU buffers, which can lead to performance issues or rendering delays.

⚠️ **Important (Warning):**  
If the sender transmits **more points** than `num_points`, the behavior on the Meta Quest is **undefined**.  
Because the headset allocates buffer memory only for the declared size, sending a larger array can cause internal errors or rendering failures — commonly the point cloud will **not be shown** until the transmitted count is reduced to ≤ `num_points`.