using UnityEngine;

public class FishAvoidanceDebug : MonoBehaviour
{
    [SerializeField] private bool showRaycast = false;
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private LayerMask obstacleLayers;

    void Update()
    {
        if (!showRaycast) return;

        // Forward direction
        Debug.DrawRay(transform.position, transform.forward * rayLength, Color.blue);

        // 45° left and right
        Debug.DrawRay(transform.position,
            Quaternion.Euler(0, -45, 0) * transform.forward * rayLength, Color.green);
        Debug.DrawRay(transform.position,
            Quaternion.Euler(0, 45, 0) * transform.forward * rayLength, Color.green);

        // 90° left and right
        Debug.DrawRay(transform.position,
            Quaternion.Euler(0, -90, 0) * transform.forward * rayLength, Color.yellow);
        Debug.DrawRay(transform.position,
            Quaternion.Euler(0, 90, 0) * transform.forward * rayLength, Color.yellow);

        // Check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayLength, obstacleLayers))
        {
            Debug.DrawLine(transform.position, hit.point, Color.red);
            // Draw the normal at the hit point
            Debug.DrawRay(hit.point, hit.normal * 1.0f, Color.magenta);
            // Draw the reflection direction
            Vector3 reflection = Vector3.Reflect(transform.forward, hit.normal);
            Debug.DrawRay(hit.point, reflection * 1.0f, Color.cyan);
        }
    }
}
