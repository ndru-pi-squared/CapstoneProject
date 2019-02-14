using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyMenuPanel : MonoBehaviour
{

    public Rigidbody rb;
    bool moveRight = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ButtonPress()
    {
        Debug.Log("Special button was pressed!");
        if (moveRight)
        {
            moveCubeRight();
        }
        else
        {
            moveCubeLeft();
        }
        moveRight = !moveRight;
    }

    void moveCubeRight()
    {
        Debug.Log("moveCubeRight() was called");
        rb.AddRelativeForce(10, 0, 0);
    }

    void moveCubeLeft()
    {
        Debug.Log("moveCubeLeft() was called");
        rb.AddRelativeForce(-10, 0, 0);
    }
}
