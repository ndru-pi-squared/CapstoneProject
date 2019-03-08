using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropTimer : MonoBehaviourPun, IPunObservable
    {
        [Tooltip("Time to drop wall (seconds)")]
        [SerializeField] private int time = 60;

        private double startTime; // game time that the wall timer began
        private double wallDropTime; // game time that the wall should be dropped
        private double timeLeft; // time left in timer (used to determine when the wall should come down)
        private bool timeIsUp = false;

        #region Properties

        public int TimeLeft { get; private set; } // Time left in seconds (used to display to players)
        public bool TimeIsUp { get; private set; } = false; // Used to keep track of whether the timeLeft <= 0 (i.e., time is up)

        #endregion Properties

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Sync the wall position over the network
            if (stream.IsWriting)
            {
                // Share timeLeft with other clients (Only the master client will be executing this)
                stream.SendNext(timeLeft);
                stream.SendNext(timeIsUp);
                Debug.LogFormat("WallDropTimer: OnPhotonSerializeView() SENDING timeLeft = {0}, timeIsUp = {1}", timeLeft, timeIsUp);
            }
            else
            {
                // Get timeLeft from master client (All clients except for the master client will be executing this)
                timeLeft = (double)stream.ReceiveNext();
                timeIsUp = (bool)stream.ReceiveNext();
                Debug.LogFormat("WallDropTimer: OnPhotonSerializeView() RECIEVING timeLeft = {0}, timeIsUp = {1}", timeLeft, timeIsUp);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // Set the starting time
            startTime = PhotonNetwork.Time;
            // Figure out the wall drop time
            wallDropTime = startTime + time;
            // Initialize timeLeftDisplayed to be more than time
            timeLeft = TimeLeft = time + 1;
        }

        // Update is called once per frame
        void Update()
        {
            // We only want the master client to change the countdown timer
            // If this client is master client...
            if (PhotonNetwork.IsMasterClient)
            {
                // Update the time left in timer
                // timeLeft will be shared with all other clients
                timeLeft = wallDropTime - PhotonNetwork.Time;
                
                // Set the 
                //TimeLeft = (int)timeLeft;

                // If the time is up...
                if (timeLeft <= 0)
                {
                    if (!TimeIsUp)
                    {
                        timeIsUp = true;
                        //photonView.RPC("DropWall", RpcTarget.All);
                    }
                }
                else
                {
                    //photonView.RPC("SetTimer", RpcTarget.All, Convert.ToInt32(timeLeft));
                }
            }

            TimeLeft = Convert.ToInt32(timeLeft);
            TimeIsUp = timeIsUp;
            Debug.LogFormat("WallDropTimer: Update() TimeLeft = {0}", TimeLeft);
        }     

        [PunRPC]
        void SetTimer(int t)
        {
            TimeLeft = t;
            Debug.LogFormat("WallDropTimer: SetTimer() TimeLeft = {0}", TimeLeft);
        }

        [PunRPC]
        void DropWall()
        {
            TimeIsUp = true;
            Debug.LogFormat("WallDropTimer: DropWall()");
        }
    }
}