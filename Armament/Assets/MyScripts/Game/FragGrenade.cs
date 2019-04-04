using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragGrenade : MonoBehaviour, IThrowable
{
    public GameObject explosionPrefab;
    public float radius = 5.0f;
    public float power = 10.0f;
    public float explosiveLift = 1.0f;
    public float explosiveDelay = 1.0f;
    private IEnumerator coroutine;
    // Start is called before the first frame update
    void Start()
    {
        coroutine = TimedExplosion(explosiveDelay);
        StartCoroutine(coroutine);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Throw()
    {

    }

    private IEnumerator TimedExplosion(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);//might break out? research it
        Debug.Log("Testing");
        Vector3 grenadeOrigin = transform.position;
        Collider[] colliders = Physics.OverlapSphere(grenadeOrigin, radius);//not sure if this will work

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Rigidbody>())
            {
                hit.GetComponent<Rigidbody>().AddExplosionForce(power, grenadeOrigin, radius, explosiveLift);
                //Destroy(gameObject);
            }
        }
        
        /*Vector3 position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Quaternion rotation = new Quaternion(transform.rotation.w, transform.rotation.x, transform.rotation.y, transform.rotation.z);
        GameObject obj = Instantiate(explosionPrefab, position, rotation) as GameObject;*/
        
    }

    private void OnTriggerEnter(Collider other)
    {

    }

}
