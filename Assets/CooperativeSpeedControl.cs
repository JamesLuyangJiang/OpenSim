using System;
using System.Collections;
using UnityEngine;
//using SWS.Scripts.Movement;
using SWS;
using System.Collections.Generic;
using System.IO;

//[RequireComponent(typeof(SplineMove))]
public class CooperativeSpeedControl : MonoBehaviour
{

    private float egoDistanceToEnd;
    private float targetDistanceToEnd;
    private float computed_ref_a, ref_a, v_ref;
    private float egov, targetv;
    private float acc_lim = 3.0f;
    private float dec_lim = -3.0f;
    private float r_ref;
    private float t_headway, d_headway;
    private int frameCounter = 0;
    private bool record = true;
    private splineMove egoSplineMove, targetSplineMove;
    private Rigidbody targetRb;
    private Transform egoTransform;


    private List<float> times = new List<float>();
    private List<float> egoDistanceToEnds = new List<float>();
    private List<float> targetDistanceToEnds = new List<float>();
    private List<float> d_headways = new List<float>();
    private List<float> t_headways = new List<float>();
    private List<float> v_refs = new List<float>();
    private List<float> v_targets = new List<float>();
    private List<float> computed_a_refs = new List<float>();
    private List<float> a_refs = new List<float>();


    public float kv = 0.35f;
    public float kd = 0.05f;
    public float realease_time;
    public GameObject targetObject;
    public bool leadvehicle = false;
    public float startspeed;
    public float timegap_ref = 2.5f;
    public Transform startpoint,mergepoint;


    private float targetTotalDistance;
    public float egoTotalDistance;


    private void Awake()
    {
        // Obtain the necessary components of the ego car
        egoSplineMove = GetComponent<SWS.splineMove>();
        egoTransform = GetComponent<Transform>();

        // Obtain the distance cauculator component of the target car
        if (targetObject!= null)
        {
            targetSplineMove = targetObject.GetComponent<SWS.splineMove>();
            targetRb = targetObject.GetComponent<Rigidbody>();
            targetTotalDistance = targetObject.GetComponent<CooperativeSpeedControl>().egoTotalDistance;
            egoSplineMove.ChangeSpeed(startspeed);
        }

        StartCoroutine(DelayedOperation());

    }
    IEnumerator DelayedOperation()
    {
        yield return new WaitForSeconds(realease_time); // wait reaslease time(seconds)
        egoTransform.position = startpoint.position;
        egoSplineMove.StartMove();
    }

    private void FixedUpdate()  //compute the speed then distance(comulative version)
    {
        if (leadvehicle)
        {
            return; // if the object is the leading vehicle, no need to use operative driving control 
        }

        frameCounter++;
        if (frameCounter >= (int)realease_time*60)
        {
            if (targetSplineMove != null)
            {
                // get speed of targetvehicle
                targetv = targetSplineMove.speed;
                // Debug.Log("speed：" + targetv);
            }
            else
            {
                targetv = targetRb.velocity.magnitude;
                Debug.Log("player mode, speed" + targetv);
            }

            egov = egoSplineMove.speed;

            //compute distance to end using speed
            egoDistanceToEnd = egoTotalDistance - egov * Time.fixedDeltaTime;
            targetDistanceToEnd = targetTotalDistance - targetv * Time.fixedDeltaTime;
            egoTotalDistance = egoDistanceToEnd;
            targetTotalDistance = targetDistanceToEnd;
            d_headway = egoTotalDistance - targetTotalDistance;
            t_headway = d_headway / egov;
            //compute the reference acceleration for the ego vehicle
            r_ref = egov * timegap_ref;
            computed_ref_a = kv * (targetv - egov) + kd * (d_headway - r_ref);
            /// Bound the acceleration with limit interval
            if (ref_a >= 0)
            {
                ref_a = Math.Min(computed_ref_a, acc_lim);
            }
            else
            {
                ref_a = Math.Max(computed_ref_a, dec_lim);
            }

            v_ref = egov + ref_a * Time.fixedDeltaTime;
            v_ref = Math.Max(v_ref, 0f);
            egoSplineMove.ChangeSpeed(v_ref);

            if (record)
            {
                times.Add(Time.time);
                egoDistanceToEnds.Add(egoDistanceToEnd);
                targetDistanceToEnds.Add(targetDistanceToEnd);
                d_headways.Add(d_headway);
                t_headways.Add(t_headway);
                v_refs.Add(v_ref);
                v_targets.Add(targetv);
                computed_a_refs.Add(computed_ref_a);
                a_refs.Add(ref_a);
            }

            /// If only record the data before the merge point(still need ajustment)
            /*
            if (IsAtEndPoint(egoTransform.position))
            {
                Debug.Log("change status");
                record = false;
            }*/

        }
        
    }

    private void OnApplicationQuit()
    {
        // SaveDataToCSV();
    }

    // Check if the endpoint has been reached
    private bool IsAtEndPoint(Vector3 currentPosition)
    {
        if (mergepoint != null)
        {
            float distanceToEnd = Vector3.Distance(currentPosition, mergepoint.position);
            return distanceToEnd < 0.1f; // Consider it reached when the distance is less than a certain threshold
        }
        return false;
    }

    // Combine List<float> arrays and return
    public List<List<float>> GetCombinedData()
    {
        List<List<float>> combinedData = new List<List<float>>();

        // make sure all the List<float> have the same length
        int dataLength = times.Count; // Use any one count of the List<float> 

        for (int i = 0; i < dataLength; i++)
        {
            List<float> rowData = new List<float>
            {
                times[i],
                egoDistanceToEnds[i],
                targetDistanceToEnds[i],
                d_headways[i],
                t_headways[i],
                v_refs[i],
                v_targets[i],
                computed_a_refs[i],
                a_refs[i]
            };
            
            combinedData.Add(rowData);
            rowData = null;
        }

        return combinedData;
    }



    public void SaveDataToCSV()
        {
            // 
            //string filePath = Application.dataPath +  "/data" + order +".csv"; // 
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string componentName = gameObject.name;
            string filePath = Application.dataPath + "/data/" +  timestamp + componentName + ".csv";

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("time,egoDistanceToEnd, targetDistanceToEnd, d_headway, t_headway, v_ref, targetv, computed_ref_a, ref_a");

                for (int i = 0; i < a_refs.Count; i++)
                {

                    writer.WriteLine(
                        times[i].ToString() + "," +
                        egoDistanceToEnds[i].ToString() + "," +
                        targetDistanceToEnds[i].ToString() + "," +
                        d_headways[i].ToString() + "," +
                        t_headways[i].ToString() + "," +
                        v_refs[i].ToString() + "," +
                        v_targets[i].ToString() + "," +
                        computed_a_refs[i].ToString() + "," +
                        a_refs[i].ToString()
                    );

                }
            }

            Debug.Log("path：" + filePath);
        }

}