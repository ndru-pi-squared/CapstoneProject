using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;


using System.Collections;



namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    ///  Player name input field. Let the user input his name
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public class PlayerNameInputField : MonoBehaviour
    {
        #region Private Constants

        // Store the PlayerPref Key to avoid typos
        const string KEY_PREF_PLAYERNAME = "PlayerName";
        const string KEY_PREF_USERNAME = "UserName";

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            string defaultPlayerName = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if(_inputField != null)
            {


                // ***
                //
                // This logic is currently a little screwy (screwey? screweee? screw to the eeeeee?).
                // Might need changing.
                // We're going to grab the playfab username (used for login) and use it as our photon player's nickname.
                //
                // ***

                // Try to set the Photon player name to the Playfab username
                if (PlayerPrefs.HasKey(KEY_PREF_USERNAME))
                {
                    defaultPlayerName = PlayerPrefs.GetString(KEY_PREF_USERNAME);
                    _inputField.text = defaultPlayerName;
                }
                
                /* 
                if (PlayerPrefs.HasKey(KEY_PREF_PLAYERNAME))
                {
                    defaultPlayerName = PlayerPrefs.GetString(KEY_PREF_PLAYERNAME);
                    _inputField.text = defaultPlayerName;
                }*/
            }

            PhotonNetwork.NickName = defaultPlayerName;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Sets the name of the player, and save it in the PlayerPrefs for future sessions.
        /// </summary>
        /// <param name="value">The name of the player</param>
        public void SetPlayerName(string value)
        {
            // #Important
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError("Player Name is null or empty");
                return;
            }
            PhotonNetwork.NickName = value;
            
            PlayerPrefs.SetString(KEY_PREF_PLAYERNAME, value);
        }
        
        #endregion
    }
}