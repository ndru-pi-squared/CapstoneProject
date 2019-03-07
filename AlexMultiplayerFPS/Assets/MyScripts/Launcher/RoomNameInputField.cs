using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Room name input field. Let the user input room name
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [RequireComponent(typeof(InputField))]
    public class RoomNameInputField : MonoBehaviour
    {
        #region Private Constants


        // Store the PlayerPref Key to avoid typos
        const string roomNamePrefKey = "RoomName";


        #endregion


        #region MonoBehaviour CallBacks


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            string defaultName = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if (_inputField != null)
            {
                if (PlayerPrefs.HasKey(roomNamePrefKey))
                {
                    defaultName = PlayerPrefs.GetString(roomNamePrefKey);
                    _inputField.text = defaultName;
                }
            }

            PhotonNetwork.NickName = defaultName;
        }


        #endregion


        #region Public Methods


        /// <summary>
        /// Save room name in the PlayerPrefs for future sessions.
        /// </summary>
        /// <param name="value">The room name</param>
        public void SetRoomName(string value)
        {
            // #Important
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError("Room Name is null or empty");
                return;
            }

            PlayerPrefs.SetString(roomNamePrefKey, value);
        }


        #endregion
    }
}