// CameraFollow.cs — gắn vào Main Camera
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset;

    [Header("Map Bounds")]
    public bool useBounds = true;
    public float minX, maxX;
    public float minY, maxY;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;

        if (useBounds && cam != null)
        {
            // Tính half size của camera
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            // Clamp vị trí camera trong bounds
            desiredPos.x = Mathf.Clamp(desiredPos.x, minX + halfWidth, maxX - halfWidth);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minY + halfHeight, maxY - halfHeight);
        }

        desiredPos.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    // Vẽ bounds trong Scene view để dễ chỉnh
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.green;
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}