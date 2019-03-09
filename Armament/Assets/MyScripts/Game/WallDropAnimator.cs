using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropAnimator : MonoBehaviour
    {
        #region Private Serializable Fields

        [SerializeField] private WallDropTimer wallDropTimer;
        [Tooltip("Related to the time it takes for wall to drop the distance equal to its height")]
        [SerializeField] private float dropTime = 10;

        #region Private Serializable Fields

        #region Private Fields

        private const bool DEBUG = true; // indicates whether we are debugging this class (Debug console output will show if true)

        private Vector3 dropPosition; // stores the final position of the wall after it is dropped

        #region Private Fields

        #region MonoBehaviour Callbacks

        // Start is called before the first frame update
        void Start()
        {
            // Figure out what the final position of the wall should be after it is dropped
            dropPosition = transform.position - new Vector3(0f, transform.localScale.y, 0f); // current wall position - height of wall
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
                    // (In other words, the drop is exponential instead of linear)
                    // Drop the position of where we want the wall to be
                    transform.position = Vector3.Lerp(transform.position, dropPosition, Time.deltaTime / dropTime);
                }
            }
            
        }

        #endregion MonoBehaviour Callbacks
        
    }
}