import time
from contextlib import contextmanager
import cv2
import Source
import Action

import argparse

parser = argparse.ArgumentParser(description="Parameters for the program.")
parser.add_argument(
    "--mode",
    type=str,
    choices=["ALL", "WRIST"],
    default="ALL",
    help='Mode: "ALL" or "WRIST"'
)
parser.add_argument(
    "--quality",
    type=str,
    choices=["HIGH", "MEDIUM", "LOW"],
    default="LOW",
    help='Quality: "HIGH", "MEDIUM", or "LOW"'
)
args = parser.parse_args()


@contextmanager
def track_time(label: str):
    start = time.perf_counter()
    yield
    elapsed = time.perf_counter() - start
    # print(f"[{label}] {elapsed:.4f} s")


MODE = args.mode
QUALITY = args.quality
upper_id = "007522061936"
left_id  = "243322073029"
right_id = "944622073668"
wcam_id  = "007522060349"

if QUALITY == "HIGH":
    width = 1280
    height = 720
    fps = 30
elif QUALITY == "MEDIUM":
    width = 848
    height = 480
    fps = 60
elif QUALITY == "LOW":
    width = 640
    height = 480
    fps = 60

if MODE == "ALL":
    upper = Source.RealSenseCamera(id=upper_id, org_width=width, org_height=height, res_width=width, res_height=height, fps=fps, use_imgs=True, use_segmentation=False, use_pc_creation=False)
    left  = Source.RealSenseCamera(id=left_id, org_width=width, org_height=height, res_width=width, res_height=height, fps=fps, use_imgs=True, use_segmentation=False, use_pc_creation=False)
    right = Source.RealSenseCamera(id=right_id, org_width=width, org_height=height, res_width=width, res_height=height, fps=fps, use_imgs=True, use_segmentation=False, use_pc_creation=False)
    wcam = Source.RealSenseCamera(id=wcam_id, org_width=width, org_height=height, res_width=width, res_height=height, fps=fps, use_imgs=True, use_segmentation=False, use_pc_creation=False)
    sender_upper = Action.ZMQPublishImageAction(topic="UPPER_CAM_VideoStream", port=7783)
    sender_left = Action.ZMQPublishImageAction(topic="LEFT_CAM_VideoStream", port=7781)
    sender_right = Action.ZMQPublishImageAction(topic="RIGHT_CAM_VideoStream", port=7782)
    sender_wcam = Action.ZMQPublishImageAction(topic="WCAM_VideoStream", port=7780)

elif MODE == "WRIST":
    wcam = Source.RealSenseCamera(id=wcam_id, org_width=width, org_height=height, res_width=width, res_height=height, fps=fps, use_imgs=True, use_segmentation=False, use_pc_creation=False)
    left  = None
    right = None
    upper = None
    sender_wcam = Action.ZMQPublishImageAction(topic="WCAM_VideoStream", port=7780)
    
Action.start_discovery_server()
while True:
        start_total = time.time()
        dev = "CUDA:0"  # oder "CPU:0"
        r_upper = None
        r_left = None
        r_right = None
        r_wcam = None

        with track_time("------ READING ------ "):
            while True:
                if r_upper is None and upper is not None:
                    r_upper = upper.get_rgbd_and_config()
                if r_left is None and left is not None:
                    r_left = left.get_rgbd_and_config()
                if r_right is None and right is not None:
                    r_right = right.get_rgbd_and_config()
                if r_wcam is None and wcam is not None:
                    r_wcam = wcam.get_rgbd_and_config()
                if (r_upper is not None or upper is None) and \
                   (r_left is not None or left is None) and \
                   (r_right is not None or right is None) and \
                   (r_wcam is not None or wcam is None):

                    if r_upper is not None:
                        rgb_upper, depth_upper, config_upper = r_upper
                    if r_left is not None:
                        rgb_left, depth_left, config_left = r_left
                    if r_right is not None:
                        rgb_right, depth_right, config_right = r_right
                    if r_wcam is not None:
                        rgb_wcam, depth_wcam, config_wcam = r_wcam
                    
                    break
                time.sleep(0.001)

        with track_time("------ SHOW ------ "):
            if upper is not None:
                cv2.imshow("Upper RGB", rgb_upper)
            if left is not None:
                cv2.imshow("Left RGB", rgb_left)
            if right is not None:
                cv2.imshow("Right RGB", rgb_right)
            if wcam is not None:
                cv2.imshow("WCam RGB", rgb_wcam)
            cv2.waitKey(1)

        with track_time("------ SENDING ------ "):
            if upper is not None:
                sender_upper.image = rgb_upper
                sender_upper.execute()

            if left is not None:
                sender_left.image = rgb_left
                sender_left.execute()

            if right is not None:
                sender_right.image = rgb_right
                sender_right.execute()

            if wcam is not None:
                sender_wcam.image = rgb_wcam
                sender_wcam.execute()