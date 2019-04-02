using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using Photon.Pun;
using System;
using Photon.Realtime;
using System.Collections.Generic;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>
    /// Manages Player information
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable, ITarget, IPunInstantiateMagicCallback
    {

        #region Public static and const Fields 

        // Key references for the Player CustomProperties hash table (so we don't use messy string literals)
        public const string KEY_KILLS = "Kills";
        public const string KEY_DEATHS = "Deaths";
        public const string KEY_MODE = "Mode"; 
        public const string KEY_TEAM = "Team";
        public const string KEY_ACTIVE_GUN = "Active Gun";

        // Value references for the Player CustomProperties hash table (so we don't use messy string literals)
        public const string VALUE_TEAM_NAME_A = "A";
        public const string VALUE_TEAM_NAME_B = "B";
        public const string VALUE_TEAM_NAME_SPECT = "Spectator";
        public const string VALUE_MODE_ALIVE = "Alive";
        public const string VALUE_MODE_DEAD_SPECT = "Dead_Spectator";

        public const int MAX_HEALTH = 100;
        public const int MAX_SHIELD = 100;

        public static GameObject LocalPlayerInstance;

        #endregion Public static and const Fields 

        #region Public Fields

        [Tooltip("The current Health of our player")]
        public float Health = 100f;

        [Tooltip("The current Health of our player")]
        public float Shield = 100f;

        [Tooltip("How many shield points to regenerate per second")]
        public float ShieldRegenerationRate = 5f;

        #endregion

        #region Private Serializable Fields

        [Tooltip("Audio Clip to play when a player dies.")]
        [SerializeField] private AudioClip deathSound;
        [Tooltip("Audio Clip to play when a player takes.")]
        [SerializeField] private AudioClip painSound;
        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField] private GameObject playerUiPrefab; // from tutorial.. probably should remove
        [Tooltip("How far to toss weapon in front of player when dropping weapon")]
        [SerializeField] int howFarToTossWeapon = 10;
        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField] private bool usingPlayerUIPrefab = false; // needed to disable tutorial code... probably should removed with playerUiPrefab

        [Tooltip("")]
        [SerializeField] private GameObject playerData;

        #endregion Private Serializable Fields

        #region Private Fields

        // Debug flags
        private const bool DEBUG = true; // indicates whether we are debugging this class
        private const bool DEBUG_Awake = false;
        private const bool DEBUG_Start = false;
        private const bool DEBUG_OnTriggerEnter = false;
        private const bool DEBUG_OnRoomPropertiesUpdate = false;
        private const bool DEBUG_OnPlayerPropertiesUpdate = false;
        private const bool DEBUG_MovePlayer = true;
        private const bool DEBUG_Respawn = true;
        private const bool DEBUG_SetActiveGun = false;
        private const bool DEBUG_DropGun = false;
        private const bool DEBUG_SwapGun = false;
        private const bool DEBUG_OnPhotonInstantiate = false;
        private const bool DEBUG_ProcessInputs = false;
        private const bool DEBUG_SetAvatar = false;
        private const bool DEBUG_TakeDamage = false;
        private const bool DEBUG_SetMode = true; 

        private AudioSource audioSource;

        private ArrayList playerWeapons;
        private GameObject gunToBePickedUpGO; // stores a reference to the a gun we want to pick up
        private Vector3 activeGunPosition;
        
        private int activeGunType; // keeps track of currently active gun's type 
        private int previousActiveGunType; // keeps track of previously active gun's type
        private Gun activeGun; // keeps track of active gun the active gun: a gun that is synced on network
        private Gun activeShowGun; // keeps track of the active "show" gun: a gun that is not synced on network (instantiated locally) and used to visually and functionally represent activeGun
        private object[] instantiationData; // information that was linked with this Gun's GO when it was instantiated by the master client
        private bool selectingWeapon; // flag to keep track of whether user is trying to select a weapon
        private int weaponSelectionIndex; 
        
        private GameObject playerGO;
        private GameObject unityChanPrefab;
        private GameObject kyleRobotPrefab;
        private Animator animator;
        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            if (DEBUG && DEBUG_Awake) Debug.LogFormat("PlayerManager: Awake()");
            playerWeapons = new ArrayList();

            // Get the instantiation data set by the master client who instantiated this player game object on the network
            // The master client will have provided the photon player actor number who this player GO was intended for
            // If we are that actor, lets claim ownership of the photonview on the player GO.
            instantiationData = GetComponent<PhotonView>().InstantiationData;
            int actorNumber = (int)instantiationData[0];

            if (DEBUG && DEBUG_Awake) Debug.LogFormat("PlayerManager: Awake() actorNumber = {0}, PhotonNetwork.LocalPlayer.ActorNumber = {1}", actorNumber, PhotonNetwork.LocalPlayer.ActorNumber);

            if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber) {
                //if (!photonView.IsMine)
                //{
                    if (DEBUG && DEBUG_Awake) Debug.LogFormat("PlayerManager: Awake() Transferring ownership to PhotonNetwork.LocalPlayer = {0}", PhotonNetwork.LocalPlayer);
                    GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer);
                //}
                
                // #Important
                // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
                if (DEBUG && DEBUG_Awake) Debug.LogFormat("PlayerManager: Awake() Setting PlayerManager.LocalPlayerInstance = {0}", gameObject);
                LocalPlayerInstance = gameObject;

                // Tell player what team they are on
                GetComponent<PlayerManager>().SetTeam((string)instantiationData[1]);

                // We need to enable all the controlling components for the local player 
                // The prefab has these components disabled so we won't be controlling other players with our input
                GetComponent<Animator>().enabled = true;
                GetComponent<CharacterController>().enabled = true;
                GetComponent<AudioSource>().enabled = true;
                GetComponent<FirstPersonController>().enabled = true;
                GetComponent<PlayerAnimatorManager>().enabled = true;
                GetComponent<PlayerManager>().enabled = true;
                GetComponentInChildren<Camera>().enabled = true;
                GetComponentInChildren<AudioListener>().enabled = true;
                GetComponentInChildren<FlareLayer>().enabled = true;

                string avatarChoice = playerData.GetComponent<PlayerData>().GetAvatarChoice();
                Debug.LogFormat("PlayerManager: Awake() avatarChoice = {0}", avatarChoice);
                //photonView.RPC("SetAvatar", RpcTarget.AllBuffered, avatarChoice);

                // Disable scene cameras; we'll use player's first-person camera now
                foreach (Camera cam in GameManager.Instance.sceneCameras)
                    cam.enabled = false;

                // Find the PlayerInfoUI (which displays things like player health and player name) in the canvas
                GameObject playerInfoUIGO = GameManager.Instance.canvas.GetComponentInChildren<PlayerInfoUI>().gameObject;
                // Call SetTarget() on PlayerInfoUI component so the PlayerInfoUI will follow be linked to this player 
                playerInfoUIGO.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
            
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(this.gameObject);

            // *** Shouldn't need this code anymore because player doesn't spawn with a gun
            // *** \/
            // Make sure our player doesn't collide with the gun it is holding
            DisableActiveGunCollider();
        }

        [PunRPC]
        public void SetAvatar(string avatarChoice)
        {
            
            /*Debug.LogFormat("PlayerManager: SetAvatar() avatarChoice = {0}, avatarChoice.Equals(\"KyleRobot\") = {1}", avatarChoice, avatarChoice.Equals("KyleRobot "));
            // If user chooses kyle...
            //if (playerData.GetComponent<PlayerData>().GetAvatarChoice().Equals("KyleRobot"))
            if (avatarChoice.Equals("KyleRobot"))
            {
                // TODO: Deactivate unitychan
                if (DEBUG && DEBUG_SetAvatar) Debug.LogFormat("PlayerManager: SetAvatar() DEACTIVATING UNITYCHAN");
                transform.Find("Model/unitychan").gameObject.SetActive(false);
            }
            // If user chooses kyle...
            else
            {
                if (DEBUG && DEBUG_SetAvatar) Debug.LogFormat("PlayerManager: SetAvatar() ACTIVATING UNITYCHAN");

                // G
                this.gameObject.GetComponent<Animator>().runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("Animation/UnityChanLocomotions");


                Transform unitychanTransform = transform.Find("Model/unitychan");

                if (DEBUG && DEBUG_SetAvatar) Debug.LogFormat("PlayerManager: SetAvatar() unitychanTransform.name = {0}", unitychanTransform.name);

                if (unitychanTransform != null)
                    this.gameObject.GetComponent<Animator>().avatar = unitychanTransform.GetComponent<Animator>().avatar;

                // Deactivate kyle
                if (DEBUG && DEBUG_SetAvatar) Debug.LogFormat("PlayerManager: SetAvatar() DEACTIVATING KYLE");
                Transform robotModelTransform = transform.Find("Model/Robot2");
                robotModelTransform.GetComponent<SkinnedMeshRenderer>().enabled = false;
            }
            //if (photonView.IsMine)
            {
            }*/
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            if (DEBUG && DEBUG_Start) Debug.LogFormat("PlayerManager: Start() ");
            //Debug.Log("Start(): Avatar choice coming from Launcher scene into PlayerData: " + PlayerData.GetComponent<PlayerData>().GetAvatarChoice());
 
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
            {
                Debug.LogError("Player is Missing Audio Source Component", this);
            }
            audioSource.enabled = true;

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
            
            // Regenerate Shield
            if (Shield < MAX_SHIELD)
            {

                //Shield = Mathf.Lerp(Shield, MAX_SHIELD, ShieldRegenerationRate/MAX_SHIELD * Time.deltaTime);
                Shield += ShieldRegenerationRate * Time.deltaTime;
            }
        }

        /// <summary>
        /// MonoBehaviour method called when the Collider 'other' enters the trigger.
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            // If this client controls this player...
            if (photonView.IsMine)
            {
                // If this player collided with a Weapon... 
                if (other.CompareTag("Weapon"))
                {
                    if (DEBUG && DEBUG_OnTriggerEnter) Debug.LogFormat("PlayerManager: OnTriggerEnter() Collided with WEAPON with name \"{0}\"," +
                        " photonView.Owner.NickName = {1}", other.GetComponentInParent<Gun>().name, photonView.Owner.NickName);

                    // Save a reference to the gun we want to pick up. We will need it later in OnRoomPropertiesUpdate() 
                    // to actually pick up the gun if we were successful in claiming ownership of the gun in this method
                    gunToBePickedUpGO = other.gameObject;

                    // Get the View ID of the photon view on the Gun GameObject
                    string gunViewID = gunToBePickedUpGO.GetComponentInParent<PhotonView>().ViewID.ToString();

                    // Get the current owner of this gun (it should be GameManager.VALUE_UNCLAIMED_ITEM if no one owns the gun)
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(gunViewID, out object value);
                    string currentOwner = Convert.ToString(value);

                    // Try to claim ownership of the gun in the CurrentRoom.CustomProperties table
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
         *   - This method is going to be called when the event UnityEngine.SceneManagement.SceneManager.sceneLoaded is triggered
         *     because we set it up to be called in the Start() method
         *   - This current contents of this method (Raycasting downwards and instantiating playerUIPrefab) are holdovers from the tutorial
         *     and don't really apply to Armament game. They should be carefully removed at some point. Don't want to remove the entire method 
         *     (at least not yet) because setting it up to be called was a little tricky and I don't want to have to figure out how to do that
         *     a second time.
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

        #region MonobehaviourPun (Photon) Callbacks

        /// <summary>
        /// Called when a room's custom properties changed. The propertiesThatChanged contains all that was set via Room.SetCustomProperties.
        /// <para> </para>
        /// <para>Since v1.25 this method has one parameter: Hashtable propertiesThatChanged.</para>
        /// <para>Changing properties must be done by Room.SetCustomProperties, which causes this callback locally, too.</para>
        /// </summary>
        /// <param name="propertiesThatChanged"></param>
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // ***
            // This code did not make use of propertiesThatChanged... 
            // It could probably be cleaned up!
            // ***

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
                    if (DEBUG && DEBUG_OnRoomPropertiesUpdate) Debug.LogFormat("PlayerManager: OnRoomPropertiesUpdate() About to pick up gun: gunViewID = {0}", gunViewID);
                    
                    // If we go to this point, we have collided with an unclaimed gun and we have successfully claimed ownership of the gun
                    // in CurrentRoom.CustomProperties. We now want the player (on all clients) to pick up the gun and make it the active gun.
                    // 
                    // To accomplish this, the client that owns this player will set the KEY_ACTIVE_GUN property with the gunViewID. 
                    // As a result, OnPlayerPropertiesUpdate() will be called on all clients. From there, we can finally make the player 
                    // (on all clients) pick up the gun and set (Gun)activeGun 
                    // 
                    // If this client owns this player...
                    if (photonView.IsMine)
                    {
                        // Set local player's active gun property (synced on the network)
                        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_ACTIVE_GUN, gunViewID } });
                    }

                }

                // Whether we were successful or not trying to pick up this gun, we are no longer trying to pick up this gun
                // and don't want to try to pick it up again if the room properties are updated again for some other reason
                gunToBePickedUpGO = null;
            }
        }

        /// <summary>
        /// Called if Player.CustomProperties has changed.
        /// </summary>
        /// <param name="target">The player whose properties have changed</param>
        /// <param name="changedProps">The properties that have just changed. If a property has been removed the key will be in changedProps but the value will be null</param>
        public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (DEBUG && DEBUG_OnPlayerPropertiesUpdate) Debug.LogFormat("PlayerManager: OnPlayerPropertiesUpdate() target.NickName = {0}", target.NickName);

            // I believe this method will execute on all players on all clients when a player's CustomProperties are changed. 
            // Currently, we only want the players whose properties have just changed to act so we will check if the player
            // executing this method is the intended target and return if not. 

            // If this player is not the player who's properties were changed... (I'm 90% sure this code does what I think it does...)
            /*if ((GameObject)target.TagObject != gameObject)
            {
                return;
            }*/

            PlayerManager targetPlayerManager = ((GameObject)target.TagObject).GetComponent<PlayerManager>();

            // Go through the list of keys for player properties that were changed...
            foreach (object key in changedProps.Keys)
            {
                // Get the value for the key
                changedProps.TryGetValue(key, out object value);
                
                // If the player property that changed was KEY_ACTIVE_GUN...
                if (KEY_ACTIVE_GUN.Equals((string)key))
                {   
                    // If the KEY_ACTIVE_GUN value was just removed... 
                    if (value == null)
                    {
                        // Drop active gun
                        targetPlayerManager.DropActiveGun();
                    }
                    else
                    {
                        int gunViewID = Convert.ToInt32((string)value);


                        // At this point, all clients know this player owns this gun.
                        // This gun is ready to be visibly "picked up" by this player on all clients
                        // ...
                        // Pick up the gun (it will remain inactive until we explicitly activate it)
                        targetPlayerManager.PickUpGun(gunViewID);

                        // Set this player's (Gun)activeGun reference (so we can use it to do things like shoot the gun)
                        targetPlayerManager.SetActiveGun(gunViewID);
                    }
                }
            }
        }

        #endregion MonobehaviourPun (Photon) Callbacks

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

        public string GetTeam()
        {
            photonView.Owner.CustomProperties.TryGetValue(KEY_TEAM, out object team);
            return (string)team;
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
                if (GameManager.Instance.FriendlyFire == false && playerWhoCausedDamage.GetTeam().Equals(GetTeam()))
                {
                    if (DEBUG && DEBUG_TakeDamage) Debug.Log("PlayerManager: TakeDamage() Players are on same team. Not logging damage.");
                    return;
                }

                if (Shield > 0)
                {
                    // Player's Shield takes damage equal to amount of damage inflicted
                    Shield -= amount;
                    // Player's health takes damage proportional to the amount of shield they have left
                    Health -= (1 - Shield/MAX_SHIELD) * amount;
                }
                else
                {
                    Health -= amount;
                }

                // If player should die...
                if (Health <= 0)
                {
                    // Make this player die on all clients
                    photonView.RPC("Die", RpcTarget.All); // calls the [PunRPC] Die method over photon network

                    // Log the kill for the player who caused the damage 
                    playerWhoCausedDamage.AddKill();
                }
                else
                {
                    // Play pain sound
                    audioSource.PlayOneShot(painSound); // I read somewhere online that this allows the sounds to overlap
                }
            }
        }

        /// <summary>
        /// Resets health
        /// </summary>
        public void ResetHealth()
        {
            Health = MAX_HEALTH;
        }

        /// <summary>
        /// Resets health
        /// </summary>
        public void ResetShield()
        {
            Shield = MAX_SHIELD;
        }

        public void MovePlayer(Transform t)
        {
            if (DEBUG && DEBUG_MovePlayer) Debug.LogFormat("PlayerManager: MovePlayer() t.position = {0}", t.position);

            /*
            transform.GetComponent<FirstPersonController>().enabled = false; // disables the first person controller so we can manually move the player's position
            //transform.position = t.position;
            transform.localRotation = t.rotation;
            GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(Vector3.zero);
            Update();
            transform.GetComponent<FirstPersonController>().enabled = true;
            */

            // Move the player to the target position
            GetComponent<CharacterController>().Move(t.position - transform.position);

            if (DEBUG && DEBUG_MovePlayer) Debug.LogFormat("PlayerManager: MovePlayer() transform.position = {0}", transform.position);
        }

        /// <summary>
        /// Right now, we just reset the health to 100% and act like nothing happened.
        /// Later, we'll figure out something better to do...
        /// </summary>
        public void Respawn(Transform playerSpawnPoint)
        {
            if (DEBUG && DEBUG_Respawn) Debug.LogFormat("PlayerManager: Respawn() photonView.Owner.NickName = {0}", photonView.Owner.NickName);

            if (photonView.IsMine)
            {
                ResetHealth();
                ResetShield();

                /*
                // If KEY_MODE is set in player's CustomProperties...
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(KEY_MODE, out object modeValueProp))
                {
                    // If player is dead and spectating...
                    if (VALUE_MODE_DEAD_SPECT.Equals((string)modeValueProp))
                    {
                        // Undo the changes we made to the player's GO in StartDeadSpectatorMode()
                        StopDeadSpectatorMode();
                    }
                }
                */

                /*
                // Temporary respawning action: Pretend the player has respawned by raising him in the air a bit
                transform.GetComponent<FirstPersonController>().enabled = false; // disables the first person controller so we can manually move the player's position
                transform.position = transform.position + new Vector3(0f, 20f, 0f);
                Update();
                transform.GetComponent<FirstPersonController>().enabled = true;*/
                
                // Move player to spawn point
                MovePlayer(playerSpawnPoint);
            }
        }
        
        public void StartDeadSpectatorMode()
        {
            // Sync player's mode (whether they are alive or dead and spectating)
            SetMode(VALUE_MODE_DEAD_SPECT);

            // If we control this player...
            if (photonView.IsMine)
            {
                // Enable scene cameras
                foreach (Camera cam in GameManager.Instance.sceneCameras)
                    cam.enabled = true;

                // Disable all the controlling components for this player 
                // The prefab has these components disabled so we won't be controlling other players with our input
                GetComponent<Animator>().enabled = false;
                GetComponent<CharacterController>().enabled = false;
                GetComponent<AudioSource>().enabled = false;
                GetComponent<FirstPersonController>().enabled = false;
                GetComponent<PlayerAnimatorManager>().enabled = false;
                //GetComponent<PlayerManager>().enabled = false;
                GetComponentInChildren<Camera>().enabled = false;
                GetComponentInChildren<AudioListener>().enabled = false;
                GetComponentInChildren<FlareLayer>().enabled = false;
            }

            // Make player invisible by disabling their model
            transform.Find("Model").gameObject.SetActive(false);
        }

        public void StopDeadSpectatorMode()
        {
            // Sync player's mode (whether they are alive or dead and spectating)
            SetMode(VALUE_MODE_ALIVE);

            if (photonView.IsMine)
            {
                // Enable scene cameras
                foreach (Camera cam in GameManager.Instance.sceneCameras)
                    cam.enabled = false;

                // Enable all the controlling components for the local player 
                GetComponent<Animator>().enabled = true;
                GetComponent<CharacterController>().enabled = true;
                GetComponent<AudioSource>().enabled = true;
                GetComponent<FirstPersonController>().enabled = true;
                GetComponent<PlayerAnimatorManager>().enabled = true;
                //GetComponent<PlayerManager>().enabled = true;
                GetComponentInChildren<Camera>().enabled = true;
                GetComponentInChildren<AudioListener>().enabled = true;
                GetComponentInChildren<FlareLayer>().enabled = true;
            }

            // Make player visible by enabling their model
            transform.Find("Model").gameObject.SetActive(true);
        }

        public void SetActiveGun(int gunViewID)
        {
            if (DEBUG && DEBUG_SetActiveGun) Debug.LogFormat("PlayerManager: SetActiveGun() gunViewID = {0}", gunViewID);

            // If there is a currently an active gun...
            if (activeGun != null)
            {
                // Inactivate activeGun
                activeGun.gameObject.SetActive(false);
                activeGun.transform.parent = transform.Find("FirstPersonCharacter/Inactive Weapons");
            }

            // Find Gun to be activated using Photon viewID
            Gun gunToBeActivated = PhotonView.Find(gunViewID).GetComponent<Gun>();

            // Activate gunToBeActivated
            gunToBeActivated.transform.parent = transform.Find("FirstPersonCharacter/Active Weapon");

            // Set activeGun so we can make use of it other places (like shooting it)
            activeGun = gunToBeActivated;

            // If there is a currently an active "show" gun...
            if (activeShowGun != null)
            {
                if (DEBUG && DEBUG_SetActiveGun) Debug.LogFormat("PlayManager: SetActiveGun() Destroying activeShowGun = {0}", activeShowGun);
                // Destroy activeGunFake
                Destroy(activeShowGun.gameObject);
                activeShowGun = null;
            }

            // Keep track of previous active gun type 
            // by recording current active gun type before setting new active gun
            previousActiveGunType = activeGunType;

            // Set active gun type
            // *** Will need to change how we instantiate based on type of gun
            activeGunType = gunToBeActivated.name.Contains("Gun 1") ? 1 : 2;

            // Instantiate show Gun based on type of gun
            // *** Will need to change how we instantiate based on type of gun
            GameObject gunPrefab = activeGunType == 1 ? GameManager.Instance.weaponsPrefabs[0] : GameManager.Instance.weaponsPrefabs[1];
            GameObject showGunToBeActivatedGO = Instantiate(gunPrefab, transform.Find("FirstPersonCharacter/Show Weapon"));

            Gun showGunToBeActivated = showGunToBeActivatedGO.GetComponent<Gun>();
            showGunToBeActivated.GetComponentInChildren<Collider>().enabled = false;

            // Set Gun's FPS Cam and Player who owns this gun
            showGunToBeActivated.fpsCam = photonView.gameObject.transform.Find("FirstPersonCharacter").GetComponent<Camera>();
            showGunToBeActivated.playerWhoOwnsThisGun = photonView.gameObject.GetComponent<MonoBehaviourPun>();

            // Activate gunToBeActivated
            showGunToBeActivated.gameObject.SetActive(true);

            // Set activeGun so we can make use of it other places (like shooting it)
            activeShowGun = showGunToBeActivated;

            UpdateWeaponsMenu();

            if (DEBUG && DEBUG_SetActiveGun) Debug.LogFormat("PlayerManager: SetActiveGun() activeShowGun = {0}", activeShowGun);
        }

        /// <summary>
        /// Picks up Gun. Makes Gun's GameObject a child of Player's "Inactive Weapons" GameObject 
        /// </summary>
        /// <param name="gunViewID">Photon View ID of the gun to pick up</param>
        public void PickUpGun(int gunViewID)
        {
            // Find Gun to pick up using Photon viewID
            Gun pickedUpGun = PhotonView.Find(gunViewID).GetComponent<Gun>();


            // Make sure we don't pickup gun of same type
            //if (pickedUpGun.gunPrefab)


            // Protect against double collisions (trying to pick up the same gun twice)
            // *** This check might not be necessary after recent code changes... TODO: look into it
            if (pickedUpGun.Equals(activeGun))
            {
                return;
            }

            // Make sure we don't collide with the new gun while we're holding it
            pickedUpGun.GetComponentInChildren<BoxCollider>().enabled = false;

            // If this client owns the player picking up the gun...
            if (photonView.IsMine)
            {
                // Transfer ownership of this gun's photonview (and GameObject) to this player
                pickedUpGun.GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer);
            }

            // Put this gun in the GameObject hierarchy where the old gun was (i.e., make it a sibling to the old gun)
            pickedUpGun.transform.parent = transform.Find("FirstPersonCharacter/Inactive Weapons");

            // Set the local position and rotation of the gun
            pickedUpGun.transform.localPosition = new Vector3(-0.06f, 0.216f, -0.496f);
            pickedUpGun.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            // Make sure the picked up gun is not visible
            pickedUpGun.gameObject.SetActive(false);

            // Set Gun's FPS Cam and Player who owns this gun
            pickedUpGun.fpsCam = photonView.gameObject.transform.Find("FirstPersonCharacter").GetComponent<Camera>();
            pickedUpGun.playerWhoOwnsThisGun = photonView.gameObject.GetComponent<MonoBehaviourPun>();

            UpdateWeaponsMenu();

        }

        #endregion Public Methods

        #region Private Methods

