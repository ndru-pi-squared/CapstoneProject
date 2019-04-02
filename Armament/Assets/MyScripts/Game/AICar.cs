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
            //Debug.Log("AI car entered a collider");
            //Transform t = other.GetComponentInParent<Transform>();//get the transform to access the game object
            //GameObject p = t.gameObject;//TODO: change name to more descriptive
            //Debug.Log("What is the thing AI car hit: " + p.name);
            //if p.name is player | FPScamp | whatever it is for a player TODO: build out and check what the player name attribute is actually set to in debugger
            //p.getComponent<PlayerManager>().TakeDamage(10f, p.GetComponent<PlayerManager>());  //something like that
            
            //previously this was the only line not commented out. commented it out for the swap to a grenade:
            //this.gameObject.transform.Rotate(new Vector3(0, 90, 0));//only turn right
    }
}
