import zmq
import pickle
import time
from concurrent.futures import ThreadPoolExecutor
from queue import Queue

import cv2
import numpy as np
import torch
from ultralytics import YOLO
from scipy.ndimage import binary_dilation
import os

# -------------------- Model -------------------
base_dir = os.path.dirname(os.path.abspath(__file__))
MODEL_PATH = os.path.join(base_dir, "train91", "best.pt")
model = YOLO(MODEL_PATH, task="segment")

print("CUDA available:", torch.cuda.is_available())


# -------------------- ZMQ Server --------------------
PORT = 25555
context = zmq.Context()
socket = context.socket(zmq.ROUTER)
socket.bind(f"tcp://*:{PORT}")

executor = ThreadPoolExecutor(max_workers=3)
reply_queue = Queue()

print("✅ YOLO server started on port", PORT)


# -------------------- Mask Utils --------------------
def get_inverted_mask(result):
    """Create inverted binary mask for selected classes."""
    if result.masks is None:
        return None

    masks = result.masks.data
    classes = result.boxes.cls

    selected = (classes == 0) | (classes == 1) | (classes == 2) | (classes == 3)
    if not selected.any():
        return None

    combined = masks[selected].any(dim=0).byte().cpu().numpy()
    combined = binary_dilation(combined, iterations=2)

    return np.logical_not(combined).astype(np.uint8) * 255


# -------------------- Request Processing --------------------
def process_request(identity, data):
    start_time = time.time()

    try:
        rgb = pickle.loads(data)
    except Exception as e:
        print("❌ Pickle error:", e)
        return

    if not isinstance(rgb, np.ndarray) or rgb.ndim != 3 or rgb.shape[2] != 3:
        print("❌ Invalid image from", identity)
        return

    if rgb.dtype != np.uint8:
        rgb = rgb.astype(np.uint8)

    rgb = cv2.resize(rgb, (640, 480), interpolation=cv2.INTER_LINEAR)

    # Model inference
    with torch.inference_mode():
        result = model.predict(
            rgb,
            conf=0.5,
            verbose=False,
            save=False,
            stream=False,
        )[0]

    torch.cuda.synchronize()
    print(f"Inference time: {time.time() - start_time:.3f}s")

    mask = get_inverted_mask(result)
    reply_queue.put((identity, pickle.dumps(mask)))

    print(f"✅ Finished request from {identity} in {time.time() - start_time:.3f}s")


# -------------------- Main Loop --------------------
while True:
    # Receive requests
    if socket.poll(1):
        ident, _, msg = socket.recv_multipart()
        executor.submit(process_request, ident, msg)

    # Send replies
    while not reply_queue.empty():
        ident, reply = reply_queue.get()
        socket.send_multipart([ident, b"", reply])
