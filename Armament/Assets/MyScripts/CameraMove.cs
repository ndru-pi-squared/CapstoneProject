using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public Transform target;
    public float distance;
    private float moveSpeed = 500.0f;
    private bool timeToMoveBack = false;
    private bool timeToMoveForward = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        distance = Vector3.Distance(target.position, transform.position);

        if (timeToMoveBack && distance > 5)
        {
            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
        }

        else if (timeToMoveForward && distance < 940)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    public void MoveBack()
    {
        timeToMoveForward = false;
        timeToMoveBack = true;
    }

    public void MoveForward() {
        timeToMoveBack = false;
        timeToMoveForward = true;
    }

    public void AssignTarget(Transform t)
    {
        target = t;
    }
}
