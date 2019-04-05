using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragGrenade : MonoBehaviour, IThrowable
{

    float timer = 3.0f;
    float countdown;
    float explosionRadius = 3.0f;
    float force = 500.0f;
    bool hasExploded;
    [SerializeField] GameObject explosionParticle;


   /* public GameObject explosionPrefab;
    public float radius = 5.0f;
    public float power = 10.0f;
    public float explosiveLift = 1.0f;
    public float explosiveDelay = 1.0f;
    private IEnumerator coroutine;*/ 
    // Start is called before the first frame update
    void Start()
    {
        hasExploded = false;
        countdown = timer;
        //coroutine = TimedExplosion(explosiveDelay);
        //StartCoroutine(coroutine);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("test");
        countdown -= Time.deltaTime;
        if (countdown <= 0 && !hasExploded)
        {
            Explode();
        }
    }

    void Explode()
    {
        GameObject spawnedParticle = Instantiate(explosionParticle, transform.position, transform.rotation);
       
        //Destroy(spawnedParticle,1);
        //Debug.Log("Explosion");
        hasExploded = true;
        PhotonNetwork.Destroy(this.gameObject);
        //StartCoroutine("DestroyGrenade", this.gameObject);
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        Rigidbody rb;
        foreach (Collider nearbyObject in colliders)
        {
             rb = nearbyObject.GetComponent<Rigidbody>();
            if(rb != null)//if rigidbody found
            {
                rb.AddExplosionForce(force, transform.position,explosionRadius);//also can use addforce but we'd have to create other variables and calculate them. much simpler to use addexplosion force
            }

        }
    }

    public void Throw()//called from playermanager?
    {

    }

   /* private IEnumerator TimedExplosion(float waitTime)
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
        }*/
        
        /*Vector3 position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Quaternion rotation = new Quaternion(transform.rotation.w, transform.rotation.x, transform.rotation.y, transform.rotation.z);
        GameObject obj = Instantiate(explosionPrefab, position, rotation) as GameObject;*/
        
    //}

    private void OnTriggerEnter(Collider other)
    {

    }

}
