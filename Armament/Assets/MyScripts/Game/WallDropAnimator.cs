using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropAnimator : MonoBehaviour
    {

        #region Public Fields

        /// <summary>
        /// OnWallReachedDropPosition delegate.
        /// </summary>
        public delegate void WallReachedDropPosition();

        /// <summary>
        /// Called when the wall has reached dropped position
        /// </summary>
        public static event WallReachedDropPosition OnWallReachedDropPosition;

        #endregion Public Fields

        #region Private Serializable Fields

        [Tooltip("Related to the time it takes for wall to drop the distance equal to its height")]
        [SerializeField] private float dropTime = 10;

        #endregion Private Serializable Fields

        #region Private Fields

        private const bool DEBUG = true; // indicates whether we are debugging this class (Debug console output will show if true)
        private const bool DEBUG_FixedUpdate = true;
        private const bool DEBUG_ResetWallPosition = false;

        private Vector3 dropPosition; // stores the final position of the wall after it is dropped
        private Vector3 originalWallPosition; // keeps track of the original wall position for ResetWallPosition()
        private float originalY;
        private object[] instantiationData;
        private bool wallReachedDropPosition; // keeps track of whether the navmesh is ready to update

        #endregion Private Fields

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            instantiationData = GetComponent<PhotonView>().InstantiationData;
            originalWallPosition = (Vector3)instantiationData[0];
        }

        void Start()
        {
            // Figure out what the final position of the wall should be after it is dropped
            dropPosition = transform.position - new Vector3(0f, transform.localScale.y * 1.25f, 0f); // current wall position - height of wall
        }

        void FixedUpdate()
        {
            // We only want the master client to move the wall (when it is time to do so)
            // If this client is master client...
            if (PhotonNetwork.IsMasterClient)
            {
                // If it is time to drop the wall...
                if (GameManager.Instance.gameObject.GetComponent<CountdownTimer>().Timer1TimeIsUp)
                {
                    // If wall has not yet reached its dropped position...
                    if (transform.position.y > originalWallPosition.y - transform.localScale.y)
                    {
                        // This code doesn't move the wall as I expected but I kind of like how the wall slows down as it drops...
                        // (In other words, the drop is exponential instead of linear)
                        // Drop the position of where we want the wall to be
                        transform.position = Vector3.Lerp(transform.position, dropPosition, Time.fixedDeltaTime / dropTime);
                    }
                    // If wall has reached its dropped position AND navmesh has not been updated yet
                    else if (!wallReachedDropPosition)
                    {
                        wallReachedDropPosition = true;
                        OnWallReachedDropPosition?.Invoke();
                    }
                }
            }
        }

        public void ResetWallPosition()
        {
            transform.position = originalWallPosition;
        }

        #endregion MonoBehaviour Callbacks
        
    }
}