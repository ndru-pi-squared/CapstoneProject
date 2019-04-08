using Photon.Pun;
using System;
using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof (UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class AICharacterControl : MonoBehaviour
    {
        public UnityEngine.AI.NavMeshAgent agent { get; private set; }             // the navmesh agent required for the path finding
        public ThirdPersonCharacter character { get; private set; } // the character we are controlling
        public Transform target;                                    // target to aim for
        [Tooltip("Explosion time")]
        public float timer = 0.2f;//spawn a grenad every 5 seconds
        [Tooltip("Keeps track of the countdown")]
        public float countdown;
        bool hasSpawnedGrenade;
        GameObject spawnedGrenade;
        //public float Health = 250f; //trying health


        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();
            countdown = timer;
            agent.updateRotation = false;
	        agent.updatePosition = true;
            hasSpawnedGrenade = false;
        }


        private void Update()
        {
            if (target != null)
                agent.SetDestination(target.position);
            else { //not sure what this empty else is
                
            }

            if (countdown <= 0)
            {
                hasSpawnedGrenade = false;//ready to spawn new grenade   
                countdown = timer;
            }

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                countdown -= Time.deltaTime;
                character.Move(agent.desiredVelocity, false, false);
            }
            else if (agent.remainingDistance < (agent.stoppingDistance + 10f))//ai is within throwing range
            {
                countdown -= Time.deltaTime;
                if (hasSpawnedGrenade == false)
                {
                    spawnedGrenade = PhotonNetwork.Instantiate("FragGrenade", gameObject.transform.position, gameObject.transform.rotation);
                    //spawnedGrenade.GetComponent<FragGrenade>().playerWhoOwnsThisGrenade = this;
                    hasSpawnedGrenade = true;
                }
            }

            else//ai is within stopping range
            {
                countdown -= Time.deltaTime;//continue counting down
                character.Move(Vector3.zero, false, false);
            }
            if (hasSpawnedGrenade == false)
            {//periodically spawn a grenade based on timer/lock
                spawnedGrenade = PhotonNetwork.Instantiate("FragGrenade", gameObject.transform.position, gameObject.transform.rotation);
                //spawnedGrenade.GetComponent<FragGrenade>().playerWhoOwnsThisGrenade = this;
                hasSpawnedGrenade = true;
            }

        }


        public void SetTarget(Transform target)
        {
            this.target = target;
        }
    }
}
