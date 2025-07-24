// using UnityEngine;
// using UnityEngine.Events;
// using System.Collections;
// using System.Collections.Generic;
// using DG.Tweening;
// using SWS;
// using UnityEngine.AI;
// using static V2X.RsuCmd;

// namespace V2X
// {
//     /// <summary>
//     /// Enhanced TrafficControl script that integrates V2X intersection management.
//     /// Handles stop signs, yield signs, traffic lights, and obstacle detection.
//     /// </summary>
//     [RequireComponent(typeof(V2XRadio))]
//     public class V2XTrafficControl : MonoBehaviour
//     {
//         [Header("V2X Components")]
//         [Tooltip("Reference to the V2X radio component")]
//         public V2XRadio radio;

//         [Header("V2X Intersection Settings")]
//         [Tooltip("Layer mask for intersection triggers")]
//         public LayerMask intersectionLayer = -1;
//         [Tooltip("Distance from intersection to start requesting entry")]
//         public float requestDistance = 15f;
//         [Tooltip("Distance from intersection to start slowing down for yield")]
//         public float yieldSlowDistance = 20f;
//         [Tooltip("Speed reduction factor for yield signs (0.5 = half speed)")]
//         [Range(0.1f, 1.0f)]
//         public float yieldSpeedFactor = 0.6f;

//         [Header("Stop Sign Settings")]
//         [Tooltip("Distance from stop sign to start stopping")]
//         public float stopDistance = 10f;
//         [Tooltip("Duration for stop sign deceleration")]
//         public float stopDecelDuration = 2.0f;

//         // Original TrafficControl components
//         private RadarCast radarCastScript;
//         private splineMove moveRef;

//         // V2X State
//         private IntersectionRSU currentRSU;
//         private bool hasRequestedEntry = false;
//         private bool hasCleared = false;
//         private bool isWaitingForGrant = false;
//         private bool isAtStopSign = false;
//         private bool isAtYieldSign = false;

//         // Original TrafficControl state
//         private bool pausedForCollision = false;
//         private bool pausedForTrafficLight = false;
//         private float randomDelay;
//         public float originalSpeed = 15.0f;
//         private bool isSpeedingUp = false;

//         // Coroutines
//         private Coroutine speedChangeCoroutine = null;
//         private Coroutine trafficLightCoroutine = null;
//         private Coroutine resumeCoroutine = null;
//         private Coroutine v2xStopCoroutine = null;
//         private Coroutine v2xYieldCoroutine = null;

//         // Global variables
//         private float radarSlowDownRange = 20.0f;
//         private float radarStopRange = 7.0f;
//         private float speedChangeDuration = 2.0f;
//         private bool obstacleDetected = false;
//         private float stopDistanceOriginal = 15.0f;

//         void Start()
//         {
//             // Initialize V2X components
//             if (radio == null)
//                 radio = GetComponent<V2XRadio>();
            
//             // Set unique vehicle ID
//             radio.VehicleId = Random.Range(1000, 9999);

//             // Initialize original TrafficControl components
//             Transform radarTransform = transform.Find("Radars/LongRangeFR");
//             if (radarTransform != null)
//                 radarCastScript = radarTransform.GetComponent<RadarCast>();

//             if (radarCastScript == null)
//                 Debug.LogError("RadarCast script not found in Radars/LongRangeFR");

//             moveRef = GetComponentInParent<splineMove>();
//             if (moveRef == null)
//                 Debug.LogError("splineMove script not found on this object.");

//             // Ensure the car starts at full speed (randomized)
//             if (moveRef != null)
//             {
//                 originalSpeed = Random.Range(15.0f, 20.0f);
//                 moveRef.ChangeSpeed(originalSpeed);
//             }

//             // Assign a random small delay for re-acceleration
//             randomDelay = Random.Range(0.1f, 0.5f);
//         }

//         void Update()
//         {
//             // Handle V2X intersection logic
//             HandleV2XIntersections();
            
//             // Handle original obstacle detection (always active for player vehicle avoidance)
//             HandleObstacleDetection();
//         }

//         /// <summary>
//         /// Handle V2X intersection detection and management
//         /// </summary>
//         void HandleV2XIntersections()
//         {
//             // Check for nearby intersections
//             Collider[] intersections = Physics.OverlapSphere(transform.position, requestDistance, intersectionLayer);
            
//             IntersectionRSU nearestRSU = null;
//             float nearestDistance = float.MaxValue;
            
//             foreach (var intersection in intersections)
//             {
//                 var rsu = intersection.GetComponent<IntersectionRSU>();
//                 if (rsu != null)
//                 {
//                     float distance = Vector3.Distance(transform.position, intersection.transform.position);
//                     if (distance < nearestDistance)
//                     {
//                         nearestDistance = distance;
//                         nearestRSU = rsu;
//                     }
//                 }
//             }
            
