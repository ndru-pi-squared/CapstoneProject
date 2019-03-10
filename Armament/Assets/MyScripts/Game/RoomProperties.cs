using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Helps us store custom properties for game room
    /// <para>Example properties:</para>
    /// <para>- Number of Players on Team A</para>
    /// <para>- Number of Players Team B</para>
    /// </summary>
    public class RoomProperties : MonoBehaviour
    {
        public const string KEY_TEAM_A_PLAYERS_COUNT = "Team A Size";
        public const string KEY_TEAM_B_PLAYERS_COUNT = "Team B Size";

        public ExitGames.Client.Photon.Hashtable Properties { get; set; } // the properties hashtable given to Photon's Room.SetCustomProperties method

        private void Awake()
        {
            Properties = new ExitGames.Client.Photon.Hashtable();
            //Properties.Add(KEY_TEAM_A_PLAYERS_COUNT, 0);
            //Properties.Add(KEY_TEAM_B_PLAYERS_COUNT, 0);
        }

    }
}