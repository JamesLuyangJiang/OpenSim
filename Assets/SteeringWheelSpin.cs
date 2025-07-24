﻿/*============================================================================*/
/*  SteeringWheelSpin.cs                                    Module for Unity  */
/*                                                                            */
/*  Script to enable the steering wheels to spin based on driver's input.     */
/*																			  */
/*  Version of 06-07-2020                                                     */
/*  Copyright by Ziran Wang                           ryanwang11@hotmail.com  */
/*	Toyota Motor North America, InfoTech Labs								  */
/*============================================================================*/

using EVP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteeringWheelSpin : MonoBehaviour
{
    public Transform steeringWheel;
    public Vector3 rotationAxisEulerAngles = new Vector3(25.0f, 0f, 0f);
    private Vector3 initialSteeringPosition;
    private Vector3 steeringRotationAxis;
    private float maxRotationAngle = 2250.0f;

    public VehicleStandardInput vehicleInput;

    private void Awake()
    {
        if (steeringWheel != null)
        {
            initialSteeringPosition = new Vector3(
                steeringWheel.localRotation.eulerAngles.x,
                steeringWheel.localRotation.eulerAngles.y,
                steeringWheel.localRotation.eulerAngles.z
            );
            steeringRotationAxis = Quaternion.Euler(rotationAxisEulerAngles) * Vector3.up;
            steeringRotationAxis = Quaternion.Euler(initialSteeringPosition) * steeringRotationAxis;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float h = GetComponent<VehicleStandardInput>().steerInput;
        moveSteeringWheel(h);
    }

    void moveSteeringWheel(float steeringInput)
    {
        // SteeringInput between -1 and 1
        if (steeringWheel != null)
        {
            steeringWheel.localRotation = Quaternion.AngleAxis(maxRotationAngle * steeringInput, steeringRotationAxis) * Quaternion.Euler(initialSteeringPosition);
        }
    }
}
