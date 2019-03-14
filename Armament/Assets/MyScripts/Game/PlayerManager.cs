using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using Photon.Pun;
using System;
using Photon.Realtime;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Manages Player information
    /// </summary>
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable, ITarget, IPunInstantiateMagicCallback
    {

        #region Public Fields

        // Key references
        public const string KEY_KILLS = "Kills";
        public const string KEY_DEATHS = "Deaths";
        public const string KEY_ISALIVE = "IsAlive";
        public const string KEY_TEAM = "Team";

        // Team name references
        public const string TEAM_NAME_A = "A";
        public const string TEAM_NAME_B = "B";
        public const string TEAM_NAME_SPECT = "Spectator";

        [Tooltip("The current Health of our player")]
        public float Health = 100f;
        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

        #endregion

        #region Private Serializable Fields

        [Tooltip("Gun owned by player")]
        [SerializeField] private Gun activeGun;
        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField] private GameObject playerUiPrefab;
        [Tooltip("How far to toss weapon in front of player when dropping weapon")]
        [SerializeField] int howFarToTossWeapon = 10;
        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField] private bool usingPlayerUIPrefab = false;

        #endregion Private Serializable Fields

        #region Private Fields

        private const bool DEBUG = true; // indicates whether we are debugging this class
        
        private ArrayList playerWeapons;
        private GameObject gunToBePickedUpGO; // stores a reference to the a gun we want to pick up

        #endregion
        
        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            playerWeapons = new ArrayList();

            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                PlayerManager.LocalPlayerInstance = this.gameObject;
            }
            
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(this.gameObject);

            // Make sure our player doesn't collide with the gun it is holding
            DisableActiveGunCollider();
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
        /// </summary>
        void OnTriggerEnter(Collider other)
        {

            if (photonView.IsMine)
            {
                // If this player collided with a Weapon (a collider on a gameobject with a tag == "Weapon")... 
                if (other.CompareTag("Weapon"))
                {
                    if (DEBUG) Debug.LogFormat("PlayerManager: OnTriggerEnter() Collided with WEAPON with name \"{0}\"," +
                        " photonView.Owner.NickName = {1}", other.GetComponentInParent<Gun>().name, photonView.Owner.NickName);

                    // Save a reference to the gun we want to pick up. We will need it later in OnRoomPropertiesUpdate() 
                    // to actually pick up the gun if we were successful in claiming ownership of the gun in this method
                    gunToBePickedUpGO = other.gameObject;

                    // Get the View ID of the photon view on the Gun GameObject
                    string gunViewID = gunToBePickedUpGO.GetComponentInParent<PhotonView>().ViewID.ToString();

                    // Get the current owner of this gun (it should be "Scene" if no one owns the gun)
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(gunViewID, out object value);
                    string currentOwner = Convert.ToString(value);

                    // Try to set this player as the owner of this gun
                    // ...
                    // Race condition: If another client's player tries to pick up the gun at the same time,
                    // we may not be successful in claiming ownership of this gun. Setting properties will 
                    // fail if we are too late. (This is a good thing!)
                    // ...
                    // After SetCustomProperties is called we will wait for OnRoomPropertiesUpdate() photon callback 
                    // to be called to see if we can actually pick up the gun
                    PhotonNetwork.CurrentRoom.SetCustomProperties(
                        new ExitGames.Client.Photon.Hashtable { { gunViewID, photonView.Owner.NickName } },
                        new ExitGames.Client.Photon.Hashtable { { gunViewID, value } });
                }
            }
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // If we want to pick up a gun...
            if (gunToBePickedUpGO != null)
            {
                // Get the View ID of the photon view on the Gun GameObject
                string gunViewID = gunToBePickedUpGO.GetComponentInParent<PhotonView>().ViewID.ToString();

                // Get the current owner of this gun
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(gunViewID, out object value);
                string currentOwner = (value != null) ? (string)value : ""; // value should never be null but just in case...

                // If we are the current owner of the gun we want to pick up...
                if (currentOwner.Equals(photonView.Owner.NickName))
                {
                    if (DEBUG) Debug.LogFormat("PlayerManager: OnRoomPropertiesUpdate() About to pick up gun: gunViewID = {0}", gunViewID);
                    // Pick up gun (synchronized on network)
                    photonView.RPC("ReplaceCurrentGunWithPickedUpGun", RpcTarget.All, Convert.ToInt32(gunViewID));
                }

                // Whether we were successful or not trying to pick up this gun, we are no longer trying to pick up this gun
                // and don't want to try to pick it up again if the room properties are updated again for some other reason
                gunToBePickedUpGO = null;
            }
        }

