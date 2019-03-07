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
        private Vector3 dropPosition; // stores the final position of the wall after it is dropped
        private Vector3 position; // stores the current position we want the wall to be

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Sync the wall position over the network
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
            }
            else
            {
                position = (Vector3)stream.ReceiveNext();
            }
        }

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
                if (wallDropTimer.WallDropped)
                {
                    // This code doesn't move the wall as I expected but I kind of like how the wall slows down as it drops...
                    // Drop the wall
                    position = Vector3.Lerp(transform.position, dropPosition, Time.deltaTime / dropTime);
                }
            }
            
            // Set the *actual* position of the wall to be the position we want it to be
            transform.position = position;

        }
    }
}