using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropAnimator : MonoBehaviour, IPunObservable
    {
        [SerializeField] private WallDropTimer wallDropTimer;
        [Tooltip("Related to the time it takes for wall to drop the distance equal to its height")]
        [SerializeField] private float dropTime = 10;

        private const bool DEBUG = true; // indicates whether we are debugging this class (Debug console output will show if true)

        private Vector3 dropPosition; // stores the final position of the wall after it is dropped
        private Vector3 position; // stores the current position we want the wall to be

        #region MonoBehaviour Callbacks

        // Start is called before the first frame update
        void Start()
        {
            // Figure out what the final position of the wall should be after it is dropped
            float wallHeight = transform.localScale.y;
            dropPosition = transform.position - new Vector3(0f, wallHeight, 0f);
            // Initialize the position we want the wall to be with the current *actual* position of the wall
            position = transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            // We only want the master client to move the wall (when it is time to do so)
            // If this client is master client...
            if (PhotonNetwork.IsMasterClient)
            {
                // If it is time to drop the wall...
                if (wallDropTimer.TimeIsUp)
                {
                    // This code doesn't move the wall as I expected but I kind of like how the wall slows down as it drops...
                    // Drop the position of where we want the wall to be
                    position = Vector3.Lerp(transform.position, dropPosition, Time.deltaTime / dropTime);
                }
            }
            
            // Set the *actual* position of the wall to be the position we want it to be
            transform.position = position;
        }

        #endregion MonoBehaviour Callbacks

        #region IPunObservable implementation
        
        /// <summary>
        /// Handles custom synchronization of information over the network
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="info"></param>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            
            // Sync the wall position over the network
            if (stream.IsWriting)
            {
                // Share wall position with other clients (Only the master client will be executing this)
                stream.SendNext(transform.position);
                // If we're debugging...
                if (DEBUG)
                {
                    Debug.LogFormat("WallDropAnimator: OnPhotonSerializeView() SENDING transform.position = {0}", transform.position);
                }
            }
            else
            {
                // Get position our wall should be from master client (All clients except for the master client will be executing this)
                position = (Vector3)stream.ReceiveNext();
                // If we're debugging...
                if (DEBUG)
                {
                    Debug.LogFormat("WallDropAnimator: OnPhotonSerializeView() RECIEVING transform.position = {0}", transform.position);
                }
            }
        }

        #endregion IPunObservable implementation
    }
}