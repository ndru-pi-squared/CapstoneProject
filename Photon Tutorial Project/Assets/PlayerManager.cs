using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using Photon.Pun;

namespace Com.Kabaj.PhotonTutorialProject
{
    /// <summary>
    /// Player manager.
    /// Handles fire Input and Beams.
    /// </summary>
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {

        // Note: I changed this from "Private Fields" to "Public Fields" because it didn't make any sense to me
        #region Public Fields


        [Tooltip("The current Health of our player")]
        public float Health = 1f;
        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;
        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField]
        public GameObject PlayerUiPrefab;


        #endregion


        #region Private Fields


        [Tooltip("The Beams GameObject to control")]
        [SerializeField]
        private GameObject beams;
        //True, when the user is firing
        bool IsFiring;


        #endregion

        #region MonoBehaviour CallBacks


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            if (beams == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> Beams Reference.", this);
            }
            else
            {
                beams.SetActive(false);
            }

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
            // Note from tutorial:
            // First, it gets the CameraWork component, we expect this, so if we don't find it, we log an error.
            CameraWork _cameraWork = this.gameObject.GetComponent<CameraWork>();


            if (_cameraWork != null)
            {
                /** Note from tutorial: 
                 * if photonView.IsMine is true, it means we need to follow this instance, and so we call _cameraWork.OnStartFollowing() 
                 * which effectivly makes the camera follow that very instance in the scene. All other player instances will have their 
                 * photonView.IsMine set as false, and so their respective _cameraWork will do nothing.
                 */
                if (photonView.IsMine)
                {
                    // Note from tutorial:
                    // makes the camera follow that very instance in the scene.
                    _cameraWork.OnStartFollowing();
                }
            }
            else
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> CameraWork Component on playerPrefab.", this);
            }

            /** Notes from tutorial:
             * There is currently an added complexity because Unity has revamped "Scene Management" and Unity 5.4 has deprecated 
             * some callbacks, which makes it slightly more complex to create a code that works across all Unity versions (from Unity 
             * 5.3.7 to the latest). So we'll need different code based on the Unity version. It's unrelated to Photon Unity Networking, but 
             * important to master for your projects to survive updates.
             */
#if UNITY_5_4_OR_NEWER
            // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, loadingMode) => // I'm guessing this creates an anonymous function with parameters scene and loadingMode
            {
                this.CalledOnLevelWasLoaded(scene.buildIndex);
            };
#endif

            /** Notes from tutorial:
             * All of this is standard Unity coding. However notice that we are sending a message to the instance we've just created. We 
             * require a receiver, which means we will be alerted if the SetTarget did not find a component to respond to it. Another 
             * way would have been to get the PlayerUI component from the instance, and then call SetTarget directly. It's generally 
             * recommended to use components directly, but it's also good to know you can achieve the same thing in various ways.
             */
            if (PlayerUiPrefab != null)
            {
                // I added  if (photonView.IsMine) to fix a bug where the local player would see both players' UIs
                // This code is also CalledOnLevelWasLoaded()
                //if (photonView.IsMine)
                //{
                    GameObject _uiGo = Instantiate(PlayerUiPrefab);
                    _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
                //}
            }
            else
                {
                    Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
                }

        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// </summary>
        void Update()
        {
            if (photonView.IsMine)
            {
                ProcessInputs();
            }

            if (Health <= 0f)
            {
                GameManager.Instance.LeaveRoom();
            }

            // trigger Beams active state
            if (beams != null && IsFiring != beams.activeSelf)
            {
                beams.SetActive(IsFiring);
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
            Health -= 0.1f;
        }
       
        /// <summary>
        /// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
        /// We're going to affect health while the beams are touching the player
        /// </summary>
        /// <param name="other">Other.</param>
        void OnTriggerStay(Collider other)
        {
            // we dont' do anything if we are not the local player.
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
        }

        // This won't be called on my system because it's newer than 5.4
 #if !UNITY_5_4_OR_NEWER
        /// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
        void OnLevelWasLoaded(int level)
        {
            this.CalledOnLevelWasLoaded(level);
        }
#endif
        

        void CalledOnLevelWasLoaded(int level)
        {
            /** Note from tutorial:
             *   raycast downwards the current player's position to see 
             *   if we hit anything. If we don't, this is means we are not above the arena's ground and we need to be repositioned back to 
             *   the center, exactly like when we are entering the room for the first time.
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
            // I added  if (photonView.IsMine) to fix a bug where the local player would see both players' UIs
            // This code is also Start()
            //if (photonView.IsMine) 
            //{
                GameObject _uiGo = Instantiate(this.PlayerUiPrefab);
                _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            //}
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


        #region Custom


        /// <summary>
        /// Processes the inputs. Maintain a flag representing when the user is pressing Fire.
        /// </summary>
        void ProcessInputs()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (!IsFiring)
                {
                    IsFiring = true;
                }
            }
            if (Input.GetButtonUp("Fire1"))
            {
                if (IsFiring)
                {
                    IsFiring = false;
                }
            }
        }


        #endregion
    }
}