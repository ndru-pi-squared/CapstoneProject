using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform[] potentialTargets;
    public Transform currentTarget;
    public float distance;
    public float lookAtDistance = 25.0f;
    public float attackRange = 15.0f;
    public float moveSpeed = 5.0f;
    public float damping = 6.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        distance = Vector3.Distance(currentTarget.position, transform.position);

        if (distance < lookAtDistance)
        {
            GetComponent<Renderer>().material.color = Color.yellow;
            LookAt();
        }

        if (distance > lookAtDistance)
        {
            GetComponent<Renderer>().material.color = Color.green;
        }

        if (distance < attackRange)
        {
            GetComponent<Renderer>().material.color = Color.red;
            Attack();
        }
    }

    void LookAt()
    {
        var rotation = Quaternion.LookRotation(currentTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping);
    }

    void Attack()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    public void AssignTarget(Transform t) {
        currentTarget = t;
    }

    public void AddTarget(Transform t) {
        if (potentialTargets.Length == 0) {
            potentialTargets[0] = t;
        }
        else {
            int i = potentialTargets.Length - 1;
            potentialTargets[i] = t;
        } 
    }
}