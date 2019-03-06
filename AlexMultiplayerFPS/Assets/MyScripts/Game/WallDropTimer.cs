using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropTimer : MonoBehaviour
    {
        [Tooltip("Time to drop wall (seconds)")]
        [SerializeField] private int time = 10;

        private float startTime; // game time that the wall timer began
        private float wallDropTime; // game time that the wall should be dropped
        private float timeLeft; // time left in timer (used to determine when the wall should come down)

        #region Properties

        public int TimeLeft { get; private set; } // Time left in seconds (used to display to players)
        public bool WallDropped { get; private set; } = false; // Used to keep track of whether the wall was dropped

        #endregion Properties

        // Start is called before the first frame update
        void Start()
        {
            // Set the starting time
            startTime = Time.time;
            // Figure out the wall drop time
            wallDropTime = startTime + time;
            // Initialize timeLeftDisplayed to be more than time
            TimeLeft = time + 1;

        }

        // Update is called once per frame
        void Update()
        {
            // Update the time left in timer
            timeLeft = wallDropTime - Time.time;
            TimeLeft = Mathf.RoundToInt(timeLeft);

            // If the time is up...
            if (timeLeft <= 0)
            {
                if (!WallDropped)
                {
                    DropWall();
                }
            }
        }

        void DropWall()
        {
            WallDropped = true;
            Debug.Log("WallDropTimer: DropWall()");
            // Disable this wall
            //gameObject.SetActive(false);
        }
    }
}