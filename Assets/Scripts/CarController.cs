using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(InputManager))]

public class CarController : MonoBehaviour
{
    private InputManager input;
    public List<WheelCollider> wheelsThrottle;
    public List<WheelCollider> wheelSteer;
    public List<GameObject> meshes;

    public float enginePower = 20000f;
    public float maxAngle = 35;
    public float maxAngleHS = 30;
    public float throttleSmoothing = 0.5f;  // Adjust this value for smoother throttle response
    public float engineSmoothing = 0.5f;
    private float currentThrottle = 0f;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        input = GetComponent<InputManager>();

        // Initialize Rigidbody if needed
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Lower the center of mass for stability
            rb.centerOfMass = new Vector3(0, -0.5f, 0);
            // Optionally, you can tweak these values to simulate car drag and friction
            rb.drag = 0.1f;
            rb.angularDrag = 1.0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Smooth throttle input over time for gradual acceleration and deceleration
        if (Input.GetKey(KeyCode.LeftShift))
        {
            enginePower = Mathf.Lerp(enginePower, enginePower * 1.5f, engineSmoothing * Time.deltaTime);
            Debug.Log("pressed Shift");
        }
        currentThrottle = Mathf.Lerp(currentThrottle, input.throttle, throttleSmoothing * Time.deltaTime);

        // Interpolate steering based on the throttle (less steering at higher speeds)
        float currentMaxAngle = Mathf.Lerp(maxAngle, maxAngleHS, Mathf.Abs(currentThrottle));

        // Apply motor torque to wheels for throttle
        foreach (var wheel in wheelsThrottle)
        {
            wheel.motorTorque = currentThrottle * enginePower * Time.deltaTime;
        }

        // Apply steering based on the current steering angle and speed
        foreach (var wheel in wheelSteer)
        {
            wheel.steerAngle = input.steer * currentMaxAngle;
            wheel.transform.rotation = Quaternion.Euler(0, wheel.steerAngle, 0);
        }

        //foreach ( var mesh in meshes)
        //{
            //mesh.transform.Rotate(0f, 0f, 0f);
        //}
    }
}
