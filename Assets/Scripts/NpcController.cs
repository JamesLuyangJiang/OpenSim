using UnityEngine;
using SWS;

public class NpcController : MonoBehaviour
{
    private bool started = false;
    public void StartAllCars()
    {
        // Call StartMove on each one or reset them to their initial positions
        foreach (Transform child in transform)
        {
            splineMove splineMove = child.GetComponent<splineMove>();
            TrafficControl trafficControl = child.GetComponent<TrafficControl>();

            if (started)
            {
                // TODO: we might need to reset current waypoint
                splineMove.GoToWaypoint(splineMove.initPoint);

                trafficControl.ResetVehicle();
                continue;
            }
            splineMove.StartMove();
        }

        started = true;
    }

    public void ResetAllCars()
    {
        gameObject.SetActive(false);
        // // Find all SplineMove components in child objects
        // splineMove[] splineMoves = GetComponentsInChildren<splineMove>();

        // // Call Reset on each one
        // foreach (var splineMove in splineMoves)
        // {
        //     // splineMove.ResetToStart();
        //     // splineMove.CreateTween();
        //     // if (splineMove.startPoint > 0)
        //     // {
        //     //     splineMove.currentPoint = splineMove.startPoint;
        //     //     splineMove.GoToWaypoint(splineMove.startPoint);
        //     // }
        //     // splineMove.ChangeSpeed(0);
        //     // splineMove.GoToWaypoint(splineMove.startPoint);
        //     // splineMove.currentPoint = splineMove.startPoint;
        //     // splineMove.Pause();
        //     // splineMove.Stop();

        //     // TrafficControl trafficControl = splineMove.GetComponentInParent<TrafficControl>();
        //     // if (trafficControl != null)
        //     // {
        //     //     trafficControl.StopAllCoroutines();
        //     // }

        //     // splineMove.Pause();
        // }
    }

    public void ResumeAllCars()
    {
        // Find all SplineMove components in child objects
        splineMove[] splineMoves = GetComponentsInChildren<splineMove>();

        // Call Resume on each one
        foreach (var splineMove in splineMoves)
        {
            splineMove.CreateTween();
            splineMove.StartMove();

            // TrafficControl trafficControl = splineMove.GetComponentInParent<TrafficControl>();
            // if (trafficControl != null)
            // {
            //     trafficControl.StopSpeedChangeCoroutine();
            //     trafficControl.StartCoroutine(trafficControl.SmoothChangeSpeed(0, trafficControl.originalSpeed, 1.0f));
            // }
        }
    }
}
