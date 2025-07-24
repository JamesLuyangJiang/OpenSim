// using UnityEngine;

// namespace V2X
// {
//     /// <summary>
//     /// Road Side Unit for yield sign intersections.
//     /// Manages vehicle access based on yield sign rules:
//     /// Vehicles must yield to traffic already in the intersection with a minimum time gap.
//     /// </summary>
//     public class YieldRSU : IntersectionRSU
//     {
//         [Header("Yield Parameters")]
//         [Tooltip("Minimum time gap required before entering intersection (seconds)")]
//         public float yieldGapSec = 2f;

//         /// <summary>
//         /// Determine if it's safe for a vehicle to enter the intersection based on yield sign rules
//         /// </summary>
//         protected override bool IsSafeToEnter(int vehId)
//         {
//             // Check if any vehicle in the intersection will reach us within the yield gap
//             foreach (int other in inside)
//                 if (other != vehId && TTC(other) < yieldGapSec) return false;
//             return true;
//         }

//         /// <summary>
//         /// Calculate Time-To-Collision (TTC) with another vehicle
//         /// </summary>
//         float TTC(int otherVehId)
//         {
//             var r = V2XBus.I?.FindRadioByVehicleId(otherVehId);
//             if (r == null) return 999;
//             var rel = (Vector3)transform.position - r.transform.position;
//             // TODO: confirm this getcomponent is correct
//             float closing = Vector3.Dot(rel, r.GetComponent<Rigidbody>().velocity.normalized);
//             return closing <= 0.1f ? 999 : rel.magnitude / closing;
//         }
//     }
// }
