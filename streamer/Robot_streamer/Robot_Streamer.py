import time
import struct
import zmq
import numpy as np

from hardware_franka import ControlType, FrankaArm
from hardware_frankahand import FrankaHand

import argparse


parser = argparse.ArgumentParser(description="Parameters for the program.")
parser.add_argument(
    "--sending",
    type=lambda x: (str(x).lower() == 'true'),  # True/False argument
    default=False,
    help="Sending data: True or False"
)
args = parser.parse_args()


SENDING = args.sending
STEP_HZ = 30
DT = 1.0 / STEP_HZ


# -----------------------------
# Robot Wrapper
# -----------------------------
class Robot:
    def __init__(self, name, ip, arm_port, gripper_port):
        self.arm = FrankaArm(
            name=f"{name}_arm",
            ip_address=ip,
            port=arm_port,
        )
        self.gripper = FrankaHand(
            name=f"{name}_gripper",
            ip_address=ip,
            port=gripper_port,
        )

    def connect(self, arm_control_type):
        self.arm.control_type = arm_control_type
        assert self.arm.connect()
        assert self.gripper.connect()

    def reset(self):
        self.arm.reset()
        self.gripper.reset()

    def close(self):
        self.arm.close()
        self.gripper.close()


# -----------------------------
# ZMQ Setup
# -----------------------------
def setup_zmq():
    ctx = zmq.Context.instance()

    joint_socket = ctx.socket(zmq.PUB)
    joint_socket.bind("tcp://192.168.0.38:7781")
    joint_topic = b"JointAngles"

    gripper_socket = ctx.socket(zmq.PUB)
    gripper_socket.bind("tcp://192.168.0.38:7782")
    gripper_topic = b"GripperCommand"

    return joint_socket, joint_topic, gripper_socket, gripper_topic


# -----------------------------
# Teleoperation Loop
# -----------------------------
def teleop_loop(leader: Robot, follower: Robot):
    joint_socket, joint_topic, grip_socket, grip_topic = setup_zmq()

    print("Teleoperation running — stop with Ctrl+C")

    next_time = time.time()

    try:
        while True:
            # --- Read leader ---
            leader_state = leader.arm.get_state()
            leader_grip_width = leader.gripper.get_sensors().item()

            # --- Arm teleop (JOINT SPACE) ---
            follower.arm.apply_commands(
                q_desired=leader_state.joint_pos,
                qd_desired=leader_state.joint_vel,
            )

            # --- Gripper teleop (BINÄR, wie Original) ---
            thresh = leader.gripper.robot.metadata.max_width / 2
            follower_gripper_cmd = -1 if leader_grip_width < thresh else 1
            follower.gripper.apply_commands(follower_gripper_cmd)

            # --- ZMQ publish (IDENTISCH ZUM ORIGINAL) ---
            if SENDING:
                joint_pos_np = leader_state.joint_pos.numpy().astype(np.float32)
                joint_socket.send_multipart([
                    joint_topic,
                    joint_pos_np.tobytes()
                ])

                follower_grip_width = follower.gripper.get_sensors().item()
                grip_msg = struct.pack("<f", float(follower_grip_width * 1.25))
                grip_socket.send_multipart([
                    grip_topic,
                    grip_msg
                ])

            # --- Timing ---
            next_time += DT
            sleep_time = next_time - time.time()
            if sleep_time > 0:
                time.sleep(sleep_time)
            else:
                next_time = time.time()

    except KeyboardInterrupt:
        print("\nStopping teleoperation...")


# -----------------------------
# Main
# -----------------------------
if __name__ == "__main__":

    leader = Robot(
        name="p2_leader",
        ip="172.16.2.1",
        arm_port=4321,
        gripper_port=4322,
    )

    follower = Robot(
        name="p1_follower",
        ip="172.16.1.1",
        arm_port=1234,
        gripper_port=1235,
    )

    leader.connect(ControlType.HUMAN_CONTROL)
    follower.connect(ControlType.IMITATION_CONTROL)

    leader.reset()
    follower.reset()

    try:
        teleop_loop(leader, follower)
    finally:
        leader.close()
        follower.close()
        print("Hardware connections closed")
