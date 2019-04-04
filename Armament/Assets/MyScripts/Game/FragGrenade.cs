using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragGrenade : MonoBehaviour, IThrowable
{
    public GameObject explosionPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
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


        yield return new WaitForSeconds(waitTime);
        Vector3 position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        Quaternion rotation = new Quaternion(transform.rotation.w, transform.rotation.x, transform.rotation.y, transform.rotation.z);
        GameObject obj = Instantiate(explosionPrefab, position, rotation) as GameObject;
    }
}
