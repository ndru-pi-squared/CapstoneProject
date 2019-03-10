using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallTarget : MonoBehaviour, ITarget
    {
        [SerializeField] private float health = 100f;
        MeshRenderer meshRenderer;
        private float originalHealth; // keeps track of original health value

        #region MonoBehaviour CallBacks

        void Start()
        {
            originalHealth = health;
            meshRenderer = GetComponent<MeshRenderer>();
        }

        #endregion MonoBehaviour CallBacks

        #region Public Methods

        public void TakeDamage(float amount)
        {
            health -= amount;

            // As the wall takes damage the color changes from white to black
            meshRenderer.material.color = Color.HSVToRGB(0, 0, health/originalHealth);

            if (health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Wall Target doesn't care who damages it so it just calles TakeDamage(float amount)
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="player"></param>
        public void TakeDamage(float amount, PlayerManager player)
        {
            TakeDamage(amount);
        }

        #endregion Public Methods

        #region Private Methods

        void Die()
        {
            Destroy(gameObject);
        }

        #endregion Private Methods
    }
}
