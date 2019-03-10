using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Helps us store custom properties for Photon Player objects
    /// </summary>
    public class PlayerProperties : MonoBehaviour
    {
        public const string KEY_KILLS = "Kills";
        public const string KEY_DEATHS = "Deaths";
        public const string KEY_ISALIVE = "IsAlive";

        public ExitGames.Client.Photon.Hashtable Properties { get; set; } // the properties hashtable given to Photon's Player.SetCustomProperties method

        private void Awake()
        {
            Properties = new ExitGames.Client.Photon.Hashtable();
            // Add properties to the hashtable with default/initial values
            Properties.Add(KEY_KILLS, 0);
            Properties.Add(KEY_DEATHS, 0);
            Properties.Add(KEY_ISALIVE, true);
        }

    }
}
