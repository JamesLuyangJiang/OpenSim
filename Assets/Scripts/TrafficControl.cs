using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SWS;
using UnityEngine.AI;
using static V2X.RsuCmd;
using V2X;

public class TrafficControl : MonoBehaviour
{
    [Header("V2X Components")]
    [Tooltip("Reference to the V2X radio component")]
    public V2X.V2XRadio radio;

    [Header("V2X Intersection Settings")]
    [Tooltip("Layer mask for intersection triggers")]
    public LayerMask intersectionLayer = -1;
    [Tooltip("Distance from intersection to start requesting entry")]
    public float requestDistance = 15f;

    [Header("Stop Sign Settings")]
    [Tooltip("Distance from stop sign to start stopping")]
    public float stopDistance = 10f;
    [Tooltip("Duration for stop sign deceleration")]
    public float stopDecelDuration = 2.0f;

    private RadarCast radarCastScript;
    private splineMove moveRef;

    // V2X State
    private V2X.IntersectionRSU currentRSU;
    private bool hasRequestedEntry = false;
    // private bool hasCleared = false;
    private bool isWaitingForGrant = false;
    private bool isAtStopSign = false;

    private bool pausedForCollision = false;      // Flag for collision avoidance pause
    private bool pausedForTrafficLight = false;   // Flag for traffic light pause
    // private bool isResuming = false;              // Flag to prevent multiple "resume" coroutines
    private float randomDelay;                    // Random delay
    [HideInInspector]
    public float originalSpeed = 15.0f;          // The car's desired (max) speed
    // private bool isSlowingDown = false;           // Flag to track if the car is slowing down
    private bool isSpeedingUp = false;            // Flag to track if the car is speeding up

    // We keep references so we can stop only the *specific* coroutines
    private Coroutine speedChangeCoroutine = null;
    private Coroutine trafficLightCoroutine = null;
    private Coroutine resumeCoroutine = null;
    private Coroutine v2xStopCoroutine = null;

    // Global variables
    private float radarSlowDownRange = 20.0f;
    private float radarStopRange = 7.0f;
    private float speedChangeDuration = 2.0f;
    private bool obstacleDetected = false;
    private float stopDistanceOriginal = 15.0f;

    void Start()
    {
        // Initialize V2X components
        if (radio == null)
            radio = GetComponent<V2X.V2XRadio>();
        
        // Set unique vehicle ID if V2X radio exists
        if (radio != null)
        {
            radio.VehicleId = Random.Range(1000, 9999);
        }

        // Retrieve key components
        Transform radarTransform = transform.Find("Radars/LongRangeFR");
        if (radarTransform != null)
            radarCastScript = radarTransform.GetComponent<RadarCast>();

        if (radarCastScript == null)
            Debug.LogError("RadarCast script not found in Radars/LongRangeFR");

        moveRef = GetComponentInParent<splineMove>();
        if (moveRef == null)
            Debug.LogError("splineMove script not found on this object.");

        // Ensure the car starts at full speed (randomized)
        if (moveRef != null)
        {
            originalSpeed = Random.Range(15.0f, 20.0f);
            moveRef.ChangeSpeed(originalSpeed);
        }

        // Assign a random small delay for re-acceleration
        randomDelay = Random.Range(0.1f, 0.5f);
    }

    // --------Object Detection Speed Control--------
    void Update()
    {
        // Handle original obstacle detection (always active for player vehicle avoidance)
        HandleObstacleDetection();
    }