#if !UNITY_5_4_OR_NEWER
        /// <summary>
        /// See CalledOnLevelWasLoaded. Outdated in Unity 5.4.
        /// </summary>
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

        /// <summary>
        /// Set what team this player is on. 
        /// This method will first be called by GameManager after player is instantiated on PhotonNetwork.
        /// </summary>
        /// <param name="team"></param>
        public void SetTeam(string team)
        {
            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { {KEY_TEAM, team} });
        }

        /// <summary>
        /// Decreases health of player by damage amount. Makes player die (on all clients), if necessary.
        /// </summary>
        /// <param name="amount">The amount of damage caused</param>
        public void TakeDamage(float amount)
        {
            // Note to self:
            //  Don't forget: Health will be synchronized by Photon via 'Object Synchronization'
            //  Each client will own one player (specifically, a PhotonView component on the player). 
            //  The client's player tells all other clients' instances of the player what their health is.

            // If the player being damaged is the one this client owns...
            if (photonView.IsMine)
            {
                Health -= amount;

                // If player should die...
                if (Health <= 0)
                {
                    // Make player die (synchronized on network)
                    photonView.RPC("Die", RpcTarget.All); // calls the [PunRPC] Die method over photon network
                }
            }
        }

        /// <summary>
        /// Decreases health of player by damage amount. Makes player die (on all clients), if necessary.
        /// Logs a kill (on all clients) for the player who caused the damage. 
        /// This method is typically called from the Gun class when a player gets shot and needs to take damage.
        /// </summary>
        /// <param name="amount">The amount of damage caused</param>
        /// <param name="playerWhoCausedDamage">The player who caused the damage</param>
        public void TakeDamage(float amount, PlayerManager playerWhoCausedDamage)
        {
            // Note to self:
            //  Don't forget: Health will be synchronized by Photon via 'Object Synchronization'
            //  Each client will own one player (specifically, a PhotonView component on the player). 
            //  The client's player tells all other clients' instances of the player what their health is.

            // If the player being damaged is the one this client owns...
            if (photonView.IsMine)
            {
                Health -= amount;

                // If player should die...
                if (Health <= 0)
                {
                    // Make this player die on all clients
                    photonView.RPC("Die", RpcTarget.All); // calls the [PunRPC] Die method over photon network

                    // Log the kill for the player who caused the damage 
                    playerWhoCausedDamage.AddKill();
                }
            }
        }
        
        #endregion Public Methods

        #region Private Methods

#if UNITY_5_4_OR_NEWER
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
        {
            this.CalledOnLevelWasLoaded(scene.buildIndex);
        }
