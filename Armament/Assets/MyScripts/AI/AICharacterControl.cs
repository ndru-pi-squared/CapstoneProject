using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    [RequireComponent(typeof (NavMeshAgent))]
    [RequireComponent(typeof(ThirdPersonCharacter))]
    [RequireComponent(typeof(PlayerManager))]
    [RequireComponent(typeof(Camera))]
    public class AICharacterControl : MonoBehaviour
    {
        public NavMeshAgent Agent { get; private set; }             // the navmesh agent required for the path finding
        public ThirdPersonCharacter Character { get; private set; } // the character we are controlling
        public Transform target;                                    // target to aim for
        [Tooltip("Explosion time")]
        public float timer = 0.2f;//spawn a grenad every 5 seconds
        [Tooltip("Keeps track of the countdown")]
        public float countdown;
        bool hasSpawnedGrenade;
        GameObject spawnedGrenade;
        //public float Health = 250f; //trying health

        PlayerManager pm; // keeps a reference of the player manager attached to the player GM
        Camera fpsCam;
        private bool DEBUG = true;
        private bool DEBUG_EnemyIsInCrosshairs = true;

        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            Agent = GetComponentInChildren<NavMeshAgent>();
            Character = GetComponent<ThirdPersonCharacter>();
            pm = GetComponent<PlayerManager>();
            fpsCam = GetComponentInChildren<Camera>();
            countdown = timer;
            Agent.updateRotation = false;
	        Agent.updatePosition = true;
            hasSpawnedGrenade = false; 
        }


        private void Update()
        {
            if (target != null)
                Agent.SetDestination(target.position);
            else { //not sure what this empty else is
                
            }

            /*if (countdown <= 0)
            {
                hasSpawnedGrenade = false;//ready to spawn new grenade   
                countdown = timer;
            }*/

            if (Agent.remainingDistance > Agent.stoppingDistance)
            {
                //countdown -= Time.deltaTime;
                Character.Move(Agent.desiredVelocity, false, false);
            }
            /*else if (agent.remainingDistance < (agent.stoppingDistance + 10f))//ai is within throwing range
            {
                countdown -= Time.deltaTime;
                if (hasSpawnedGrenade == false)
                {
                    spawnedGrenade = PhotonNetwork.Instantiate("FragGrenade", gameObject.transform.position, gameObject.transform.rotation);
                    //spawnedGrenade.GetComponent<FragGrenade>().playerWhoOwnsThisGrenade = this;
                    hasSpawnedGrenade = true;
                }
            }*/
            else//ai is within stopping range
            {
                //countdown -= Time.deltaTime;//continue counting down
                Character.Move(Vector3.zero, false, false);
            }

            /*if (hasSpawnedGrenade == false)
            {//periodically spawn a grenade based on timer/lock
                spawnedGrenade = PhotonNetwork.Instantiate("FragGrenade", gameObject.transform.position, gameObject.transform.rotation);
                //spawnedGrenade.GetComponent<FragGrenade>().playerWhoOwnsThisGrenade = this;
                hasSpawnedGrenade = true;
            }*/

            if (EnemyIsInCrosshairs(out PlayerManager enemy))
            {
                // If we found an enemy in our cross hairs...
                if (enemy!= null)
                {
                    SetTarget(enemy.transform);
                }

                ShootGun();
            }
        }

        private bool EnemyIsInCrosshairs(out PlayerManager enemy)
        {
            
            // Create a raycast from fps camera position in the direction it is facing (limit raycast to 'range' distance away)
            // Get back the 'hit' value (what got hit)
            // If ray cast hit something...
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, 200f))
            {
                if (DEBUG && DEBUG_EnemyIsInCrosshairs) Debug.LogFormat("AICharacterControl: EnemyIsInCrosshairs() RAYCAST HIT");

                PlayerManager possibleEnemy = hit.transform.GetComponent<PlayerManager>();

                // If we have a possible enemy in our cross hairs...
                if (possibleEnemy != null)
                {
                    if (DEBUG && DEBUG_EnemyIsInCrosshairs) Debug.LogFormat("AICharacterControl: EnemyIsInCrosshairs() POSSIBLE ENEMY FOUND");
                    // If player we are aiming at is on a different team...
                    if (!pm.GetTeam().Equals(possibleEnemy.GetTeam()))
                    {
                        // We found an enemy in our crosshairs
                        enemy = possibleEnemy;
                        return true;
                    }
                }

            }
            // We did not find enemy in our crosshairs
            enemy = null;
            return false;
        }

        private void ShootGun()
        {
            // If player has an active gun...
            if (pm.ActiveGun != null)
            {

                // Shoot the gun
                pm.CallShootRPC();
            }
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }
    }
}
