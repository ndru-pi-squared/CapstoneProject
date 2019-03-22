using UnityEngine;
using UnityEngine.UI;
using System.Collections;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
	/// Player Info UI. Constraint the UI to follow a PlayerManager GameObject in the world,
	/// Affect a slider and text to display Player's name and health
	/// </summary>
    public class PlayerInfoUI: MonoBehaviour
    {
        #region Private Fields

        [Tooltip("UI Text to display Player's Name")]
        [SerializeField] private Text playerNameText;

        [Tooltip("UI Slider to display Player's Health")]
        [SerializeField] private Slider playerHealthSlider;
        
        private PlayerManager target;
        
        #endregion

        #region MonoBehaviour Callbacks
        
        void Awake()
        {
        }

        void Update()
        {
            // Reflect the Player Health
            if (playerHealthSlider != null && target != null)
            {
                //Debug.LogFormat("PlayerInfoUI: Update() targert = {0}", target);
                playerHealthSlider.value = target.Health;
            }


        }

        #endregion
    
        #region Public Methods
        
        /// <summary>
		/// Assigns a Player Target to Follow and represent.
		/// </summary>
		/// <param name="target">Target.</param>
		public void SetTarget(PlayerManager _target)
        {

            if (_target == null)
            {
                Debug.LogError("<Color=Red><b>Missing</b></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
                return;
            }

            // Cache references for efficiency because we are going to reuse them.
            this.target = _target;

            // Set player name on UI
            if (playerNameText != null)
            {
                playerNameText.text = this.target.photonView.Owner.NickName;
            }
        }

        #endregion


    }
}