#endif
        
        /// <summary>
        /// Drops the gun.
        /// </summary>
        /// <param name="gun">The gun. Must be gun that is currently being held by a player.</param>
        void DropGun(Gun gun)
        {
            // Relinquish gun ownership to the Scene 
            int viewID = gun.GetComponent<PhotonView>().ViewID;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { viewID.ToString(), "Scene" } });

            // Make this gun a sibling of the player in the GameObject hierarchy
            gun.transform.parent = LocalPlayerInstance.transform.parent;

            // Toss gun away from player so we don't immediately collide with it again
            // (For now, just move it forward a bit)
            gun.transform.position = gun.transform.position + LocalPlayerInstance.transform.forward * howFarToTossWeapon;
            gun.transform.rotation = LocalPlayerInstance.transform.rotation;
            
            // Re-enable the gun and its gun's collider so it's visible and can be picked up again
            gun.transform.gameObject.SetActive(true);
            gun.GetComponentInChildren<BoxCollider>().enabled = true;
        }

        /// <summary>
        /// Disables the active gun collider.
        /// This is important to do whenever we have a new active gun so we don't collide with it.
        /// </summary>
        void DisableActiveGunCollider()
        {
            activeGun.GetComponentInChildren<BoxCollider>().enabled = false;
        }

        /// <summary>
        /// Adds a death for this player. This kill is synced for this player on all clients. 
        /// This method is called by [PunRPC] Die().
        /// </summary>
        void AddDeath()
        {
            // Get current deaths for this player
            photonView.Owner.CustomProperties.TryGetValue(KEY_DEATHS, out object value);
            int deaths = (value == null) ? 0 : Convert.ToInt32(value);

            // Add a death for this player
            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { {KEY_DEATHS, ++deaths} });

            if (DEBUG) Debug.LogFormat("PlayerManager: AddDeath() deaths = {0}, photonView.Owner.NickName = {1}", deaths, photonView.Owner.NickName);
        }

        /// <summary>
        /// Adds a kill for this player. This kill is synced for this player on all clients. 
        /// This method is called by TakeDamage(float,PlayerManager) (an instance of another player) when that player dies.
        /// </summary>
        void AddKill()
        {
            // Get current deaths for this player
            photonView.Owner.CustomProperties.TryGetValue(KEY_KILLS, out object value);
            int kills = (value == null) ? 0 : Convert.ToInt32(value);

            // Add a kill for this player
            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { {KEY_KILLS, ++kills} });

            if (DEBUG) Debug.LogFormat("PlayerManager: AddKill() kills = {0}, photonView.Owner.NickName = {1}", kills, photonView.Owner.NickName);
        }

        /// <summary>
        /// Right now, we just reset the health to 100% and act like nothing happened.
        /// Later, we'll figure out something better to do...
        /// </summary>
        void Respawn()
        {
            if (DEBUG) Debug.LogFormat("PlayerManager: Respawn() photonView.Owner.NickName = {0}", photonView.Owner.NickName);

            if (photonView.IsMine)
            {
                Health = 100;

                // Temporary respawning action: Pretend the player has respawned by raising him in the air a bit
                transform.GetComponent<FirstPersonController>().enabled = false; // disables the first person controller so 
                transform.position = transform.position + new Vector3(0f, 20f, 0f);
                Update();
                transform.GetComponent<FirstPersonController>().enabled = true;
            }
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

            }

            // Check if the user is trying to fire gun continuously
            //Dictionary<int, string> inputsDict = new Dictionary<InputClass, string>();
            //or without a dict we could just have 1 input class with lots of commands. nah we should really divide them up based on input type (mobile, pc, UI for each, etc)
            //inputsDict.get(name).execute(input); //where input is "Fire1" or "Weapon1" and map.get(name) returns a class that can execute that input.
            //we set the state in another place in the code. this type of code is easier to maintain than the long if statements
            if (Input.GetButton("Fire1"))
            {
                // Check if gun is ready to shoot before sending the RPC to avoid overloading network
                if (activeGun.IsReadyToShoot)
                {
                    // Call the [PunRPC] Shoot method over photon network
                    photonView.RPC("Shoot", RpcTarget.All);
                }
            }
        }

        #endregion Private Methods

        #region RPC Methods

        /// <summary>
        /// Picks up gun.
        /// Requires player to have a gun already to know how to position the new gun.
        /// *This is not the best implementation!!
        /// </summary>
        /// <param name="pickedUpGun">The picked up gun.</param>
        [PunRPC]
        void ReplaceCurrentGunWithPickedUpGun(int viewID)
        {
            // Find gun to pick up using Photon viewID
            Gun pickedUpGun = PhotonView.Find(viewID).GetComponent<Gun>();

            // Protect against double collisions (trying to pick up the same gun twice)
            if (pickedUpGun.Equals(activeGun))
            {
                return;
            }
                
            // Make sure we don't collide with the new gun while we're holding it
            pickedUpGun.GetComponentInChildren<BoxCollider>().enabled = false;

            // Put this gun in the GameObject hierarchy where the old gun was (i.e., make it a sibling to the old gun)
            pickedUpGun.transform.parent = activeGun.transform.parent;

            // Give the picked up gun the same position and rotation as the active gun
            pickedUpGun.transform.position = activeGun.transform.position;
            pickedUpGun.transform.rotation = activeGun.transform.rotation;
            
            // Disable the old gun and enable new gun
            activeGun.transform.gameObject.SetActive(false);
            pickedUpGun.transform.gameObject.SetActive(true);
            
            // Set FPS Cam and Player who owns this gun
            pickedUpGun.fpsCam = activeGun.fpsCam;
            pickedUpGun.playerWhoOwnsThisGun = activeGun.playerWhoOwnsThisGun;

            // Make the picked up gun our active gun and Drop the old gun
            Gun oldGun = activeGun;
            activeGun = pickedUpGun;
            DropGun(oldGun);
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
            activeGun.Shoot();
        }

        /// <summary>
        /// Handles what happens to this player when it dies.
        /// Right now, we register that this player has died and "respawn" the player
        /// </summary>
        [PunRPC]
        void Die()
        {
            //GameManager.Instance.LeaveRoom();
            //PhotonNetwork.Destroy(gameObject);
            //Destroy(gameObject);

            // Register a death for this player on all client
            AddDeath();

            // Respawn
            Respawn();
        }

        #endregion RPC Methods

        #region IPunObservable implementation

        /// <summary>
        /// Handles custom synchronization of information over the network.
        /// <para>
        /// This method will be called in scripts that are assigned as Observed component of a PhotonView.
        /// PhotonNetwork.SerializationRate affects how often this method is called.
        /// PhotonNetwork.SendRate affects how often packages are sent by this client.
        /// </para>
        /// <para>
        /// Implementing this method, you can customize which data a PhotonView regularly synchronizes. 
        /// Your code defines what is being sent (content) and how your data is used by receiving clients.
        /// </para>
        /// <para>
        /// Unlike other callbacks, OnPhotonSerializeView only gets called when it is assigned to a PhotonView as 
        /// PhotonView.observed script.
        /// </para>
        /// <para>
        /// To make use of this method, the PhotonStream is essential. 
        /// It will be in "writing" mode" on the client that controls a PhotonView (PhotonStream.IsWriting == true) 
        /// and in "reading mode" on the remote clients that just receive that the controlling client sends.
        /// </para>
        /// <para>
        /// If you skip writing any value into the stream, PUN will skip the update. Used carefully, 
        /// this can conserve bandwidth and messages (which have a limit per room/second).
        /// </para>
        /// <para>
        /// Note that OnPhotonSerializeView is not called on remote clients when the sender does not send any update. This can't be used as "x-times per second Update()".
        /// </para>
        /// <para>
        /// Implements IPunObservable.
        /// </para>
        /// </summary>
        /// <param name="stream">Stream on which to read or write custom data.</param>
        /// <param name="info">Information about the message (like who sent it).</param>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // If this client owns this player (specifically, the PhotonView component on this player)...
            if (stream.IsWriting)
            {
                // We own this player: send the others our data
                stream.SendNext(Health);
            }
            // If this client doesn't own this player (specifically, the PhotonView component on this player)...
            else
            {
                // Network player, receive data
                this.Health = (float)stream.ReceiveNext();
            }
        }

        #endregion IPunObservable implementation

        #region IPunInstantiateMagicCallback implementation

        /// <summary>
        /// Photon Callback method. Called after player has been instantiated on network. Used to set up player properties.
        /// </summary>
        /// <param name="info"></param>
        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
        }

        #endregion IPunInstantiateMagicCallback implementation
    }
}