//             // Update current RSU
//             if (nearestRSU != currentRSU)
//             {
//                 // Exiting previous intersection
//                 if (currentRSU != null && !hasCleared)
//                 {
//                     radio.SendToRsu(Clear, currentRSU);
//                     hasCleared = true;
//                     ResetV2XState();
//                 }
                
//                 // Entering new intersection
//                 currentRSU = nearestRSU;
//                 hasRequestedEntry = false;
//                 hasCleared = false;
//             }

//             // Handle intersection logic
//             if (currentRSU != null)
//             {
//                 HandleIntersectionLogic();
//             }
//         }

//         /// <summary>
//         /// Handle intersection entry requests and grants
//         /// </summary>
//         void HandleIntersectionLogic()
//         {
//             float distanceToIntersection = Vector3.Distance(transform.position, currentRSU.transform.position);
            
//             // Request entry if we haven't already
//             if (!hasRequestedEntry && distanceToIntersection <= requestDistance)
//             {
//                 radio.SendToRsu(RequestEntry, currentRSU);
//                 hasRequestedEntry = true;
//                 isWaitingForGrant = true;
//                 Debug.Log($"Vehicle {radio.VehicleId} requesting entry to intersection");
//             }

//             // Handle different intersection types
//             if (currentRSU is StopRSU)
//             {
//                 HandleStopSign(distanceToIntersection);
//             }

//         }

//         /// <summary>
//         /// Handle stop sign intersection logic
//         /// </summary>
//         void HandleStopSign(float distanceToIntersection)
//         {
//             if (distanceToIntersection <= stopDistance && !isAtStopSign)
//             {
//                 isAtStopSign = true;
//                 StopV2XStopCoroutine();
                
//                 // Always stop at stop sign, regardless of grant
//                 v2xStopCoroutine = StartCoroutine(StopAtStopSign());
//             }

//             // If we have a grant and we're stopped, proceed
//             if (radio.GrantReceived && isAtStopSign && moveRef.speed < 0.1f)
//             {
//                 Debug.Log($"Vehicle {radio.VehicleId} proceeding through stop sign");
//                 isAtStopSign = false;
//                 StopV2XStopCoroutine();
//                 ResumeAfterV2XStop();
//             }
//         }

//         /// <summary>
//         /// Handle yield sign intersection logic
//         /// </summary>
//         void HandleYieldSign(float distanceToIntersection)
//         {
//             if (distanceToIntersection <= yieldSlowDistance && !isAtYieldSign)
//             {
//                 isAtYieldSign = true;
                
//                 if (radio.GrantReceived)
//                 {
//                     // Grant received - slow down but don't stop
//                     Debug.Log($"Vehicle {radio.VehicleId} slowing down for yield sign (granted)");
//                     SlowDownForYield();
//                 }
//                 else
//                 {
//                     // No grant - stop and wait
//                     Debug.Log($"Vehicle {radio.VehicleId} stopping for yield sign (no grant)");
//                     StopV2XYieldCoroutine();
//                     v2xYieldCoroutine = StartCoroutine(StopAtYieldSign());
//                 }
//             }

//             // If we get a grant while waiting, proceed
//             if (radio.GrantReceived && isAtYieldSign && moveRef.speed < 0.1f)
//             {
//                 Debug.Log($"Vehicle {radio.VehicleId} proceeding through yield sign");
//                 isAtYieldSign = false;
//                 StopV2XYieldCoroutine();
//                 ResumeAfterV2XYield();
//             }
//         }

//         /// <summary>
//         /// Coroutine to stop at stop sign
//         /// </summary>
//         IEnumerator StopAtStopSign()
//         {
//             float currentSpeed = moveRef.speed;
//             yield return StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, stopDecelDuration));
//             moveRef.Pause();
//         }

//         /// <summary>
//         /// Coroutine to stop at yield sign
//         /// </summary>
//         IEnumerator StopAtYieldSign()
//         {
//             float currentSpeed = moveRef.speed;
//             yield return StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, stopDecelDuration));
//             moveRef.Pause();
//         }

//         /// <summary>
//         /// Slow down for yield sign when granted
//         /// </summary>
//         void SlowDownForYield()
//         {
//             float targetSpeed = originalSpeed * yieldSpeedFactor;
//             StopV2XYieldCoroutine();
//             v2xYieldCoroutine = StartCoroutine(SmoothChangeSpeed(moveRef.speed, targetSpeed, 1.0f));
//         }

//         /// <summary>
//         /// Resume after stop sign
//         /// </summary>
//         void ResumeAfterV2XStop()
//         {
//             moveRef.Resume();
//             StopV2XStopCoroutine();
//             v2xStopCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
//         }

//         /// <summary>
//         /// Resume after yield sign
//         /// </summary>
//         void ResumeAfterV2XYield()
//         {
//             moveRef.Resume();
//             StopV2XYieldCoroutine();
//             v2xYieldCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
//         }

