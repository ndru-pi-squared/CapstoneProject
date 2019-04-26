using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{

    [RequireComponent(typeof(AudioSource))]
    public class FragGrenade : MonoBehaviourPun, IThrowable
    {
        [Tooltip("Explosion time")]
        public float timer = 3.0f;
        [Tooltip("Keeps track of the countdown")]
        public float countdown;
        public float explosionRadius = 500.0f;
        public float explosionForce = 2000f;
        public float baseDamage = 100f;
        private float distanceFromGrenade;
        private float damageCaused;
        public bool thrown;
        public bool hasExploded;
        public bool grenadeWasPickedUp;
        [SerializeField] GameObject explosionParticle;
        [Tooltip("The player who is holding the grenade. **This implementation might need revision**")]
        public MonoBehaviourPun playerWhoOwnsThisGrenade;


        void Start()
        {
            grenadeWasPickedUp = false;
            hasExploded = false;
            countdown = timer;

            //coroutine = TimedExplosion(explosiveDelay);
            //StartCoroutine(coroutine);
        }

        void Awake()
        {
            thrown = false;
            hasExploded = false;
            countdown = timer;
        }

        // Update is called once per frame
        void Update()
        {
            if (grenadeWasPickedUp == true)
            {
                photonView.RPC("DestroyRPC", RpcTarget.All);
            }
            if (playerWhoOwnsThisGrenade != null && thrown == false)
            {
                GetComponent<BoxCollider>().isTrigger = false;
                GetComponent<Rigidbody>().isKinematic = false;
                Debug.Log("Calling Throw()");
                Throw();
            }

            if (thrown == true)
            {
                //Debug.Log("Thrown set to true");
                countdown -= Time.deltaTime;
                if (countdown <= 0 && !hasExploded)
                {
                    photonView.RPC("Explode", RpcTarget.All);
                }
            }

        }

        [PunRPC]
        void DestroyRPC()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(this.gameObject);
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { photonView.ViewID.ToString(), null } });
            }
                
        }

        [PunRPC]
        void Explode()//this might need to be an rpc function
        {
            //GameObject spawnedParticle = Instantiate(explosionParticle, transform.position, transform.rotation);
            GameObject spawnedParticle = PhotonNetwork.Instantiate("Explosion", gameObject.transform.position, gameObject.transform.rotation);
            //Debug.Log("Explosion");
            hasExploded = true;
            if(PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(this.gameObject);
            //StartCoroutine("DestroyGrenade", this.gameObject);

            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            Rigidbody rb;
            foreach (Collider nearbyObject in colliders)
            {
                rb = nearbyObject.attachedRigidbody;//GetComponent<Rigidbody>();
                if (rb != null)//if rigidbody found
                {
                    //apply force
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);//also can use addforce but we'd have to create other variables and calculate them. much simpler to use addexplosion force
                    //apply damage
                    ITarget target = nearbyObject.gameObject.GetComponent<ITarget>();
                    float distX = transform.position.x - nearbyObject.transform.position.x;
                    //Debug.Log(distX);
                    float distY = transform.position.y - nearbyObject.transform.position.y;
                    //Debug.Log(distY);
                    float distZ = transform.position.z - nearbyObject.transform.position.z;
                    //Debug.Log(distZ);
                    distanceFromGrenade = (float)Math.Sqrt((Math.Pow(distX, 2)) + (Math.Pow(distY, 2)) + (Math.Pow(distZ, 2)));
                    Debug.Log("Distance from grenade " + distanceFromGrenade);
                    if (distanceFromGrenade == 0 || distanceFromGrenade == float.NaN) //0 or infinity (?) not 100% sure about the NaN value
                        damageCaused = baseDamage;//no mitigation if youre standing on top of a grenade
                    else
                    {
                        damageCaused = baseDamage - (distanceFromGrenade * 5);//reduce dmg based on distance from grenade 
                    }
                    if (target != null && nearbyObject.gameObject.GetComponent<PlayerManager>() != null) //if it's a playermanager
                    {
                        target.TakeDamage(damageCaused, (PlayerManager)playerWhoOwnsThisGrenade);//only players can take grenade damage. we could update to damage the wall too
                    }
                    else if (target != null)
                    {
                        target.TakeDamage(damageCaused);//damage wall?
                    }
                    //if collider.getgameobject has ITarget
                    //player.takedamage
                }
            }
            Debug.Log(spawnedParticle.name);
            //Destroy(spawnedParticle.gameObject);//despawns too early--handled in the explosion itself
        }

        public void Throw()//called from playermanager. pulling into here makes it more modular in PlayerManager since theres a lot of cod ethere. Similar to shoot. 
        {
            //GetComponent<BoxCollider>().isTrigger = false;
            //add up and forward forces to lob it
            thrown = true;
            this.gameObject.GetComponent<Rigidbody>().AddForce(transform.forward * 1500.0f);
            this.gameObject.GetComponent<Rigidbody>().AddForce(0, 400, 0);
        }

        public bool IsReadyToThrow()
        {
            return false; //how often can he throw?
        }


    }
}