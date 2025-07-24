// using UnityEngine;
// using static V2X.RsuCmd;

// namespace V2X
// {
//     /// <summary>
//     /// Zone that triggers V2X communication when vehicles enter.
//     /// Place this before stop signs or yield signs to initiate intersection requests.
//     /// </summary>
//     [RequireComponent(typeof(BoxCollider))]
//     public class StopYieldZone : MonoBehaviour
//     {
//         [Header("Zone Settings")]
//         [Tooltip("Type of zone - determines vehicle behavior")]
//         public ZoneType zoneType = ZoneType.Stop;
        
//         [Tooltip("Reference to the RSU that manages this intersection")]
//         public IntersectionRSU targetRSU;
        
//         [Tooltip("Distance from zone to start requesting entry")]
//         public float requestDistance = 5f;

//         public enum ZoneType
//         {
//             Stop,   // Vehicle must stop and wait for grant
//             Yield   // Vehicle may slow down or stop based on grant
//         }

//         void OnTriggerEnter(Collider other)
//         {
//             // Check if this is a vehicle with V2X capabilities
//             var radio = other.GetComponentInParent<V2XRadio>();
//             if (radio == null) return;

//             // Check if this is a vehicle with TrafficControl
//             var trafficControl = other.GetComponentInParent<TrafficControl>();
//             if (trafficControl == null) return;

//             // Trigger V2X communication
//             if (targetRSU != null)
//             {
//                 Debug.Log($"Vehicle {radio.VehicleId} entered {zoneType} zone");
                
//                 // Send request to RSU
//                 radio.SendToRsu(RequestEntry, targetRSU);
                
//                 // Notify TrafficControl about the zone entry
//                 trafficControl.OnEnterStopYieldZone((int)zoneType, targetRSU);
//             }
//             else
//             {
//                 Debug.LogWarning($"StopYieldZone {name} has no target RSU assigned!");
//             }
//         }

//         void OnTriggerExit(Collider other)
//         {
//             // Check if this is a vehicle with V2X capabilities
//             var radio = other.GetComponentInParent<V2XRadio>();
//             if (radio == null) return;

//             // Check if this is a vehicle with TrafficControl
//             var trafficControl = other.GetComponentInParent<TrafficControl>();
//             if (trafficControl == null) return;

//             // Notify TrafficControl about zone exit
//             trafficControl.OnExitStopYieldZone();
//         }

//         // Visual debugging
//         void OnDrawGizmosSelected()
//         {
//             if (targetRSU != null)
//             {
//                 Gizmos.color = zoneType == ZoneType.Stop ? Color.red : Color.yellow;
//                 Gizmos.DrawLine(transform.position, targetRSU.transform.position);
//             }
//         }
//     }
// } 