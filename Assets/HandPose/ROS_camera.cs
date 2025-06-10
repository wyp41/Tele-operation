using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class ImageSubscriber : MonoBehaviour
{
    //public string topicName = "/camera/image_raw";
    public string topicName = "/camera/compressed_image";
    public RawImage rawImage; // Assign this in the Unity Editor

    private ROSConnection ros;
    private Texture2D texture;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<CompressedImageMsg>(topicName, ReceiveImage);

        texture = new Texture2D(2, 2);
        //rawImage.texture = texture; // 将初始纹理绑定到 RawImage
    }

    void ReceiveImage(CompressedImageMsg imageMessage)
    {
        // 将 ROS 图像消息数据转换为纹理
        uint secs = imageMessage.header.stamp.sec;
        uint nsecs = imageMessage.header.stamp.nanosec;

        // 转换为DateTime
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double totalSeconds = (double)secs + (double)nsecs / 1e9;
        DateTime stampTime = epoch.AddSeconds(totalSeconds);

        // 获取当前时间
        DateTime nowTime = DateTime.UtcNow;

        // 计算延迟
        TimeSpan latency = nowTime - stampTime;
        double latencyMs = latency.TotalMilliseconds;

        // 输出结果
        if (latencyMs >= 0)
            Debug.Log($"图像延迟: {latencyMs:F2} 毫秒");
        else
            Debug.LogWarning("接收到未来时间戳，时钟可能不同步！");
        byte[] imageData = imageMessage.data;
        texture.LoadImage(imageData);
        rawImage.texture = texture;

        //texture.LoadRawTextureData(imageData);
        //texture.Apply();
    }
}