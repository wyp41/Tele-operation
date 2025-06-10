using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Holofunk.HandPose;
using Microsoft.MixedReality.Toolkit.UI;
using RosMessageTypes.Geometry;
using RosMessageTypes.UrRobotDriver;
using RosMessageTypes.Std;
using Unity.Mathematics;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEditor.PlayerSettings;
using RosMessageTypes.BuiltinInterfaces;

public class velocity_ctrl : MonoBehaviour
{
    // Hardcoded variables
    const int k_NumRobotJoints = 6;
    const float k_JointAssignmentWait = 0.001f;
    //const float k_PoseAssignmentWait = 0.1f;
    public int run = 1;

    // Variables required for ROS communication
    public string m_RosServiceName = "velocity_ctrl";
    public string topicName = "/sent_time";

    public Vector3 pos_velocity;
    public Vector3 quat_velocity;
    public int hand_status;

    [SerializeField]

    public GameObject Hand;

    public Transform head;

    public Transform real_view;

    public Transform start_point;

    public Transform end_point;

    // ROS Connector
    ROSConnection m_Ros;

    Vector3 prev_pos;

    Vector3 prev_rot;

    Vector3 cur_pos;

    Vector3 cur_rot;

    Vector2 arrow;

    Vector3 prev_head;

    int hands = 0;

    float z_platform = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterRosService<velocityServiceRequest, velocityServiceResponse>(m_RosServiceName);
        print(m_RosServiceName);
        m_Ros.RegisterPublisher<HeaderMsg>(topicName);

