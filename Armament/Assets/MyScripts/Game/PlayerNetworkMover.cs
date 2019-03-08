using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class PlayerNetworkMover : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Tooltip("Player camera whose vertical rotation we want to sync over network")]
        [SerializeField] Camera playerCamera;
        
        private Quaternion rotation; // holds camera rotation info from network
        private readonly float smoothing = 10.0f; // Not exactly sure what smoothing achieves. I probably copied this code from somewhere else

        #region MonoBehaviour Callbacks

        // Update is called once per frame
        void Update()
        {
            // If player is not mine...
            if (!photonView.IsMine)
            {
                // Rotate the opponent's cameras in my local game (not on the network) based on rotation info gathered from network
                playerCamera.transform.rotation = Quaternion.Lerp(playerCamera.transform.rotation, rotation, Time.deltaTime * smoothing);
            }
        }

        #endregion MonoBehaviour Callbacks

        #region IPunObservable Implementation 
        
        /// <summary>
        /// Handles custom synchronization of information over the network
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="info"></param>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // I'm trying to sync the vertical rotation of the camera on the player over the network
            if (stream.IsWriting)
            {
                // Share our player's camera rotation with other clients (Only our player will be executing this)
                stream.SendNext(playerCamera.transform.rotation);
            }
            else
            {
                // Get other players' camera rotations (All players except for our player will be executing this)
                rotation = (Quaternion)stream.ReceiveNext();
            }
        }

        #endregion IPunObservable Implementation 
    }
}