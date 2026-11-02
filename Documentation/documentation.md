# Documentation

There are 4 different part of script: RGB Streaing, PC Streaming, Teleoperation & Joint Streaming and also Segmentation. 

## RGB Streaming


## PC Streaming:
- [Source](docu_source.md): is responsibel for receveing and providing the data (e.g. point clouds) to the main loop
- [Main Loop](docu_mainloop.md): reads all pointclouds out of the cameras and processes them at the end it sends them to Meta Quest

## Teleoperation & Joint Streaming


## Segmentation


## Meta Quest


### RGB

Open the [Scene](../UnityProjects/RGBs/Assets/SceneLoader/Scene/RGBs.unity) in the Unity Editor.  
The relevant objects are the receivers for **WCAM**, **LEFT_CAM**, **RIGHT_CAM**, and **UPPER_CAM**.  
These objects include the [ZMQVideoReceiver](../UnityProjects/RGBs/Assets/ZMQVideoReceiver.cs), which receives the RGB images and displays them on the corresponding planes.

### PC

The relevant scene is [PointCloud](../UnityProjects/PC/Assets/SceneLoader/Scene/PointCould.unity).  
All implementations related to point cloud rendering are explained here.  
The point cloud rendering itself is implemented in  
[PointCloudLoader.cs](../UnityProjects/PC/Assets/SceneLoader/Scripts/PointCloudLoader.cs).

A more detailed explanation of how the rendering works can be found in the  
[PC Rendering Documentation](docu_pc_renderer.md).

Controller interaction is implemented in:  
[RaycastControllerInteractor.cs](../UnityProjects/PC/Assets/RaycastControllerInteractor.cs).

The [JointAngleReceiver](../UnityProjects/PC/Assets/JointAngleReceiver.cs) maps the joint angles to the robot model in the VR headset.  
Similarly, the state of the gripper is mapped to the virtual gripper in VR by  
[FrankaGripperZMQReceiver](../UnityProjects/PC/Assets/FrankaGripperZMQReceiver_f.cs).

### PC + RGB

The relevant scene is [PC+RGB](../UnityProjects/PC+RGB/Assets/SceneLoader/Scene/PC+RGB.unity).  
Most files are identical to those in the PC project, but are integrated into the **PC+RGB** project.

For RGB streaming, the  
[ZMQVideoReceiver](../UnityProjects/PC+RGB/Assets/ZMQVideoReceiver.cs)  
is used to display the received frames on a plane.
