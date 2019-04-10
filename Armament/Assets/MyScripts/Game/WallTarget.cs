using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallTarget : MonoBehaviourPunCallbacks, ITarget
    {
        // Key references for the Room CustomProperties hash table (so we don't use messy string literals)
        public const string KEY_WALL_HEALTH = "Wall Health";

        [SerializeField] private float health = 100f;
        MeshRenderer meshRenderer;
        private float originalHealth; // keeps track of original health value


        #region MonoBehaviour CallBacks

        void Awake()
        {
            originalHealth = health;
            meshRenderer = GetComponent<MeshRenderer>();
            SyncWallHealth();
        }

        #endregion MonoBehaviour CallBacks

        #region MonoBehaviourPun Callbacks

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {   
            if (!PhotonNetwork.IsMasterClient)
            {
                // Get the current health of the wall from Room CustomProperties
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_WALL_HEALTH, out object value))
                {
                    health = (float)value;
                    UpdateWallColor();

                    if (health <= 0)
                        GetComponent<BoxCollider>().enabled = false;
                    else
                        GetComponent<BoxCollider>().enabled = true;
                }
            }
        }

        #endregion MonoBehaviourPun Callbacks

        #region Public Methods

        public void TakeDamage(float amount)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                health -= amount;
                UpdateWallColor();

                SyncWallHealth();

                if (health <= 0)
                    GetComponent<BoxCollider>().enabled = false;
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
        
        public void ResetHealth()
        {
            // Reset wall health
            health = originalHealth;
            UpdateWallColor();

            SyncWallHealth();

            // Re-enable wall's box collider
            GetComponent<BoxCollider>().enabled = true;
        }

        #endregion Public Methods

        #region Private Methods

        void SyncWallHealth()
        {
            // If we are the master client...
            if (PhotonNetwork.IsMasterClient)
            {
                // Sync current wall health in Room CustomProperties
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { {KEY_WALL_HEALTH, health }});
            }
        }

        void UpdateWallColor()
        {
            // As the wall takes damage the color changes from white to black
            //meshRenderer.material.color = Color.HSVToRGB(0, 0, Math.Max(health, 0) / originalHealth);
            
            // Trying to make wall more transparent as health decreases - not yet successful
            float tickleMeElmoFactor = Math.Max(health, 0) / originalHealth;
            Color color = Color.HSVToRGB(0, 0, tickleMeElmoFactor);
            color.a = tickleMeElmoFactor;
            meshRenderer.material.color = color;
        }

        #endregion Private Methods
    }
}