//         /// <summary>
//         /// Reset V2X state when exiting intersection
//         /// </summary>
//         void ResetV2XState()
//         {
//             isAtStopSign = false;
//             isAtYieldSign = false;
//             isWaitingForGrant = false;
//             radio.ResetGrantStatus();
//         }

//         /// <summary>
//         /// Handle original obstacle detection logic
//         /// </summary>
//         void HandleObstacleDetection()
//         {
//             if (radarCastScript == null || moveRef == null)
//                 return;

//             // Get detected objects from radar
//             List<RadarCast.DetectedObject> detectedObjects = radarCastScript.detectedObjects;

//             // Check for obstacles too close
//             float closestObstacleDistance = radarSlowDownRange;
//             foreach (var detectedObject in detectedObjects)
//             {
//                 if (detectedObject.distance < closestObstacleDistance)
//                 {
//                     closestObstacleDistance = detectedObject.distance;
//                     obstacleDetected = true;
//                 }
//             }

//             if (closestObstacleDistance == radarSlowDownRange)
//                 obstacleDetected = false;

//             bool isPaused = pausedForTrafficLight || pausedForCollision || (moveRef.tween != null && moveRef.tween.IsPlaying() == false);

//             // Don't apply obstacle detection if we're at a V2X stop sign (prioritize V2X stops)
//             if (isAtStopSign)
//                 return;

//             if (!isPaused)
//             {
//                 float stopBuffer = 0.4f;
//                 // If close to the stop range, since speed is very low, we can just stop instead of sliding
//                 if (closestObstacleDistance < radarStopRange + stopBuffer)
//                 {
//                     moveRef.ChangeSpeed(0);
//                 }
//                 else
//                 {
//                     // Smooth speed adjustment based on closest obstacle
//                     float minDistance = radarStopRange;
//                     float maxDistance = radarSlowDownRange;
//                     float t = Mathf.InverseLerp(minDistance, maxDistance, closestObstacleDistance);
//                     float speedFactor = Mathf.SmoothStep(0, 1, t);
                    
//                     // For yield signs, don't go above the yield speed factor
//                     float maxSpeed = originalSpeed;
//                     if (isAtYieldSign && !radio.GrantReceived)
//                     {
//                         maxSpeed = originalSpeed * yieldSpeedFactor;
//                     }
                    
//                     float targetSpeed = Mathf.Lerp(0, maxSpeed, speedFactor);
//                     float minCrawlSpeed = 0f;
//                     targetSpeed = Mathf.Max(targetSpeed, minCrawlSpeed);
//                     float smoothness = 3.0f;
//                     float newSpeed = Mathf.Lerp(moveRef.speed, targetSpeed, Time.deltaTime * smoothness);
//                     moveRef.ChangeSpeed(newSpeed);
//                 }
//             }
//         }

//         // --------Coroutine Control--------
//         public void StopV2XStopCoroutine()
//         {
//             if (v2xStopCoroutine != null)
//             {
//                 StopCoroutine(v2xStopCoroutine);
//                 v2xStopCoroutine = null;
//             }
//         }

//         public void StopV2XYieldCoroutine()
//         {
//             if (v2xYieldCoroutine != null)
//             {
//                 StopCoroutine(v2xYieldCoroutine);
//                 v2xYieldCoroutine = null;
//             }
//         }

//         public void StopSpeedChangeCoroutine()
//         {
//             if (speedChangeCoroutine != null)
//             {
//                 StopCoroutine(speedChangeCoroutine);
//                 speedChangeCoroutine = null;
//             }
//         }

//         public void StopResumeCoroutine()
//         {
//             if (resumeCoroutine != null)
//             {
//                 StopCoroutine(resumeCoroutine);
//                 resumeCoroutine = null;
//             }
//         }

//         public new void StopAllCoroutines()
//         {
//             StopSpeedChangeCoroutine();
//             StopResumeCoroutine();
//             StopV2XStopCoroutine();
//             StopV2XYieldCoroutine();
//             if (trafficLightCoroutine != null)
//             {
//                 StopCoroutine(trafficLightCoroutine);
//                 trafficLightCoroutine = null;
//             }
//         }

//         // --------Helpers for smooth speed change--------
//         public IEnumerator ResumeAfterRandomDelay()
//         {
//             yield return new WaitForSeconds(randomDelay);

//             if (!pausedForTrafficLight)
//             {
//                 moveRef.Resume();

//                 // Ensure we always start from 0
//                 moveRef.ChangeSpeed(0);
//                 yield return new WaitForSeconds(0.1f); // Short wait to ensure no conflicting coroutine

//                 if (!isSpeedingUp)
//                 {
//                     isSpeedingUp = true;
//                     StopSpeedChangeCoroutine();
//                     speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
//                 }
//                 pausedForCollision = false;
//             }
//         }

