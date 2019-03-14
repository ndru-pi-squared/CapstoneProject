using UnityEngine;
using UnityEngine.UI;
using System.Collections;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
	/// Player UI. Constraint the UI to follow a PlayerManager GameObject in the world,
	/// Affect a slider and text to display Player's name and health
	/// </summary>
    public class PlayerInfoUI: MonoBehaviour
    {
        #region Private Fields

        [Tooltip("UI Text to display Player's Name")]
        [SerializeField] private Text playerNameText;

        [Tooltip("UI Slider to display Player's Health")]
        [SerializeField] private Slider playerHealthSlider;
        
        /** Note from tutorial:
         *   We need to think ahead here, we'll be looking up for the health regularly, 
         *   so it make sense to cache a reference of the PlayerManager for efficiency.
         */
        private PlayerManager target;
        
        #endregion

        #region MonoBehaviour Callbacks
        
        void Awake()
        {
            /** Note from tutorial:
             *   Why going brute force and find the Canvas this way? Because when scenes are going 
             *   to be loaded and unloaded, so is our Prefab, and the Canvas will be everytime different. To avoid more complex 
             *   code structure, we'll go for the quickest way. However it's really not recommended to use "Find", because this is a 
             *   slow operation. This is out of scope for this tutorial to implement a more complex handling of such case, but a 
             *   good exercise when you'll feel comfortable with Unity and scripting to find ways into coding a better management 
             *   of the reference of the Canvas element that takes loading and unloading into account.
             *  My note:
             *    I changed it from "Canvas" to "Canvas/Top Panel" which is where I want the component to reside in our game. 
             *    ...
             *    Come to think of it... I probably don't even need this code at all because Canvas itself is currently a prefab in our game
             *    and Player UI info is a child of Canvas and not a prefab itself like it was in the tutorial
             */
            /*this.transform.SetParent(GameObject.Find("Canvas/Top Panel").transform,true);
            Vector3 transformPosition = transform.position;
            transformPosition.y = 0;
            this.transform.position = Camera.main.WorldToScreenPoint(new Vector3 (0f, 0f, 0f));
            */
        }

        void Update()
        {
            /** Notes from tutorial:
             *   This code, while easy, is actually quite handy. Because of the way Photon deletes 
             *   Instances that are networked, it's easier for the UI instance to simply destroy itself if the target reference is null. 
             *   This avoids a lot of potential problems, and is very secure, no matter the reason why a target is missing, the related 
             *   UI will automatically destroy itself too, very handy and quick.
             */
            /** My note paraphrasing tutorial: 
             *   - It's so we don't have orphaned UIs when a player leaves the game
             */
            // Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Player over the network
            
            if (target == null)
            {
                Destroy(this.gameObject);
                return;
            }

            // Reflect the Player Health
            if (playerHealthSlider != null)
            {
                playerHealthSlider.value = target.Health;
            }


        }

        void LateUpdate()
        {
            /** Note from tutorial:
             *   So, the trick to match a 2D position with a 3D position is to use the WorldToScreenPoint function of a camera and 
             *   since we only have one in our game, we can rely on accessing the Main Camera which is the default setup for a Unity 
             *   Scene.
             *   Notice how we setup the offset in several steps: first we get the actual position of the target, then we add the 
             *   characterControllerHeight, and finally, after we've deduced the screen position of the top of the Player, we add the 
             *   screen offset.
             */
            /** My note:
             *   - How is targetTransform != null ?!?!?
             *   - Answer: I don't think it ever is in the tutorial code! WTF!
             *   - My Solution: I added "targetTransform = _target.transform;" in SetTarget()
             */
            // #Critical
            // Follow the Target GameObject on screen.
            /*if (targetTransform != null)
            {
                targetPosition = targetTransform.position;
                targetPosition.y += characterControllerHeight;
                this.transform.position = Camera.main.WorldToScreenPoint(targetPosition) + screenOffset;
            }*/
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
