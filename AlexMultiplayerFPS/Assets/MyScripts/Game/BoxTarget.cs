using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class BoxTarget : MonoBehaviour, ITarget
    {
        [SerializeField]
        private float Health = 100f;

        public void TakeDamage(float amount)
        {
            Health -= amount;
            if (Health <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            Destroy(gameObject);
        }
    }
}
