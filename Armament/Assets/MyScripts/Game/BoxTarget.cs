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

        /// <summary>
        /// Box Target doesn't care who damages it so it just calles TakeDamage(float amount)
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="player"></param>
        public void TakeDamage(float amount, PlayerManager player)
        {
            TakeDamage(amount);
        }

        void Die()
        {
            Destroy(gameObject);
        }
    }
}
