using UnityEngine;

public class RotateAndMove : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 50, 0); // tốc độ xoay
    public Transform pointA; // điểm đầu
    public Transform pointB; // điểm cuối
    [SerializeField] private float moveSpeed = 2f; // tốc độ di chuyển

    private Transform targetPoint;

    void Start()
    {
        targetPoint = pointB; // bắt đầu di chuyển về pointB
    }

    void Update()
    {
        // Xoay liên tục
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // Di chuyển tới targetPoint
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

        // Đổi hướng nếu đã đến nơi
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            targetPoint = (targetPoint == pointA) ? pointB : pointA;
        }
    }
}
