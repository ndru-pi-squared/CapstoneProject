using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame {

    public class FragGrenade : MonoBehaviour, IThrowable
    {

        float timer = 3.0f;
        float countdown;
        float explosionRadius = 3.0f;
        float force = 500.0f;
        bool hasExploded;
        [SerializeField] GameObject explosionParticle;
        [Tooltip("The player who is holding the gun. **This implementation might need revision**")]
        public MonoBehaviourPun playerWhoOwnsThisGrenade;


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
                if (rb != null)//if rigidbody found
                {
                    rb.AddExplosionForce(force, transform.position, explosionRadius);//also can use addforce but we'd have to create other variables and calculate them. much simpler to use addexplosion force
                    ITarget target = nearbyObject.gameObject.GetComponent<ITarget>();
                    if ( target != null && nearbyObject.gameObject.GetComponent<PlayerManager>() != null) //if it's a playermanager
                    {
                        target.TakeDamage(50, (PlayerManager) playerWhoOwnsThisGrenade);//only players can take grenade damage. we could update to damage the wall too
                    }
                    else if (target != null)
                    {
                        target.TakeDamage(50);//damage wall?
                    }
                //if collider.getgameobject has ITarget
                //player.takedamage
                }

            }
        }

        public void Throw()//called from playermanager. pulling into here makes it more modular in PlayerManager since theres a lot of cod ethere. Similar to shoot. 
        {

        }
    }
}