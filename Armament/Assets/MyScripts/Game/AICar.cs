using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICar : MonoBehaviour
{
    //Collider objectCollider;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
       // objectCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //this.transform.position = transform.position.forward;// new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z+0.3f); //just move forward
        rb.velocity = transform.forward * 10;
    }

    private void OnTriggerEnter(Collider other)
    {
            Debug.Log("AI car entered a collider");
            this.gameObject.transform.Rotate(new Vector3(0, 90, 0));
    }
}
