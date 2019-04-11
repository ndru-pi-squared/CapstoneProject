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
        #region Public Fields

        public NavMeshAgent Agent { get; private set; }             // the navmesh agent required for the path finding
        public ThirdPersonCharacter Character { get; private set; } // the character we are controlling
        public Transform target;                                    // target to navigate to

        #endregion Public Fields

        #region Private Fields

        private const bool DEBUG = true;
        private const bool DEBUG_EnemyIsInCrosshairs = true;

        PlayerManager pm; // keeps a reference of the player manager attached to the player GM
        Camera fpsCam;

        #endregion Private Fields

        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            Agent = GetComponentInChildren<NavMeshAgent>();
            Character = GetComponent<ThirdPersonCharacter>();
            pm = GetComponent<PlayerManager>();
            fpsCam = GetComponentInChildren<Camera>();

            Agent.updateRotation = false; // Alex's note to self: look into why this is set to false
	        Agent.updatePosition = true;
        }
        
        private void Update()
        {
            // If enemy player is in AI player's crosshairs...
            if (EnemyIsInCrosshairs(out PlayerManager enemy))
            {
                // If we found an enemy in our cross hairs...
                SetTarget(enemy.transform);
                // Shoot the gun (if we have a gun to shoot)
                ShootGun();
            }

            // If we have a target for the AI
            if (target != null)
            {
                // Set the destination based on the target's current position
                Agent.SetDestination(target.position);
            }

            // If AI player is outside of stopping distance from target
            if (Agent.remainingDistance > Agent.stoppingDistance)
            {
                Character.Move(Agent.desiredVelocity, false, false);
            }
            // If AI player is within stopping distance of target
            else
            {
                // Stop moving
                // *** Alex: Not sure if this is needed.
                Character.Move(Vector3.zero, false, false);
            }
        }

        /// <summary>
        /// Raycasts from player's camera to check if enemy (player on a different team) is in this player's crosshairs.
        /// </summary>
        /// <param name="enemy">Returns true if we have an enemy in our crosshairs. Also returns that (non-null) enemy.
        /// Returns false if we do not have an enemy in our crosshairs. Also returns enemy = null.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Shoots active gun if player has an active gun to shoot.
        /// </summary>
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