#if UNITY_5_4_OR_NEWER
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
        {
            this.CalledOnLevelWasLoaded(scene.buildIndex);
        }
#endif

        void UpdateWeaponsMenu()
        {
            Debug.LogFormat("PlayerManager: UpdateWeaponsMenu()");
            Transform t = GameManager.Instance.canvas.transform.Find("Weapon Inventory Menu");

            t.GetComponent<WeaponsMenuManager>().UpdateWeaponInventoryMenu();
        }

        /// <summary>
        /// Drops the gun.
        /// </summary>
        /// <param name="gun">The gun. Must be gun that is currently being held by a player.</param>
        void DropGun(Gun gun)
        {
            // Do nothing if there is no gun to drop
            if (gun == null)
            {
                return;
            }

            // Relinquish gun ownership
            int viewID = gun.GetComponent<PhotonView>().ViewID;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { viewID.ToString(), GameManager.VALUE_UNCLAIMED_ITEM } });

            // Make this gun a sibling of the player in the GameObject hierarchy
            gun.transform.parent = LocalPlayerInstance.transform.parent;

            // Make gun visible
            gun.transform.gameObject.SetActive(true);

            // Re-enable Photon's syncing of the gun position
            gun.GetComponent<PhotonView>().enabled = true;
            gun.GetComponent<PhotonView>().Synchronization = ViewSynchronization.ReliableDeltaCompressed;

            // Toss gun away from player so we don't immediately collide with it again
            // (For now, just move it forward a bit)
            gun.transform.position = gun.transform.position + photonView.gameObject.transform.forward * howFarToTossWeapon;
            gun.transform.rotation = photonView.gameObject.transform.rotation;

            // Re-enable its gun's collider so it's visible and can be picked up again
            gun.GetComponentInChildren<BoxCollider>().enabled = true;

            // If this client owns the player dropping the gun...
            if (photonView.IsMine)
            {                
                // Transfer Gun's PhotonView ownership to the "Scene"
                gun.GetComponent<PhotonView>().TransferOwnership(0);
            }

            // If we just dropped our active gun...
            if (gun == activeGun)
            {
                activeGun = null;

                // If there is a currently an active "show" gun...
                if (activeShowGun != null)
                {
                    if (DEBUG && DEBUG_DropGun) Debug.LogFormat("PlayManager: DropGun() Destroying activeShowGun = {0}", activeShowGun);
                    // Destroy activeGunFake
                    Destroy(activeShowGun.gameObject);
                    activeShowGun = null;
                }

                Transform trannyWannyDooDa = transform.Find("FirstPersonCharacter/Inactive Weapons");

                if (DEBUG && DEBUG_DropGun)
                {
                    for (int i = 0; i < trannyWannyDooDa.childCount; i++)
                    {
                        Transform childTransform = trannyWannyDooDa.GetChild(i);
                        Debug.LogFormat("PlayerManager: DropGun() child #{1}: childTransform.gameObject = {0}", childTransform.gameObject, i);
                    }
                }

                // Automatically select next gun in Player's inventory to be new active gun 
                if (trannyWannyDooDa.childCount > 0)
                {
                    int gunViewID = trannyWannyDooDa.GetChild(0).GetComponent<PhotonView>().ViewID;
                    SetActiveGun(gunViewID);
                }
            }

            UpdateWeaponsMenu();
        }

        /// <summary>
        /// Disables the active gun collider.
        /// This is important to do whenever we have a new active gun so we don't collide with it.
        /// </summary>
        void DisableActiveGunCollider()
        {
            if (activeGun == null)
            {
                return;
            }

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
            //Add Kill to player's db stats
            //var addKillScript = GameObject.Find("GamePlayFabController").GetComponent<GamePlayFabController>();
            //addKillScript.IncrementKillCount();

            // Get current deaths for this player
            photonView.Owner.CustomProperties.TryGetValue(KEY_KILLS, out object value);
            int kills = (value == null) ? 0 : Convert.ToInt32(value);

            // Add a kill for this player
            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_KILLS, ++kills } });
            
            if (DEBUG) Debug.LogFormat("PlayerManager: AddKill() kills = {0}, photonView.Owner.NickName = {1}", kills, photonView.Owner.NickName);
        }

        IEnumerator DestroyCar(GameObject skyCar)
        {
            yield return new WaitForSeconds(3.0f);
            PhotonNetwork.Destroy(skyCar);
        }

        /// <summary>
        /// Processes the inputs. 
        /// (This method should only be called in Update if photonView.IsMine)
        /// </summary>
        void ProcessInputs()
        {
            // If user wants to select a weapon
            if (Input.mouseScrollDelta.y != 0)
            {
                // If user is not currently in "selecting weapon" mode
                if (!selectingWeapon)
                {
                    // Put user in "selecting weapon" mode
                    selectingWeapon = true;
                    GameManager.Instance.canvas.transform.Find("Weapon Inventory Menu").GetComponent<WeaponsMenuManager>().OpenMenu();
                }
            }

            // If user is not currently selecting a weapon
            if (!selectingWeapon) { 
                if (Input.GetButtonDown("Fire1"))
                {
                    // we don't want to fire when we interact with UI buttons for example. IsPointerOverGameObject really means IsPointerOver*UI*GameObject
                    // notice we don't use on on GetbuttonUp() few lines down, because one can mouse down, move over a UI element and release, which would lead to not lower the isFiring Flag.
                    if (EventSystem.current.IsPointerOverGameObject())
                    {
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
                    if (activeGun != null && activeGun.IsReadyToShoot)
                    {
                        // Call the [PunRPC] Shoot method over photon network
                        photonView.RPC("Shoot", RpcTarget.All);
                    }
                }
            }
            // If user is selecting weapon
            else
            {
                GameObject weaponInventoryMenuGO = GameManager.Instance.canvas.transform.Find("Weapon Inventory Menu").gameObject;
                
                // If user wants to highlight previous weapon
                if (Input.mouseScrollDelta.y > 0)
                {
                    if (DEBUG && DEBUG_ProcessInputs) Debug.LogFormat("PlayerManager: ProcessInputs() User wants to highlight PREVIOUS weapon");
                    WeaponsMenuManager weaponsMenuManager = weaponInventoryMenuGO.GetComponent<WeaponsMenuManager>();
                    weaponsMenuManager.MoveHighlightIndexBackward();
                }

                // If user wants to highlight next weapon
                if (Input.mouseScrollDelta.y < 0)
                {
                    if (DEBUG && DEBUG_ProcessInputs) Debug.LogFormat("PlayerManager: ProcessInputs() User wants to highlight NEXT weapon");
                    WeaponsMenuManager weaponsMenuManager = weaponInventoryMenuGO.GetComponent<WeaponsMenuManager>();
                    weaponsMenuManager.MoveHighlightIndexForward();
                }

                // If user wants to select highlighted
                if (Input.GetButtonUp("Fire1"))
                {
                    selectingWeapon = false;

                    WeaponsMenuManager weaponsMenuManager = weaponInventoryMenuGO.GetComponent<WeaponsMenuManager>();
                    weaponsMenuManager.CloseMenu();
                    int gunViewID = weaponsMenuManager.GetHighlightedGunViewID();
                    if (gunViewID != -1)
                    {
                        SetActiveGun(gunViewID);
                    }
                }
            }

            // Check if user is trying to active weapon
            // If has pressed and released the G key...
            if (Input.GetKeyUp(KeyCode.G))
            {
                if (activeGun == null) {
                    if (DEBUG && DEBUG_ProcessInputs) Debug.Log("PlayerManager: ProcessInputs() Trying to drop active gun but this.activeGun == null");
                    return;
                }

                if (photonView.IsMine)
                {
                    // Set local player's active gun property (synced on the network)
                    PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_ACTIVE_GUN, null } });
                }
                // Drop this player's active gun (synchronized on network)
                //photonView.RPC("DropActiveGun", RpcTarget.All);
            }

            if (Input.GetKeyUp(KeyCode.C))
            {
                //var TimeToKeepAlive = 5;
                if (DEBUG && DEBUG_ProcessInputs) Debug.Log("keycode C");
                if (photonView.IsMine)//network ismasterclient
                {
                    GameObject skyCar = PhotonNetwork.Instantiate("RoombaCar", gameObject.transform.position, gameObject.transform.rotation);
                    //yield return new WaitForSeconds(2.0f);
                    //PhotonNetwork.Destroy(skycar);
                    StartCoroutine("DestroyCar", skyCar);
                }
                
            }

            if (Input.GetKeyUp(KeyCode.K))
            {
                if (photonView.IsMine)//network ismasterclient
                {
                    //Add Kill to player's db stats
                    var addKillScript = GameObject.Find("GamePlayFabController").GetComponent<GamePlayFabController>();
                    addKillScript.IncrementKillCount();
                }

            }

            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                // Call the [PunRPC] Shoot method over photon network
                photonView.RPC("SwapActiveGun", RpcTarget.All, 1);
            }

            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                // Call the [PunRPC] Shoot method over photon network
                photonView.RPC("SwapActiveGun", RpcTarget.All, 2);
            }

            if (Input.GetKeyUp(KeyCode.Q))
            {
                // Call the [PunRPC] Shoot method over photon network
                photonView.RPC("SwapActiveGun", RpcTarget.All, previousActiveGunType);
            }

        }

        void SetMode(string modeValue)
        {
            if (DEBUG && DEBUG_SetMode) Debug.LogFormat("PlayerManger: SetMode() modeValue = {0}", modeValue);
            // If we don't control this player...
            if (!photonView.IsMine)
            {
                return;
            }

            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_MODE, modeValue } });
        }

        #endregion Private Methods

        #region RPC Methods

        /// <summary>
        /// Swap 
        /// </summary>
        /// <param name="gunType">Type of gun to switch to. Currently, there are only two guns so the only valid options are 1 and 2</param>
        [PunRPC]
        void SwapActiveGun(int gunType)
        {
            if (DEBUG && DEBUG_SwapGun) Debug.LogFormat("PlayerManager: SwapGun gunType = {0}", gunType);
            
            // Find a gun in player inventory matching the gun type and set it as our active gun
            Transform inactiveWeapons = transform.Find("FirstPersonCharacter/Inactive Weapons");
            for (int i = 0; i < inactiveWeapons.childCount; i++)
            {
                Gun gun = inactiveWeapons.GetChild(i).GetComponent<Gun>();

                if (gun.TypeOfGun == gunType)
                {
                    SetActiveGun(gun.GetComponent<PhotonView>().ViewID);
                    break;
                }
            }
        }

        /// <summary>
        /// Drops this player's active gun
        /// </summary>
        [PunRPC]
        void DropActiveGun()
        {
            if (activeGun == null)
            {
                Debug.Log("PlayerManager: DropActiveGun() Trying to drop active gun but this.activeGun == null");
                return;
            }

            // Make player on this client drop the gun
            DropGun(activeGun);
        }
        
        /// <summary>
        /// Shoot was invoked over photon network via RPC
        /// </summary>
        /// <param name="info">Info about the RPC call such as who sent it, and when it was sent</param>
        [PunRPC]
        void Shoot(PhotonMessageInfo info)
        {
            //Debug.LogFormat("PlayerManager: [PunRPC] Shoot() {0}, {1}, {2}.", info.Sender, info.photonView, info.SentServerTime);
            if (activeGun == null)
            {
                if (DEBUG) Debug.LogFormat("PlayerManager: [PunRPC] Shoot() Trying to shoot gun but activeGun = null");
                return; 
            }

            /*
            // Tell the gun to shoot
            activeGun.Shoot();
            */
            
            // Tell the "show" gun to shoot
            activeShowGun.Shoot();
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
            AudioSource audioSource = GetComponent<AudioSource>();
            audioSource.enabled = true;
            Debug.LogFormat("PlayerManager: Die() audioSource = {0}, deathSound = {1}", audioSource, deathSound);

            // Play death sound
            audioSource.PlayOneShot(deathSound); // I read somewhere online that this allows the sounds to overlap

            // Register a death for this player on all clients
            AddDeath();

            // Respawn
            //Respawn();

            // Go to spectator mode
            StartDeadSpectatorMode();
        }

        [PunRPC]
        void SetAvatar()//TODO: make this update on all clients not just mine. should be broadcasting the rpc but i guess not?
        {
            /*if (photonView.IsMine)
            {
                Transform playerMgrTransform = this.gameObject.transform;
                if (PlayerData.GetComponent<PlayerData>().GetAvatarChoice() == "KyleRobot")//TODO change hardcoded string 
                {
                    Debug.Log("GameManager: Player chose KyleRobot");
                    unityChanPrefab.SetActive(false);//it's only doing it on the local client
                    playerMgrTransform.GetChild(2).gameObject.SetActive(false);//2 is the unity chan model, turn her off

                    //set animator to kyle robot, or maybe do nothing since he's the default
                }
                else if (PlayerData.GetComponent<PlayerData>().GetAvatarChoice() == "UnityChan")
                {
                    Debug.Log("GameManager: Player chose UnityChan");
                    //kyleRobotPrefab.SetActive(false);//it's only doing it on the local client
                    playerMgrTransform.GetChild(1).gameObject.SetActive(false);//1 is kyle robot, turn him off
                    animator.runtimeAnimatorController = (RuntimeAnimatorController)Resources.Load("Animation/UnityChanLocomotions");//the controller is significantly easier to get than the avatar
                    animator.avatar = playerMgrTransform.GetChild(2).gameObject.GetComponent<Animator>().avatar;
                    //PhotonView.Find(this.gameObject.GetPhotonView().ViewID).
                }
            }*/
        }

        #endregion RPC Methods

        #region IPunObservable Implementation

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
                stream.SendNext(Shield);
            }
            // If this client doesn't own this player (specifically, the PhotonView component on this player)...
            else
            {
                // Network player, receive data
                this.Health = (float)stream.ReceiveNext();
                this.Shield = (float)stream.ReceiveNext();
            }
        }

        #endregion IPunObservable Implementation

        #region IPunInstantiateMagicCallback implementation

        /// <summary>
        /// Photon Callback method. Called after player has been instantiated on network. Used to set up player properties.
        /// </summary>
        /// <param name="info"></param>
        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (DEBUG && DEBUG_OnPhotonInstantiate) Debug.LogFormat("PlayerManager: OnPhotonInstantiate() info.Sender = {0}, gameObject = {1}", info.Sender, gameObject);
            
            instantiationData = GetComponent<PhotonView>().InstantiationData;
            int actorNumber = (int)instantiationData[0];
            Player thisPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

            if (DEBUG && DEBUG_OnPhotonInstantiate)  Debug.LogFormat("PlayerManager: OnPhotonInstantiate() actorNumber = {0}, thisPlayer = {1}", actorNumber, thisPlayer);

            if (thisPlayer != null)
            {
                // Store this gameobject as this player's charater in Player.TagObject
                thisPlayer.TagObject = gameObject;

                // ***
                // For clients entering a room late...
                // Make sure all local players have picked up their guns and set their active gun
                // ***

                foreach (object key in PhotonNetwork.CurrentRoom.CustomProperties.Keys)
                {
                    string keyString = (string)key;
                    int gunViewID = -1;
                    try { gunViewID = Convert.ToInt32(keyString); }
                    catch (Exception) { continue; }

                    if (DEBUG && DEBUG_OnPhotonInstantiate) Debug.LogFormat("PlayerManager: OnPhotonInstantiate() gunViewID = {0} ", gunViewID);
                                        
                    Gun gun = PhotonView.Find(gunViewID).GetComponent<Gun>();
                    
                    // If this gun has a registered owner (player) in the room's CustomProperties...
                    if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(gunViewID.ToString(), out object gunOwnerNickName))
                    {
                        // If this player is the gun owner...
                        if (thisPlayer.NickName.Equals((string)gunOwnerNickName))
                        {
                            // Make gunOwner pick up this gun 
                            PickUpGun(gunViewID);

                            // If this player has registered an active gun in the player's CustomProperties...
                            if (thisPlayer.CustomProperties.TryGetValue(KEY_ACTIVE_GUN, out object gunViewIDObject))
                            {
                                // If this gun is the active gun (for the player who owns this gun)...
                                if (gunViewID == Convert.ToInt32(gunViewIDObject))
                                {
                                    if (DEBUG && DEBUG_OnPhotonInstantiate) Debug.LogFormat("PlayerManager: OnPhotonInstantiate() Making player {0} SETACTIVE gun {1} with ViewID = {2}", gunOwnerNickName, this.ToString(), gunViewID);

                                    // Make gunOwner set this gun as the active gun
                                    SetActiveGun(gunViewID);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion IPunInstantiateMagicCallback implementation
    }
}