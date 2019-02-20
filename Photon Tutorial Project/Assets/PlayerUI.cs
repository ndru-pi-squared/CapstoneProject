using UnityEngine;
using UnityEngine.UI;


using System.Collections;


namespace Com.Kabaj.PhotonTutorialProject
{
    public class PlayerUI : MonoBehaviour
    {
        #region Private Fields


        [Tooltip("UI Text to display Player's Name")]
        [SerializeField]
        private Text playerNameText;


        [Tooltip("UI Slider to display Player's Health")]
        [SerializeField]
        private Slider playerHealthSlider;

        [Tooltip("Pixel offset from the player target")]
        [SerializeField]
        private Vector3 screenOffset = new Vector3(0f, 30f, 0f);

        /** Note from tutorial:
         *   We need to think ahead here, we'll be looking up for the health regularly, 
         *   so it make sense to cache a reference of the PlayerManager for efficiency.
         */
        private PlayerManager target;

        float characterControllerHeight = 0f;
        Transform targetTransform;
        Vector3 targetPosition;


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
             */
            this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
        }

        void Update()
        {
            // Reflect the Player Health
            if (playerHealthSlider != null)
            {
                playerHealthSlider.value = target.Health;
            }

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
            if (targetTransform != null)
            {
                targetPosition = targetTransform.position;
                targetPosition.y += characterControllerHeight;
                this.transform.position = Camera.main.WorldToScreenPoint(targetPosition) + screenOffset;
            }
        }


        #endregion


        #region Public Methods


        /** My note:
         *   - This function is called in two places in PlayerManager after PlayerUiPrefab instantiation with this code:
         *     _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
         *   - _uiGo represents a user interface game object
         */
        public void SetTarget(PlayerManager _target)
        {
            if (_target == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
                return;
            }
            // Cache references for efficiency
            target = _target;
            if (playerNameText != null)
            {
                playerNameText.text = target.photonView.Owner.NickName;
            }

            /** My Note: 
             *   - I added this code to test a method of setting targetTransform to fix the PlayerUIPrefab position glitch
             *     Tested by building and running a copy of the game while running game in unity. 
             *   - Result: apparent success!
             *   - Additional Note: I figured out later that the position "glitch" wasn't so much a glitch in position but 
             *     a lack of positioning all together. I think this is explains everything. If targetTransform was never
             *     being set it remains null and the positioning code at the bottom of LateUpdate() never gets executed.
             */
            targetTransform = _target.transform;

            /** Note from tutorial:
             *   We know our player to be based off a CharacterController, which features a Height property, we'll need 
             *   this to do a proper offset of the UI element above the Player.
             */
            CharacterController characterController = _target.GetComponent<CharacterController>();
            // Get data from the Player that won't change during the lifetime of this Component
            if (characterController != null)
            {
                characterControllerHeight = characterController.height;
            }
        }


        #endregion


    }
}