        pos_velocity = new Vector3(0, 0, 0);
        quat_velocity = new Vector3(0, 0, 0);
        arrow = new Vector2(0, 0);
        z_platform = real_view.localPosition.z;
        var realView_pos = real_view.localPosition;
        realView_pos.x = -10;
        realView_pos.y = 10;
        //prev_pos = Hand.transform.position;
        //prev_rot = Hand.transform.rotation.eulerAngles;
        //cur_pos = Hand.transform.position;
        //cur_rot = Hand.transform.rotation.eulerAngles;
        prev_pos = Hand.transform.localPosition;
        prev_rot = Hand.transform.localRotation.eulerAngles;
        cur_pos = Hand.transform.localPosition;
        cur_rot = Hand.transform.localRotation.eulerAngles;
        prev_head = head.transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_RosServiceName == "right_hand")
        {
            var realView_pos = real_view.localPosition;
            realView_pos.z = z_platform;
            if (realView_pos.x > 0.75f) { realView_pos.x = 0.75f; }
            else if (realView_pos.x < -0.75f) { realView_pos.x = -0.75f; }
            if (realView_pos.y < -0.75f) { realView_pos.y = -0.75f; }
            else if (realView_pos.y > 0.75f) { realView_pos.y = 0.75f; }
            real_view.localPosition = realView_pos;
        }
            
        if (run != 0)
        {
            run = 0;
            //var current_pos = Hand.transform.position;
            //var current_quat = Hand.transform.rotation;

            var current_pos = Hand.transform.localPosition;
            var current_quat = Hand.transform.localRotation;
            

            if (m_RosServiceName == "right_hand")
            {
                var start_position = real_view.localPosition;
                //start_position.x += 0.0f;
                start_point.localPosition = start_position;
                Vector3 end_position = Vector3.zero;
                var norm_arrow = arrow.normalized;
                end_position.x = start_position.x + norm_arrow.x / 10.0f;
                end_position.y = start_position.y - norm_arrow.y / 10.0f;
                end_position.z = start_position.z;
                //print(arrow[0]);
                //print(arrow[1]);
                end_point.localPosition = end_position;
            }

            cur_pos = current_pos;
            cur_rot = current_quat.eulerAngles;

            var hand_status = Hand.GetComponent<HandPoseVisualizer>().hand_status;

            if (hand_status == HandPose.Opened) { hands = 1; }
            else if (hand_status == HandPose.Closed) { hands = 2; }
            else if (hand_status == HandPose.Bloom) { hands = 3; }
            else if (hand_status == HandPose.Flat) { hands = 4; }
            else { hands = 0; }

            var joint_input = new MoveitJointsMsg();
            joint_input.joints[0] = hands;
            for (var i = 0; i < 2; i++)
            {
                joint_input.joints[i+1] = 0;
            }

            var head_rot = head.rotation.eulerAngles;
            if (math.abs(head_rot[1]) > 90)
            {
                head_rot[1] = math.abs(head_rot[1]) - 360;
            }
            if (math.abs(head_rot[0]) > 90)
            {
                head_rot[0] = math.abs(head_rot[0]) - 360;
            }
            if (math.abs(head_rot[2]) > 180)
            {
                head_rot[2] = math.abs(head_rot[2]) - 360;
            }
            //joint_input.joints[3] = head_rot.z - prev_head.z;
            //joint_input.joints[4] = -(head_rot.x - prev_head.x);
            //joint_input.joints[5] = head_rot.y - prev_head.y;
            joint_input.joints[3] = head_rot.z;
            joint_input.joints[4] = -head_rot.x;
            joint_input.joints[5] = head_rot.y;
            prev_head = head_rot;

            //print(joint_input.joints[3]);
            //print(joint_input.joints[4]);
            //print(joint_input.joints[5]);


            //PoseMsg target_pose = new PoseMsg
            //{
            //    position = current_pos.To<FLU>(),
            //    orientation = current_quat.To<FLU>()
            //};

            PoseMsg target_pose = new PoseMsg
            {
                position = real_view.localPosition.To<FLU>(),
                orientation = current_quat.To<FLU>()
            };

            var request = new velocityServiceRequest();
            request.joints_input = joint_input;
            request.velocity_input = velocity_cal();
            request.target_pose = target_pose;

            PublishCurrentTimestamp();
            m_Ros.SendServiceMessage<velocityServiceResponse>(m_RosServiceName, request, JointVelocityResponse);
        }
    }

    void PublishCurrentTimestamp()
    {
        System.DateTime unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        System.DateTime currentTime = System.DateTime.UtcNow;

        // 转换为 UNIX 时间戳（秒）
        double unixTime = (currentTime - unixEpoch).TotalSeconds;

        // 将时间戳分为秒和纳秒
        uint secs = (uint)unixTime;
        uint nsecs = (uint)((unixTime - secs) * 1e9);

        // 创建 Header 消息
        HeaderMsg headerMessage = new HeaderMsg
        {
            seq = 0,                // 序列号（可以递增，也可以忽略）
            stamp = new TimeMsg     // 设置时间戳
            {
                sec = secs,
                nanosec = nsecs
            },
            frame_id = "unity_frame" // 可选的帧 ID
        };

        // 发布消息
        m_Ros.Publish(topicName, headerMessage);
        //Debug.Log($"Published timestamp: {secs}.{nsecs}");
    }

    MoveitJointsMsg velocity_cal()
    {
        var vels = new MoveitJointsMsg();

        for (var i = 0; i < 3; i++)
        {
            //vels.joints[i] = cur_pos[i] - prev_pos[i];
            //vels.joints[i+3] = cur_rot[i] - prev_rot[i];
            vels.joints[i] = cur_pos[i];
            vels.joints[i + 3] = cur_rot[i];
            prev_pos[i] = cur_pos[i];
            prev_rot[i] = cur_rot[i];
        }

        return vels;
    }

    void JointVelocityResponse(velocityServiceResponse response)
    {
        if (response.velocity_output.joints.Length > 0)
        {
            //print(response.velocity_output.joints);
            arrow[0] = (float) response.velocity_output.joints[0];
            arrow[1] = (float) response.velocity_output.joints[1];

            //print(arrow[0]);
            //print(arrow[1]);
            //Debug.Log("velocity returned.");
            //StartCoroutine(ExecuteJointVelocity(response));
        }
        else
        {
            Debug.LogError("No trajectory returned from MoverService.");
        }
        run = 1;
    }

    IEnumerator ExecuteJointVelocity(velocityServiceResponse response)
    {
        yield return new WaitForSeconds(k_JointAssignmentWait);
    }
}
