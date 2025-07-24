using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraManager : MonoBehaviour
{
    public GameObject target;
    public Vector3 offset;

    private float r = -0.3f;
    private float f = 0.64f;
    private float g = 0.04f;
    // Start is called before the first frame update
    void Start()
    {
        offset = new Vector3(r,f,g);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = target.transform.position + target.transform.TransformDirection(offset);
        transform.rotation = target.transform.rotation;
    }
}
