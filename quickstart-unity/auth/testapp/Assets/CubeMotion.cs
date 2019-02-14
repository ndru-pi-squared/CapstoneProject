using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeMotion : MonoBehaviour
{
    public Rigidbody rb; 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.AddTorque(0, 1 * Time.deltaTime, 1 * Time.deltaTime);
    }

}
