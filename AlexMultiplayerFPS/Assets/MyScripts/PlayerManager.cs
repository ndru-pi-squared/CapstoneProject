using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using Photon.Pun;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Player manager.
    /// Handles fire Input and Beams.
    /// </summary>
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable, ITarget
    {

        // Note: I changed this from "Private Fields" to "Public Fields" because it didn't make any sense to me
        #region Public Fields

        [Tooltip("The current Health of our player")]
        public float Health = 100f;

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

        #endregion

        #region Private Fields

        [Tooltip("Gun owned by player")]
        [SerializeField]
        private Gun gun;
        
        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField]
        private GameObject playerUiPrefab;

        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField]
        private bool usingPlayerUIPrefab = false;

        //True, when the user is firing
        bool IsFiring;

        #endregion

        #region MonoBehaviour CallBacks


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                PlayerManager.LocalPlayerInstance = this.gameObject;
            }
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
           /** Notes from tutorial:
             *   All of this is standard Unity coding. However notice that we are sending a message to the instance we've just created. We 
             *   require a receiver, which means we will be alerted if the SetTarget did not find a component to respond to it. Another 
             *   way would have been to get the PlayerUI component from the instance, and then call SetTarget directly. It's generally 
             *   recommended to use components directly, but it's also good to know you can achieve the same thing in various ways.
             */
            if (playerUiPrefab != null)
            {
                if (usingPlayerUIPrefab)
                {
                    // This code is also used in CalledOnLevelWasLoaded()
                    GameObject _uiGo = Instantiate(playerUiPrefab);
                    _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
                }
            }
            else
            {
                Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
            }

            /** Notes from tutorial:
            *   There is currently an added complexity because Unity has revamped "Scene Management" and Unity 5.4 has deprecated 
            *   some callbacks, which makes it slightly more complex to create a code that works across all Unity versions (from Unity 
            *   5.3.7 to the latest). So we'll need different code based on the Unity version. It's unrelated to Photon Unity Networking, but 
            *   important to master for your projects to survive updates.
            */
#if UNITY_5_4_OR_NEWER
            /** My Note:
             *   I'm replacing this code from the tutorial with similar code from the demo package
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, loadingMode) => // I'm guessing this creates an anonymous function with parameters scene and loadingMode
                    {
                        this.CalledOnLevelWasLoaded(scene.buildIndex);
                    };
            */
            // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

#endif

        }

        /** My Note:
         *   Added this function because it's in the demo package script
         */
        public override void OnDisable()
        {
            // Always call the base to remove callbacks
            base.OnDisable();

#if UNITY_5_4_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
#endif
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// </summary>
        void Update()
        {
            // Figure out what should be done if this is the player I'm controlling
            if (photonView.IsMine)
            {
                ProcessInputs();

            }

        }

        /// <summary>
        /// MonoBehaviour method called when the Collider 'other' enters the trigger.
        /// Affect Health of the Player if the collider is a beam
        /// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
        /// One could move the collider further away to prevent this or check if the beam belongs to the player.
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            /*if (!photonView.IsMine)
            {
                return;
            }*/
            Debug.LogFormat("PlayerManager: OnTriggerEnter() Collided with object with name \"{0}\"", other.name);

            if (other.CompareTag("Weapon"))
            {
                Debug.LogFormat("PlayerManager: OnTriggerEnter() Collided with weapon with name \"{0}\"", other.name);
                other.gameObject.SetActive(false);
            }
        }
       
        /// <summary>
        /// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
        /// We're going to affect health while the beams are touching the player
        /// </summary>
        /// <param name="other">Other.</param>
        void OnTriggerStay(Collider other)
        {
            /*// we dont' do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }
            // We are only interested in Beamers
            // we should be using tags but for the sake of distribution, let's simply check by name.
            if (!other.name.Contains("Beam"))
            {
                return;
            }
            // we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
            Health -= 0.1f * Time.deltaTime;
            */
        }

        // This won't be called on my system because it's newer than 5.4
#if !UNITY_5_4_OR_NEWER
        /// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
        void OnLevelWasLoaded(int level)
        {
            this.CalledOnLevelWasLoaded(level);
        }
