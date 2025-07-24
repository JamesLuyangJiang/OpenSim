using UnityEngine;
using static V2X.RsuCmd;

namespace V2X
{
    /// <summary>
    /// Example script showing how to integrate V2X system with vehicle controllers.
    /// This should be attached to vehicles that need V2X communication capabilities.
    /// </summary>
    [RequireComponent(typeof(V2XRadio))]
    public class V2XExample : MonoBehaviour
    {
        [Header("V2X Components")]
        [Tooltip("Reference to the V2X radio component")]
        public V2XRadio radio;
        
        [Header("Intersection Detection")]
        [Tooltip("Layer mask for intersection triggers")]
        public LayerMask intersectionLayer = -1;
        
        [Header("Behavior")]
        [Tooltip("Distance from intersection to start requesting entry")]
        public float requestDistance = 10f;
        
        // Private state
        private IntersectionRSU currentRSU;
        private bool hasRequestedEntry = false;
        private bool hasCleared = false;
        
        void Start()
        {
            // Get the V2X radio component
            if (radio == null)
                radio = GetComponent<V2XRadio>();
                
            // Set a unique vehicle ID (in a real system, this would be assigned by a manager)
            radio.VehicleId = Random.Range(1000, 9999);
        }
        
        void Update()
        {
            // Check for nearby intersections
            CheckForIntersections();
            
            // Handle intersection logic
            HandleIntersectionLogic();
        }
        
        /// <summary>
        /// Check for nearby intersections and detect when entering/exiting
        /// </summary>
        void CheckForIntersections()
        {
            // Simple sphere cast to detect intersections
            Collider[] intersections = Physics.OverlapSphere(transform.position, requestDistance, intersectionLayer);
            
            IntersectionRSU nearestRSU = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var intersection in intersections)
            {
                var rsu = intersection.GetComponent<IntersectionRSU>();
                if (rsu != null)
                {
                    float distance = Vector3.Distance(transform.position, intersection.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestRSU = rsu;
                    }
                }
            }
            
            // Update current RSU
            if (nearestRSU != currentRSU)
            {
                // Exiting previous intersection
                if (currentRSU != null && !hasCleared)
                {
                    radio.SendToRsu(Clear, currentRSU);
                    hasCleared = true;
                }
                
                // Entering new intersection
                currentRSU = nearestRSU;
                hasRequestedEntry = false;
                hasCleared = false;
            }
        }
        
        /// <summary>
        /// Handle intersection entry requests and grants
        /// </summary>
        void HandleIntersectionLogic()
        {
            if (currentRSU == null) return;
            
            // Request entry if we haven't already
            if (!hasRequestedEntry)
            {
                radio.SendToRsu(RequestEntry, currentRSU);
                hasRequestedEntry = true;
                Debug.Log($"Vehicle {radio.VehicleId} requesting entry to intersection");
            }
            
            // Check if we received a grant
            if (radio.GrantReceived)
            {
                Debug.Log($"Vehicle {radio.VehicleId} received grant - proceeding through intersection");
                // In a real implementation, you might control the vehicle's behavior here
                // For example, allow the vehicle to proceed or adjust speed
            }
        }
        
        /// <summary>
        /// Reset grant status when exiting intersection
        /// </summary>
        void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<IntersectionRSU>() != null)
            {
                radio.ResetGrantStatus();
                Debug.Log($"Vehicle {radio.VehicleId} exited intersection");
            }
        }
        
        // Optional: Visual debugging
        void OnDrawGizmosSelected()
        {
            if (radio != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, requestDistance);
                
                if (currentRSU != null)
                {
                    Gizmos.color = radio.GrantReceived ? Color.green : Color.yellow;
                    Gizmos.DrawLine(transform.position, currentRSU.transform.position);
                }
            }
        }
    }
} 