using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallDropController : MonoBehaviour
{
    public Animator anim;
    
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        StartCoroutine(AniDelay());
    }

    IEnumerator AniDelay()
    {
        yield return new WaitForSeconds(60);
        anim.Play("Wall_Drop");
    }
    // Update is called once per frame
    void Update()
    {


    }
}
