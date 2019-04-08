using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class FragExplosion : MonoBehaviour
    {
        public AudioClip explosionSound;
        AudioSource audioSource;

       
        // Start is called before the first frame update
        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            audioSource.PlayOneShot(explosionSound, 0.7F);
            //audioSource.PlayOneShot(grenadeSound);
           /* audioSource.clip = grenadeSound;
            audioSource.Play();*/
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
           
        }
    }
}