//         public IEnumerator SmoothChangeSpeed(float fromSpeed, float toSpeed, float duration)
//         {
//             float elapsedTime = 0f;

//             while (elapsedTime < duration)
//             {
//                 float newSpeed = Mathf.Lerp(fromSpeed, toSpeed, elapsedTime / duration);
//                 moveRef.ChangeSpeed(newSpeed);
//                 elapsedTime += Time.deltaTime;
//                 yield return null;
//             }

//             moveRef.ChangeSpeed(toSpeed); // Ensure final speed is exact
//             isSpeedingUp = false;
//         }

//         // Overloaded version with callback
//         public IEnumerator SmoothChangeSpeed(float fromSpeed, float toSpeed, float duration, System.Action onComplete)
//         {
//             yield return StartCoroutine(SmoothChangeSpeed(fromSpeed, toSpeed, duration));
//             onComplete?.Invoke();
//         }

//         // --------Traffic Light Stop Control--------
//         // When vehicle passes a waypoint at traffic light, will trigger this function
//         public void PauseCar(Object target)
//         {
//             // Check if target is null
//             if (target == null)
//             {
//                 Debug.LogWarning("PauseCar called with null target");
//                 return;
//             }

//             GameObject targetGameObject = target as GameObject;
//             if (targetGameObject == null)
//             {
//                 Debug.LogWarning("PauseCar target is not a GameObject");
//                 return;
//             }

//             Renderer lightRenderer = targetGameObject.GetComponent<Renderer>();
//             if (lightRenderer == null)
//             {
//                 Debug.LogWarning("PauseCar target GameObject has no Renderer component");
//                 return;
//             }

//             // If the traffic light is RED and the car is not paused for collision or traffic light, and no obstacle is detected, then pause the car
//             if (moveRef.tween != null && lightRenderer.sharedMaterial != null && lightRenderer.sharedMaterial.name.EndsWith("OFF") && !pausedForTrafficLight && !obstacleDetected)
//             {
//                 // Stop any ongoing speed changes
//                 StopSpeedChangeCoroutine();
//                 StopResumeCoroutine();

//                 pausedForTrafficLight = true;

//                 float currentSpeed = moveRef.speed; // or originalSpeed
//                 float duration = 2 * stopDistanceOriginal / currentSpeed;

//                 // Use this duration in your SmoothChangeSpeed call
//                 speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration, () => {
//                     moveRef.Pause();
//                 }));

//                 if (trafficLightCoroutine != null) StopCoroutine(trafficLightCoroutine);
//                 trafficLightCoroutine = StartCoroutine(WaitForLightToTurnOn(lightRenderer, moveRef));
//             }
//         }

//         private IEnumerator WaitForLightToTurnOn(Renderer lightRenderer, splineMove moveRef)
//         {
//             // Wait until the material changes from lightOff
//             while (lightRenderer.sharedMaterial.name.EndsWith("OFF"))
//             {
//                 yield return new WaitForSeconds(0.5f);
//             }

//             pausedForTrafficLight = false;

//             // If not also paused for collision, then resume
//             if (!pausedForCollision)
//             {
//                 yield return new WaitForSeconds(randomDelay);
//                 moveRef.Resume();

//                 // Smoothly accelerate from 0 to original speed
//                 if (!isSpeedingUp)
//                 {
//                     isSpeedingUp = true;
//                     StopSpeedChangeCoroutine();
//                     speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
//                 }
//             }
//         }

//         // --------Reset Control--------
//         public void ResetVehicle()
//         {
//             // Reset flags
//             pausedForCollision = false;
//             pausedForTrafficLight = false;
//             isSpeedingUp = false;
//             obstacleDetected = false;
//             ResetV2XState();

//             // Stop all coroutines
//             StopAllCoroutines();
//             // Resume with smooth acceleration
//             ResumeAfterRandomDelay();
//         }

//         // Optional: Visual debugging
//         void OnDrawGizmosSelected()
//         {
//             if (radio != null)
//             {
//                 // Request distance
//                 Gizmos.color = Color.blue;
//                 Gizmos.DrawWireSphere(transform.position, requestDistance);
                
//                 // Yield slow distance
//                 Gizmos.color = Color.yellow;
//                 Gizmos.DrawWireSphere(transform.position, yieldSlowDistance);
                
//                 // Stop distance
//                 Gizmos.color = Color.red;
//                 Gizmos.DrawWireSphere(transform.position, stopDistance);
                
//                 if (currentRSU != null)
//                 {
//                     Gizmos.color = radio.GrantReceived ? Color.green : Color.yellow;
//                     Gizmos.DrawLine(transform.position, currentRSU.transform.position);
//                 }
//             }
//         }
//     }
// } 