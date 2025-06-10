using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawArrow : MonoBehaviour
{
    public Transform startPoint; // 箭头起点
    public Transform endPoint;   // 箭头终点
    public float arrowHeadLength = 0.5f; // 箭头头部长度
    public float arrowHeadAngle = 20f;  // 箭头头部角度

    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.002f;
        lineRenderer.endWidth = 0.002f;
    }

    private void Update()
    {
        if (startPoint != null && endPoint != null)
        {
            DrawArrowBetweenPoints(startPoint.position, endPoint.position);
        }
    }

    private void DrawArrowBetweenPoints(Vector3 start, Vector3 end)
    {
        // 计算箭头方向
        Vector3 direction = (end - start).normalized;

        // 计算箭头头部的两个边
        Vector3 rightHead = Quaternion.Euler(0, 0, arrowHeadAngle) * -direction * arrowHeadLength;
        Vector3 leftHead = Quaternion.Euler(0, 0, -arrowHeadAngle) * -direction * arrowHeadLength;

        // 设置 LineRenderer 的顶点数：主线 + 两条箭头边
        lineRenderer.positionCount = 5;

        // 设置箭头主线的起点和终点
        lineRenderer.SetPosition(0, start); // 主线起点
        lineRenderer.SetPosition(1, end);   // 主线终点

        // 设置箭头尖端的两条边
        lineRenderer.SetPosition(2, end + rightHead); // 箭头右边
        lineRenderer.SetPosition(3, end);            // 返回到箭头顶点
        lineRenderer.SetPosition(4, end + leftHead); // 箭头左边
    }
}