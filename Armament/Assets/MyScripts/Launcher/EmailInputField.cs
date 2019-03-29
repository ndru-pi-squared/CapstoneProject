using UnityEngine;
using UnityEngine.UI;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    ///  Player name input field. Let the user input his name
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public class EmailInputField : MonoBehaviour
    {
        #region Private Constants

        // Store the PlayerPref Key to avoid typos
        const string PREF_KEY_EMAIL = "Email";

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            string defaultEmail = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if (_inputField != null)
            {
                if (PlayerPrefs.HasKey(PREF_KEY_EMAIL))
                {
                    _inputField.text = PlayerPrefs.GetString(PREF_KEY_EMAIL);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the name of the player, and save it in the PlayerPrefs for future sessions.
        /// </summary>
        /// <param name="value">The name of the player</param>
        public void SetEmail(string value)
        {
            // #Important
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError("User Name is null or empty");
                return;
            }

            PlayerPrefs.SetString(PREF_KEY_EMAIL, value);
        }

        #endregion
    }
}