#endif

        /** My note:
         *   - This function is going to be called when the event UnityEngine.SceneManagement.SceneManager.sceneLoaded is triggered
         *     because we set it up to be called in the Start() function
         */
        void CalledOnLevelWasLoaded(int level)
        {
            /** Note from tutorial:
             *   raycast downwards the current player's position to see 
             *   if we hit anything. If we don't, this is means we are not above the arena's ground and we need to be repositioned back to 
             *   the center, exactly like when we are entering the room for the first time.
             */
            /** My Note:
             *   - I was getting this error when second player left room: 
             *      MissingReferenceException: The object of type 'PlayerManager' has been destroyed but you are still trying to access it.
             *      Your script should either check if it is null or you should not destroy the object.
             *   - Trying to fix that, I check transform is null first. 
             *   - Result: Didn't work! I get the same error on "if(transform != null)" (which makes no sense to me) after this debug log line:
             *      Network destroy Instantiated GO: My Robot Kyle(Clone)
             *   - Trying to comment out the repositioning code completely to see what happens...
             *   - Result: I get this error:
             *      <Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.
             *      UnityEngine.Debug:LogError(Object, Object)
             *      Com.Kabaj.PhotonTutorialProject.PlayerUI:SetTarget(PlayerManager) (at Assets/PlayerUI.cs:130)
             *      UnityEngine.GameObject:SendMessage()
             *      Com.Kabaj.PhotonTutorialProject.PlayerManager:CalledOnLevelWasLoaded(Int32) (at Assets/PlayerManager.cs:262)
             */
            // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }


            /** Note from tutorial:
             *   when a new level is loaded, the UI is being 
             *   destroyed yet our player remains... so we need to instantiate it as well when we know a level was loaded
             *   ...
             *   Note that there are more complex/powerful ways to deal with this and the UI could be made out with a singleton, but it 
             *   would quickly become complex, because other players joining and leaving the room would need to deal with their UI as 
             *   well. In our implementation, this is straight forward, at the cost of a duplication of where we instantiate our UI prefab. As a 
             *   simple exercise, you can create a private method that would instantiate and send the "SetTarget" message, and from the 
             *   various places, call that method instead of duplicating the code.
             */
            if (usingPlayerUIPrefab)
            {
                GameObject _uiGo = Instantiate(this.playerUiPrefab);
                _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
        }

        #endregion

        #region Public Methods

        public void TakeDamage(float amount)
        {
            Health -= amount;
            if (Health <= 0)
            {
                Die();
            }
        }

        #endregion

        #region Private Methods

#if UNITY_5_4_OR_NEWER
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
        {
            this.CalledOnLevelWasLoaded(scene.buildIndex);
        }
#endif

        

        void Die()
        {
            GameManager.Instance.LeaveRoom();
            //PhotonNetwork.Destroy(gameObject);
            //Destroy(gameObject);
        }

        /// <summary>
        /// Processes the inputs. 
        /// (This method should only be called in Update if photonView.IsMine)
        /// </summary>
        void ProcessInputs()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                // we don't want to fire when we interact with UI buttons for example. IsPointerOverGameObject really means IsPointerOver*UI*GameObject
                // notice we don't use on on GetbuttonUp() few lines down, because one can mouse down, move over a UI element and release, which would lead to not lower the isFiring Flag.
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    //Debug.Log("PlayerManager: ProcessInputs() Mouse over UI GameObject -> Do not shoot gun");

                    // Remove cursor lock to enable the Leave Game UI button to be clicked
                    // I had to create a public SetCursorLock method inside FirstPersonController to access the MouseLook.SetCursorLock method
                    // !! This might not have been the best way to handle this problem!
                    GetComponent<FirstPersonController>().SetCursorLock(false);
                    
                    // Return so we don't shoot gun
                    return;
                }

                //Debug.Log("PlayerManager: ProcessInputs() Input.GetButtonDown(\"Fire1\")");
                
                // Call the [PunRPC] Shoot method over photon network
                //photonView.RPC("Shoot", RpcTarget.AllViaServer);

            }
            // Check if the user is trying to fire gun continuously
            if (Input.GetButton("Fire1"))
            {
                //Debug.Log("PlayerManager: ProcessInputs() Input.GetButton(\"Fire1\")");

                // Check if gun is ready to shoot before sending the RPC to avoid overloading network
                if (gun.IsReadyToShoot)
                {
                    //Debug.LogFormat("PlayerManager: ProcessInputs() gun.IsReadyToShoot = {0}", gun.IsReadyToShoot);
                
                    // Call the [PunRPC] Shoot method over photon network
                    photonView.RPC("Shoot", RpcTarget.All);
                }
            }
        }
        
        /// <summary>
        /// Shoot was invoked over photon network via RPC
        /// </summary>
        /// <param name="info">Info about the RPC call such as who sent it, and when it was sent</param>
        [PunRPC]
        void Shoot(PhotonMessageInfo info)
        {
            //Debug.LogFormat("PlayerManager: [PunRPC] Shoot() {0}, {1}, {2}.", info.Sender, info.photonView, info.SentServerTime);
            
            // Tell the gun to shoot
            gun.Shoot();
        }

        #endregion

        #region IPunObservable implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // We own this player: send the others our data
                stream.SendNext(IsFiring);
                stream.SendNext(Health);
            }
            else
            {
                // Network player, receive data
                this.IsFiring = (bool)stream.ReceiveNext();
                this.Health = (float)stream.ReceiveNext();
            }
        }

        #endregion
    }
}