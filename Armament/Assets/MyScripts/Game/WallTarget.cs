using Photon.Pun;
using System;
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

        void Awake()
        {
            originalHealth = health;
            meshRenderer = GetComponent<MeshRenderer>();
        }

        #endregion MonoBehaviour CallBacks

        #region Public Methods

        public void TakeDamage(float amount)
        {
            health -= amount;
            UpdateWallColor();

            if (health <= 0)
                GetComponent<BoxCollider>().enabled = false;
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
        
        public void ResetHealth()
        {
            // Reset wall health
            health = originalHealth;
            UpdateWallColor();

            // Re-enable wall's box collider
            GetComponent<BoxCollider>().enabled = true;
        }

        #endregion Public Methods

        #region Private Methods

        void UpdateWallColor()
        {
            // As the wall takes damage the color changes from white to black
            //meshRenderer.material.color = Color.HSVToRGB(0, 0, Math.Max(health, 0) / originalHealth);
            
            // Trying to make wall more transparent as health decreases - not yet successful
            int tickleMeElmoFactor = Convert.ToInt32(Math.Floor(Math.Max(health, 0) / originalHealth));
            Color color = Color.HSVToRGB(0, 0, tickleMeElmoFactor);
            color.a = tickleMeElmoFactor;
            meshRenderer.material.color = color;
        }

        #endregion Private Methods
    }
}