    /// <summary>
    /// Coroutine to stop at stop sign
    /// </summary>
    IEnumerator StopAtStopSign()
    {
        // Debug.Log($"[V2X] Vehicle {radio.VehicleId} starting StopAtStopSign at {transform.position}");
        // Slow down to stop
        float currentSpeed = moveRef.speed;
        yield return StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, stopDecelDuration));
        moveRef.Pause();
        
        // Send request to RSU
        if (currentRSU != null)
        {
            radio.SendToRsu(RequestEntry, currentRSU);
            // Debug.Log($"Vehicle {radio.VehicleId} stopped and requesting entry");
        }

        hasRequestedEntry = true;
        isWaitingForGrant = true;
        
        // Wait for grant
        while (!radio.GrantReceived)
        {
            yield return null;
        }

        // Resume when granted
        // Debug.Log($"Vehicle {radio.VehicleId} received grant, proceeding");
        moveRef.Resume();
        StopV2XStopCoroutine();
        v2xStopCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
    }



    /// <summary>
    /// Reset V2X state when exiting intersection
    /// </summary>
    void ResetV2XState()
    {
        isAtStopSign = false;
        isWaitingForGrant = false;
        if (radio != null)
        {
            radio.ResetGrantStatus();
        }
    }

    /// <summary>
    /// Handle original obstacle detection logic
    /// </summary>
    void HandleObstacleDetection()
    {
        if (radarCastScript == null || moveRef == null)
            return;

        // Get detected objects from radar
        List<RadarCast.DetectedObject> detectedObjects = radarCastScript.detectedObjects;

        // Check for obstacles too close
        float closestObstacleDistance = radarSlowDownRange;
        foreach (var detectedObject in detectedObjects)
        {
            if (detectedObject.distance < closestObstacleDistance)
            {
                closestObstacleDistance = detectedObject.distance;
                obstacleDetected = true;
            }
        }

        if (closestObstacleDistance == radarSlowDownRange)
            obstacleDetected = false;

        bool isPaused = pausedForTrafficLight || pausedForCollision || (moveRef.tween != null && moveRef.tween.IsPlaying() == false);

        // Don't apply obstacle detection if we're at a V2X stop sign (prioritize V2X stops)
        // But still allow basic collision avoidance to prevent vehicle overlap
        if (isAtStopSign)
        {
            // Only do basic collision avoidance when stopped
            if (closestObstacleDistance < radarStopRange + 0.5f)
            {
                moveRef.ChangeSpeed(0);
            }
            return;
        }

        if (!isPaused)
        {
            float stopBuffer = 0.4f;
            // If close to the stop range, since speed is very low, we can just stop instead of sliding
            if (closestObstacleDistance < radarStopRange + stopBuffer)
            {
                moveRef.ChangeSpeed(0);
            }
            else
            {
                // Smooth speed adjustment based on closest obstacle
                float minDistance = radarStopRange;
                float maxDistance = radarSlowDownRange;
                float t = Mathf.InverseLerp(minDistance, maxDistance, closestObstacleDistance);
                float speedFactor = Mathf.SmoothStep(0, 1, t);
                
                float maxSpeed = originalSpeed;
                
                float targetSpeed = Mathf.Lerp(0, maxSpeed, speedFactor);
                float minCrawlSpeed = 0f;
                targetSpeed = Mathf.Max(targetSpeed, minCrawlSpeed);
                float smoothness = 3.0f;
                float newSpeed = Mathf.Lerp(moveRef.speed, targetSpeed, Time.deltaTime * smoothness);
                moveRef.ChangeSpeed(newSpeed);
            }
        }
    }
    
    public void YieldCar()
    {
        // If the traffic light is RED and the car is not paused for collision or traffic light, and no obstacle is detected, then pause the car
        if (moveRef.tween != null && !pausedForTrafficLight && !obstacleDetected)
        {
            // Stop any ongoing speed changes
            StopSpeedChangeCoroutine();
            StopResumeCoroutine();

            float currentSpeed = moveRef.speed; // or originalSpeed
            float duration = 1.5f * stopDistance / currentSpeed;

            // Use this duration in your SmoothChangeSpeed call
            speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration));
        }
    }

    // --------Coroutine Control--------
    public void StopV2XStopCoroutine()
    {
        if (v2xStopCoroutine != null)
        {
            StopCoroutine(v2xStopCoroutine);
            v2xStopCoroutine = null;
        }
    }



    public void StopSpeedChangeCoroutine()
    {
        if (speedChangeCoroutine != null)
        {
            StopCoroutine(speedChangeCoroutine);
            speedChangeCoroutine = null;
        }
    }

    public void StopResumeCoroutine()
    {
        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }
    }

    public new void StopAllCoroutines()
    {
        StopSpeedChangeCoroutine();
        StopResumeCoroutine();
        StopV2XStopCoroutine();
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = null;
        }
    }

    // --------Helpers for smooth speed change--------
    public IEnumerator ResumeAfterRandomDelay()
    {
        yield return new WaitForSeconds(randomDelay);

        if (!pausedForTrafficLight)
        {
            moveRef.Resume();

            // Ensure we always start from 0
            moveRef.ChangeSpeed(0);
            yield return new WaitForSeconds(0.1f); // Short wait to ensure no conflicting coroutine

            if (!isSpeedingUp)
            {
                isSpeedingUp = true;
                // isSlowingDown = false;
                StopSpeedChangeCoroutine();
                speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
            }
            pausedForCollision = false;
        }
    }

    public IEnumerator SmoothChangeSpeed(float fromSpeed, float toSpeed, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float newSpeed = Mathf.Lerp(fromSpeed, toSpeed, elapsedTime / duration);
            moveRef.ChangeSpeed(newSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        moveRef.ChangeSpeed(toSpeed); // Ensure final speed is exact
        isSpeedingUp = false;
    }

    // Overloaded version with callback
    public IEnumerator SmoothChangeSpeed(float fromSpeed, float toSpeed, float duration, System.Action onComplete)
    {
        yield return StartCoroutine(SmoothChangeSpeed(fromSpeed, toSpeed, duration));
        onComplete?.Invoke();
    }

    // --------Traffic Light Stop Control--------
    // When vehicle passes a waypoint at traffic light, will trigger this function
    public void PauseCar(Object target)
    {
        // Check if target is null
        if (target == null)
        {
            Debug.LogWarning("PauseCar called with null target");
            return;
        }

        GameObject targetGameObject = target as GameObject;
        if (targetGameObject == null)
        {
            Debug.LogWarning("PauseCar target is not a GameObject");
            return;
        }

        Renderer lightRenderer = targetGameObject.GetComponent<Renderer>();
        if (lightRenderer == null)
        {
            Debug.LogWarning("PauseCar target GameObject has no Renderer component");
            return;
        }

        // If the traffic light is RED and the car is not paused for collision or traffic light, and no obstacle is detected, then pause the car
        if (moveRef.tween != null && lightRenderer.sharedMaterial != null && lightRenderer.sharedMaterial.name.EndsWith("OFF") && !pausedForTrafficLight && !obstacleDetected)
        {
            // Stop any ongoing speed changes
            StopSpeedChangeCoroutine();
            StopResumeCoroutine();

            pausedForTrafficLight = true;

            float currentSpeed = moveRef.speed; // or originalSpeed
            float duration = 2 * stopDistanceOriginal / currentSpeed;

            // Use this duration in your SmoothChangeSpeed call
            speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration, () => {
                moveRef.Pause();
            }));

            if (trafficLightCoroutine != null) StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = StartCoroutine(WaitForLightToTurnOn(lightRenderer, moveRef));
        }
    }

    private IEnumerator WaitForLightToTurnOn(Renderer lightRenderer, splineMove moveRef)
    {
        // Debug.Log("WaitForLightToTurnOn started - Waiting for light to turn GREEN");

        // Wait until the material changes from lightOff
        while (lightRenderer.sharedMaterial.name.EndsWith("OFF"))
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Debug.Log("Traffic light turned GREEN - Resuming car");
        pausedForTrafficLight = false;

        // If not also paused for collision, then resume
        if (!pausedForCollision)
        {
            yield return new WaitForSeconds(randomDelay);
            moveRef.Resume();

            // Smoothly accelerate from 0 to original speed
            if (!isSpeedingUp)
            {
                isSpeedingUp = true;
                // isSlowingDown = false;
                StopSpeedChangeCoroutine();
                speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
            }
        }
        else
        {
            // Debug.Log("Car is also paused for collision - not resuming yet");
        }
    }

    // --------V2X Zone Handling--------
    /// <summary>
    /// Called when vehicle enters a stop zone
    /// </summary>
    public void OnEnterStopZone(V2X.IntersectionRSU rsu)
    {
        // Debug.Log($"[V2X] Vehicle {radio.VehicleId} entered stop zone {rsu.name} at {transform.position}");
        // Prevent multiple zone entries
        if (hasRequestedEntry && currentRSU == rsu)
        {
            return; // Already in this zone
        }
        
        currentRSU = rsu;
        
        isAtStopSign = true;
        StopV2XStopCoroutine();
        v2xStopCoroutine = StartCoroutine(StopAtStopSign());
    }

    /// <summary>
    /// Called when vehicle exits a stop zone
    /// </summary>
    public void OnExitStopZone()
    {
        // Only clear if we've actually been granted and are moving through the intersection
        // The IntersectionRSU will handle clearing when vehicle exits the intersection zone
        // Don't clear here - let the intersection zone handle it
        hasRequestedEntry = false;
        isWaitingForGrant = false;
        isAtStopSign = false;
        // Debug.Log($"[V2X] Vehicle {radio.VehicleId} exited stop zone at {transform.position}");
    }

    // --------Reset Control--------
    public void ResetVehicle()
    {
        // Reset flags
        pausedForCollision = false;
        pausedForTrafficLight = false;
        // isSlowingDown = false;
        isSpeedingUp = false;
        obstacleDetected = false;
        ResetV2XState();

        // Stop all coroutines
        StopAllCoroutines();
        // Resume with smooth acceleration
        ResumeAfterRandomDelay();
    }

    // Optional: Visual debugging
    void OnDrawGizmosSelected()
    {
        if (radio != null)
        {
            // Request distance
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, requestDistance);
            

            
            // Stop distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
            
            if (currentRSU != null)
            {
                Gizmos.color = radio.GrantReceived ? Color.green : Color.yellow;
                Gizmos.DrawLine(transform.position, currentRSU.transform.position);
            }
        }
    }
}
