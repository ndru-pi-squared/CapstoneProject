// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CountdownTimer.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
// This is a basic CountdownTimer. In order to start the timer, the MasterClient can add a certain entry to the Custom Room Properties,
// which contains the property's name 'StartTime' and the actual start time describing the moment, the timer has been started.
// To have a synchronized timer, the best practice is to use PhotonNetwork.Time.
// In order to subscribe to the CountdownTimerHasExpired event you can call CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
// from Unity's OnEnable function for example. For unsubscribing simply call CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;.
// You can do this from Unity's OnDisable function for example.
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

using ExitGames.Client.Photon;
using Photon.Pun;
using System;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// This is a basic CountdownTimer. In order to start the timer, the MasterClient can add a certain entry to the Custom Room Properties,
    /// which contains the property's name 'StartTime' and the actual start time describing the moment, the timer has been started.
    /// To have a synchronized timer, the best practice is to use PhotonNetwork.Time.
    /// In order to subscribe to the CountdownTimerHasExpired event you can call CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
    /// from Unity's OnEnable function for example. For unsubscribing simply call CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;.
    /// You can do this from Unity's OnDisable function for example.
    /// </summary>
    [RequireComponent(typeof(GameManager))]
    public class CountdownTimer : MonoBehaviourPunCallbacks
    {

        #region Public Fields

        /// <summary>
        /// OnCountdownTimer1HasExpired delegate.
        /// </summary>
        public delegate void CountdownTimer1HasExpired();
        /// <summary>
        /// OnCountdownTimer2HasExpired delegate.
        /// </summary>
        public delegate void CountdownTimer2HasExpired();

        /// <summary>
        /// Called when the timer has expired.
        /// </summary>
        public static event CountdownTimer1HasExpired OnCountdownTimer1HasExpired;
        /// <summary>
        /// 
        /// Called when the timer has expired.
        /// </summary>
        public static event CountdownTimer2HasExpired OnCountdownTimer2HasExpired;

        #endregion Public Fields

        #region Private Fields

        private bool isTimer1Running; // default value = false
        private bool isTimer2Running; // default value = false
        private float startTime1;
        private float startTime2;

        #endregion Private Fields

        #region Properties

        public int Timer1TimeLeft { get; private set; } // Time left in seconds (used to display to players on jumbotron)
        public bool Timer1TimeIsUp { get; private set; } = false; // Used to keep track of whether the timeLeft <= 0 (i.e., time is up)

        public int Timer2TimeLeft { get; private set; } // Time left in seconds (used to display to players on jumbotron)
        public bool Timer2TimeIsUp { get; private set; } = false; // Used to keep track of whether the timeLeft <= 0 (i.e., time is up)
        
        #endregion Properties

        #region Private Methods

        void CheckForTimerInfo(ExitGames.Client.Photon.Hashtable props)
        {
            if (props.TryGetValue(GameManager.KEY_STAGE_1_COUNTDOWN_START_TIME, out object startTime1FromProps))
            {
                if (startTime1FromProps != null)
                {
                    isTimer1Running = true;
                    Timer1TimeIsUp = false;
                    startTime1 = (float)startTime1FromProps;
                }
            }

            if (props.TryGetValue(GameManager.KEY_STAGE_2_COUNTDOWN_START_TIME, out object startTime2FromProps))
            {
                if (startTime2FromProps != null)
                {
                    isTimer2Running = true;
                    Timer2TimeIsUp = false;
                    startTime2 = (float)startTime2FromProps;
                }
            }
        }

        #endregion Private Methods

        #region MonoBehaviour Callbacks

        void Start()
        {
            CheckForTimerInfo(PhotonNetwork.CurrentRoom.CustomProperties);
        }

        void Update()
        {
            if (isTimer1Running)
            {
                float timer1 = (float)PhotonNetwork.Time - startTime1; // counts up from 0 
                float countdown1 = GameManager.Instance.Stage1Time - timer1;

                Timer1TimeLeft = Convert.ToInt32(Mathf.Ceil(countdown1));
                
                if (countdown1 <= 0.0f)
                {
                    isTimer1Running = false;
                    Timer1TimeIsUp = true;

                    OnCountdownTimer1HasExpired?.Invoke();
                }
            }

            if (isTimer2Running)
            {
                float timer2 = (float)PhotonNetwork.Time - startTime2;
                float countdown2 = GameManager.Instance.Stage2Time - timer2;

                Timer2TimeLeft = Convert.ToInt32(Mathf.Ceil(countdown2));

                if (countdown2 <= 0.0f)
                {
                    isTimer2Running = false;
                    Timer2TimeIsUp = true;

                    OnCountdownTimer2HasExpired?.Invoke();
                }
            }
        }

        #endregion MonoBehaviour Callbacks

        #region MonoBehaviourPun Callbacks

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            CheckForTimerInfo(propertiesThatChanged);
        }

        #endregion MonoBehaviourPun Callbacks
    }
}