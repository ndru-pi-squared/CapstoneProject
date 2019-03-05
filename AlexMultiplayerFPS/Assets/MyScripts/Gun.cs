using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    [RequireComponent(typeof(AudioSource))]
    public class Gun : MonoBehaviour
    {
        public float damage = 10f; // How much damage a gun's bullet can impart (on a target)
        public float range = 100f; // How far a bullet can go
        public float fireRate = 1f; // How fast (per second) a bullet can be fired
        public float impactForce = 30f; // Force imparted on a bullet hit
        
        public AudioClip gunshotSound;
        public Camera fpsCam;
        public ParticleSystem muzzleFlash;
        public GameObject impactEffect;

        // Setting this reference in unity allows this class to know what player owns this gun
        // so we don't try to fire the wrong players' guns (or every players' guns) 
        // There could be a better way of figuring this out... this works for now though
        public MonoBehaviourPun playerWhoOwnsThisGun;

        private AudioSource audioSource;
        private float nextTimeToFire = 0f; // Used to make sure we don't fire faster than fireRate allows


        [SerializeField]
        private bool gunShootsItselfImplementation = false;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            // If we're not using the implementation where the gun code is responsible for shooting itself..
            if (!gunShootsItselfImplementation)
            {
                // Do nothing
                return;
            }

            // Make sure we don't fire other players' guns  
            // an alternative might be: if(!PhotonView.Get(this).IsMine) ... or something like it
            if (!playerWhoOwnsThisGun.photonView.IsMine)
            {
                return;
            }

            // If the Fire1 (on pc, left mouse click) button was pressed AND
            // If the current time is after the next time we can fire...
            if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
            {
                // Calculate the next time we can fire based on current time and 
                nextTimeToFire = Time.time + 1f / fireRate;
                // Shoot the gun (Duh!)
                Shoot();
            }
        }


        public bool IsReadyToShoot
        {
            get { return Time.time >= nextTimeToFire; }
        }

        /// <summary>
        /// Called by PlayerManager every time the gun needs to be shot.
        /// Protects against shooting faster than firerate allows
        /// 
        /// </summary>
        public void Shoot()
        {
            // Make sure we can't shoot until it's time
            if (!IsReadyToShoot) { return; }

            // Calculate the next time we can fire based on current time and the firerate
            nextTimeToFire = Time.time + 1f / fireRate;

            //Play gunshot sound
            PlayGunShotSound();

            // Play the muzzle flash particle system
            muzzleFlash.Play();

            RaycastHit hit;
            // Create a raycast from fps camera position in the direction it is facing (limit raycast to 'range' distance away)
            // Get back the 'hit' value (what got hit)
            // If cast hit something...
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
            {
                // Log what we hit
                //Debug.LogFormat("Gun: Shoot() hit object: {0}", hit.transform.name);

                // We expect game objects we are concerned about possibly hitting to be marked as targets (by our design)
                // Get the target that was hit (if any)
                ITarget target = hit.transform.GetComponent<ITarget>();
                
                // If we hit a target...
                if (target != null)
                {
                    // Make target take damage 
                    target.TakeDamage(damage);
                }

                // Add force to rigid body of target 
                // If target has rigidboy...
                if (hit.rigidbody != null)
                {
                    // We can add a force either one of two possible directions:
                    // 1) the direction we are looking
                    // 2) the normal of the surface we hit
                    // We'll use the normal (make sure it's negative so it's going backwards)
                    hit.rigidbody.AddForce(-hit.normal * impactForce);
                }

                // Create a new impact effect (GameObject) 
                // (Vector3) hit.point: where in space we our bullet landed
                // Quaternion.LookRotation(hit.normal): a rotation representing the normal of the surface that we hit
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                // Destroy the impact game object after 2 seconds so it doesn't clutter our game heirarchy during gameplay 
                Destroy(impactGO, 2f);
            }
        }


        private void PlayGunShotSound()
        {
            //audioSource.clip = gunshotSound;
            // I read somewhere online that this allows the sounds to overlap
            audioSource.PlayOneShot(gunshotSound);
        }
    }
}