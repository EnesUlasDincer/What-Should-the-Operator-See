import threading
import socket
import zmq
import numpy as np
import struct
import cv2
import time
from collections import deque

stop_event = threading.Event()
def start_discovery_server(port=5556, response_message=b"ZMQ_SERVER"):
    global stop_event

    def discovery_loop():
        global stop_event
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(('', port))
        print(f"[INFO] Discovery-Server runs on Port {port}...")

        while not stop_event.is_set():
            try:
                data, addr = sock.recvfrom(1024)
                if data == b"DISCOVER_ZMQ_SERVER":
                    print(f"[DISCOVERY] request from {addr[0]}")
                    sock.sendto(response_message, addr)
            except Exception as e:
                print("[ERROR] Discovery-Server:", e)
                break

        sock.close()

    thread = threading.Thread(target=discovery_loop, daemon=True)
    thread.start()


def stop_discovery_server():
    stop_event.set()



class ZMQPublishPointCloudAction():
    def __init__(self, port=5555):
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.PUB)
        self.socket.setsockopt(zmq.SNDHWM, 1)
        self.socket.bind(f"tcp://*:{port}")
        print(f"[INFO] ZMQ Server runs on Port {port}...")

        self.topic = "PointCloud"
        self.last_fps_log_time = time.time()
        self.call_counter = 0

        self.xyz = None
        self.rgb = None
        
        self.recent_points = deque(maxlen=100)
        self.avg_points = 0

    def execute(self, num_points = 100000):
            if self.xyz is None or self.rgb is None:
                print("[WARN] No Pointcloud, skip sending")
                return

            xyz_m = np.asarray(self.xyz)        # shape (N, 3), float32, Meter
            rgb_f = np.asarray(self.rgb)        # shape (N, 3), float32, [0.0, 1.0]

            # convert XYZ → int16 into Millimeter
            xyz_mm = (xyz_m * 1000.0).astype(np.int16)

            # convert RGB → uint8 (0–255)
            rgb_u8 = (rgb_f * 255.0).astype(np.uint8)

            xyz_bytes = xyz_mm.tobytes()
            rgb_bytes = rgb_u8.tobytes()

            num_points_bytes = struct.pack('<I', num_points)
            try:
                self.socket.send_multipart([
                    self.topic.encode(),
                    num_points_bytes,
                    xyz_bytes,
                    rgb_bytes
                ])
            except zmq.Again:
                print("[WARN] ZMQ send failed (socket busy)")

            current_time = time.time()
            self.call_counter += 1            
            self.recent_points.append(self.xyz.shape[0])
            
            # Calculate FPS
            elapsed = current_time - self.last_fps_log_time

            if elapsed >= 1.0:
                fps = self.call_counter / elapsed
                self.call_counter = 0
                self.last_fps_log_time = current_time
                self.avg_points = sum(self.recent_points) / len(self.recent_points)
                print(f"[Sending FPS] Streamer is sending points with: {fps:.2f} FPS, Points: {self.avg_points}")

            self.xyz = None
            self.rgb = None

    def set_pointcloud(self, xyz, rgb):
        self.xyz = xyz
        self.rgb = rgb


class ZMQPublishImageAction:
    def __init__(self, topic, ip="*", port=7781):
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.PUB)
        self.socket.bind(f"tcp://{ip}:{port}")

        self.topic = topic
        self.image = None

        self.last_fps_log_time = time.time()
        self.call_counter = 0
        self.frame_times = deque(maxlen=500)

        print(f"[INFO] ZMQ Image Publisher on tcp://{ip}:{port}, topic={topic}")

    def execute(self):
        if self.image is None:
            return

        img = np.asarray(self.image)
        success, encoded_image = cv2.imencode(".jpg", img)

        if not success:
            print("[WARN] Failed to encode image")
            return

        image_bytes = encoded_image.tobytes()

        try:
            self.socket.send_multipart([self.topic.encode(), image_bytes])
        except zmq.Again:
            print("[WARN] ZMQ send failed (socket busy)")

        # FPS Logging
        now = time.time()
        self.frame_times.append(now)

        # Nur loggen, wenn mindestens 2 Frames vorhanden
        if len(self.frame_times) > 1:
            elapsed = self.frame_times[-1] - self.frame_times[0]
            avg_fps = (len(self.frame_times) - 1) / elapsed if elapsed > 0 else 0
            # Nur alle 1 Sekunde loggen
            if now - self.last_fps_log_time >= 1.0:
                print(f"[Sending FPS] Streamer is sending frames at: {avg_fps:.2f}")
                self.last_fps_log_time = now

        self.image = None

    def set_image(self, image):
        self.image = image
