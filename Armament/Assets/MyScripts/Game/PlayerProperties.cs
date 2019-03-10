using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Helps us store custom properties for Photon Player objects
    /// <para>Example properties:</para>
    /// <para>- Number of player kills</para>
    /// <para>- Number of player player deaths</para>
    /// <para>- Is the player still alive</para>
    /// </summary>
    public class PlayerProperties : MonoBehaviour
    {
        // Key references
        public const string KEY_KILLS = "Kills";
        public const string KEY_DEATHS = "Deaths";
        public const string KEY_ISALIVE = "IsAlive";
        public const string KEY_TEAM = "Team";

        // Team name references
        public const string TEAM_NAME_A = "A";
        public const string TEAM_NAME_B = "B";
        public const string TEAM_NAME_SPECT = "Spectator";

        public ExitGames.Client.Photon.Hashtable Properties { get; set; } // the properties hashtable given to Photon's Player.SetCustomProperties method

        private void Awake()
        {
            Properties = new ExitGames.Client.Photon.Hashtable();
            // Add properties to the hashtable with default/initial values
            //Properties.Add(KEY_KILLS, 0);
            //Properties.Add(KEY_DEATHS, 0);
            //Properties.Add(KEY_ISALIVE, true);
            //Properties.Add(KEY_TEAM, TEAM_NAME_SPECT);
        }

    }
}
