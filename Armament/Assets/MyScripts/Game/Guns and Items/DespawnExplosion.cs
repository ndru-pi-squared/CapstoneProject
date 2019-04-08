using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class Explosion : MonoBehaviour
    {
        AudioSource audioSource;
        public AudioClip grenadeSound;
        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
            {
                Debug.LogError("Grenade is Missing Audio Source Component", this);
            }
            PlaySound();
        }

        // Update is called once per frame
        void Update()
        {
            if (!this.transform.GetChild(0).GetComponent<ParticleSystem>().isPlaying)//polling each time it updates, which is probably bad, but it only does it for 3 sec.
            {                                                                        //destroying the explosion was otherwise cumbersome. 
                                                                                     //for some reason i was not able to destroy it by calling Destroy on the spawnedParticle, so this is a temp fix.
                Destroy(this.gameObject);
            }
        }

        public void PlaySound()
        {
            audioSource.PlayOneShot(grenadeSound);
        }
    }
}