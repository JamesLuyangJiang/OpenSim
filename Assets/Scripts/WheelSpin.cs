using SWS;
using UnityEngine;

public class WheelSpin : MonoBehaviour
{
    private Rigidbody carRigidbody;
    private splineMove splineMoveComponent;
    private float wheelRadius;
    private Transform wheelsHubs; // Reference to WheelsHubs object
    private WheelCollider wheelCollider;

    // Store cumulative spin angle for wheel rolling
    private float spinAngle = 0f;

    void Start()
    {
        // [Initialization code remains unchanged...]
        carRigidbody = GetComponentInParent<Rigidbody>();
        if (carRigidbody == null)
        {
            Debug.LogError($"WheelSpin: No Rigidbody found in parent objects of {gameObject.name}");
            enabled = false;
            return;
        }

        splineMoveComponent = carRigidbody.GetComponent<splineMove>();
        if (splineMoveComponent == null)
        {
            Debug.LogError($"WheelSpin: No splineMove component found on {carRigidbody.gameObject.name}");
            enabled = false;
            return;
        }

        wheelsHubs = carRigidbody.transform.Find("WheelsHubs");
        if (wheelsHubs == null)
        {
            Debug.LogError($"WheelSpin: No WheelsHubs object found under {carRigidbody.gameObject.name}");
            enabled = false;
            return;
        }

        wheelCollider = FindMatchingWheelCollider();
        if (wheelCollider != null)
        {
            wheelRadius = wheelCollider.radius;
        }
        else
        {
            Debug.LogError($"WheelSpin: No matching WheelCollider found for {gameObject.name}. Defaulting to 0.3m");
            wheelRadius = 0.3f;
        }
    }

    void Update()
    {
        if (splineMoveComponent == null || wheelCollider == null) return;

        // Get car speed and calculate the spin angular speed.
        float carSpeed = splineMoveComponent.speed;
        float angularSpeed = (carSpeed / wheelRadius) * Mathf.Rad2Deg;
        spinAngle += (angularSpeed / 5f) * Time.deltaTime;
        spinAngle %= 360f;  // Keep it between 0 and 360.

        // For front wheels, calculate the steering angle.
        float steerAngle = 0f;
        if (gameObject.name.Contains("Front"))
        {
            Vector3 carPos = carRigidbody.transform.position;
            Vector3 carForward = carRigidbody.transform.forward;

            int waypointCount = splineMoveComponent.pathContainer.GetWaypointCount();
            
            if (splineMoveComponent.currentPoint >= waypointCount - 1)
            {
                // At the final waypoint: use the last segment's direction.
                int prevIndex = Mathf.Max(splineMoveComponent.currentPoint - 1, 0);
                Vector3 lastDir = (splineMoveComponent.pathContainer.GetWaypoint(splineMoveComponent.currentPoint).position -
                                splineMoveComponent.pathContainer.GetWaypoint(prevIndex).position).normalized;
                steerAngle = Vector3.SignedAngle(carForward, lastDir, Vector3.up);
            }
            else
            {
                // Normal case: use the next waypoint.
                int nextIndex = splineMoveComponent.currentPoint + 1;
                Vector3 nextWaypointPos = splineMoveComponent.pathContainer.GetWaypoint(nextIndex).position;
                Vector3 directionToNext = (nextWaypointPos - carPos).normalized;
                steerAngle = Vector3.SignedAngle(carForward, directionToNext, Vector3.up);
            }
        }

        // Create a rotation for the spin (around X) and for the steering (around Y).
        Quaternion spinRotation = Quaternion.Euler(spinAngle, 0, 0);
        Quaternion steerRotation = Quaternion.Euler(0, steerAngle, 0);

        // Combine them: apply steering first, then the wheel spin.
        transform.localRotation = steerRotation * spinRotation;
    }

    private WheelCollider FindMatchingWheelCollider()
    {
        foreach (Transform child in wheelsHubs)
        {
            if (child.name.Contains("WheelHub")) // Look for "WheelHub" objects
            {
                WheelCollider wc = child.GetComponent<WheelCollider>();
                if (wc != null && AreWheelsMatching(this.gameObject, child.gameObject))
                {
                    return wc;
                }
            }
        }
        return null;
    }

    private bool AreWheelsMatching(GameObject wheelMesh, GameObject wheelColliderObject)
    {
        // Match "WheelFrontRight" with "WheelHubFrontRight"
        string expectedHubName = wheelMesh.name.Replace("Wheel", "WheelHub");
        return wheelColliderObject.name == expectedHubName;
    }
}
