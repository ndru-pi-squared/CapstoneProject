using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropTimer : MonoBehaviourPun, IPunObservable
    {

        #region Private Fields 

        [Tooltip("Time to drop wall (seconds)")]
        [SerializeField] private int time = 60; 

        private const bool DEBUG = false; // indicates whether we are debugging the timer (Debug console output will show if true)
        private int lastDebugTimeLeftOutput = int.MaxValue; // used for limiting the output of TimeLeft during debugging

        private double wallDropTime; // game time that the wall should be dropped
        private double timeLeft; // time left in timer (used to determine when the wall should come down)
        private bool timeIsUp = false; // will be set to true if timeLeft <= 0
        
        #endregion Private Fields

        #region Properties

        public int TimeLeft { get; private set; } // Time left in seconds (used to display to players on jumbotron)
        public bool TimeIsUp { get; private set; } = false; // Used to keep track of whether the timeLeft <= 0 (i.e., time is up)

        #endregion Properties

        #region MonoBehaviour CallBacks

        // Start is called before the first frame update
        void Start()
        {
            // Figure out the wall drop time
            wallDropTime = PhotonNetwork.Time + time;
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
                if (timeLeft <= 0)
                {
                    timeIsUp = true;
                }
            }

            // Update public properties for Jumbotron to use (All clients will do this)
            TimeLeft = Convert.ToInt32(timeLeft);
            TimeIsUp = timeIsUp;

            // If we're debugging the timer AND
            // If it has been atleast 1 second since our last debug output...
            if (DEBUG && TimeLeft <= lastDebugTimeLeftOutput - 1)
            {
                lastDebugTimeLeftOutput = TimeLeft;
                Debug.LogFormat("WallDropTimer: Update() TimeLeft = {0}", TimeLeft);
            }
        }

        #endregion MonoBehaviour CallBacks

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
                // Share timeLeft and timeIsUp with other clients (Only the master client will be executing this)
                stream.SendNext(timeLeft);
                stream.SendNext(timeIsUp);
                // If we're debugging the timer...
                if (DEBUG)
                {
                    Debug.LogFormat("WallDropTimer: OnPhotonSerializeView() SENDING timeLeft = {0}, timeIsUp = {1}", timeLeft, timeIsUp);
                }
            }
            else
            {
                // Get timeLeft and timeIsUp from master client (All clients except for the master client will be executing this)
                timeLeft = (double)stream.ReceiveNext();
                timeIsUp = (bool)stream.ReceiveNext();
                // If we're debugging the timer...
                if (DEBUG)
                {
                    Debug.LogFormat("WallDropTimer: OnPhotonSerializeView() RECIEVING timeLeft = {0}, timeIsUp = {1}", timeLeft, timeIsUp);
                }
            }
        }

        #endregion IPunObservable implementation
    }
}