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

        private PlayerProperties playerProperties; // represents our custom class for keeping track of player properties
        //private int kills = 0;
        //private int deaths = 0;

        #endregion

        #region Properties

        public ExitGames.Client.Photon.Hashtable PlayerInfo{
            get { return playerProperties.Properties; }
            private set { playerProperties.Properties = value; }
        }

        public int PhotonPlayerID { get; private set; }

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            // PlayerProperties is a class we created to help us set custom properties for photon players 
            playerProperties = gameObject.GetComponent<PlayerProperties>();

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
            if (!photonView.IsMine)
            {
                return;
            }

            if (DEBUG) Debug.LogFormat("PlayerManager: OnTriggerEnter() Collided with object with name \"{0}\"", other.name);
            
            // If this player collided with a Weapon (a collider on a gameobject with a tag == "Weapon")
            if (other.CompareTag("Weapon"))
            {
                if (DEBUG) Debug.LogFormat("PlayerManager: OnTriggerEnter() Collided with weapon with name \"{0}\"", other.GetComponentInParent<Gun>().name);
                
                // Pick up gun
                ReplaceCurrentGunWithPickedUpGun(other.GetComponentInParent<Gun>());
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

            // ***
            //
            // Probably want to change implementation to not use PlayerInfo anymore
            //
            // ***

            /*
            // Update what team this player is on in PlayerProperties
            PlayerInfo.Remove(PlayerProperties.KEY_TEAM);
            PlayerInfo.Add(PlayerProperties.KEY_TEAM, team);
            PhotonNetwork.LocalPlayer.SetCustomProperties(PlayerInfo);
            */
            
            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { {KEY_TEAM, team} });
        }
        
        /// <summary>
        /// Decrease health of player by damage amount. 
        /// </summary>
        /// <param name="amount">The amount of damage caused</param>
        public void TakeDamage(float amount)
        {
            // Note to self:
            //  Don't forget: Health will be synchronized by Photon via 'Object Synchronization'
            //  Each client will own one player (specifically, a PhotonView component on the player). 
            //  The client's player tells all other clients' instances of the player what their health is.
            //  ** Maybe this code should only be executed inside "if (photonView.IsMine) { }"

            // If the attacked player is the one this client owns...
            if (photonView.IsMine) {
                Health -= amount;

                if (Health <= 0)
                {
                    // Make player die (synchronized on network)
                    photonView.RPC("Die", RpcTarget.All); // calls the [PunRPC] Die method over photon network
                }
            }
        }

        /// <summary>
        /// Decrease health of player by damage amount.
        /// </summary>
        /// <param name="amount">The amount of damage caused</param>
        /// <param name="playerWhoCausedDamage">The player who caused the damage</param>
        public void TakeDamage(float amount, PlayerManager playerWhoCausedDamage)
        {
            // Note to self:
            //  Don't forget: Health will be synchronized by Photon via 'Object Synchronization'
            //  Each client will own one player (specifically, a PhotonView component on the player). 
            //  The client's player tells all other clients' instances of the player what their health is.
            //  ** Maybe this code should only be executed inside "if (photonView.IsMine) { }"

            // If the attacked player is the one this client owns...
            if (photonView.IsMine)
            {
                Health -= amount;

                if (Health <= 0)
                {
                    // Make player die (synchronized on network)
                    photonView.RPC("Die", RpcTarget.All); // calls the [PunRPC] Die method over photon network

                    playerWhoCausedDamage.AddKill();
                }
            }
        }
        
        // Remove this method when done testing the Die() method
        public void TestDie()
        {
            Die();
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
        /// Picks up gun.
        /// Requires player to have a gun already to know how to position the new gun.
        /// *This is not the best implementation!!
        /// </summary>
        /// <param name="pickedUpGun">The picked up gun.</param>
        void ReplaceCurrentGunWithPickedUpGun(Gun pickedUpGun)
        {
            // Put this gun in the GameObject hierarchy where the old gun was (i.e., make it a sibling to the old gun)
            pickedUpGun.transform.parent = activeGun.transform.parent;
            // Copy the old gun's position and rotation
            pickedUpGun.transform.position = activeGun.transform.position;
            pickedUpGun.transform.rotation = activeGun.transform.rotation;
            // Disable old gun and enable new gun
            // (Disabling the old gun is necessary if we change the code to all the player to pick up multiple guns)
            activeGun.transform.gameObject.SetActive(false);
            pickedUpGun.transform.gameObject.SetActive(true);
            // Set FPS Cam and Player who owns this gun
            pickedUpGun.fpsCam = activeGun.fpsCam;
            pickedUpGun.playerWhoOwnsThisGun = activeGun.playerWhoOwnsThisGun;

            // Make sure the gun is not moved by physics engine
            // (Because gun is currently floating in our program it would otherwise just fall)
            //pickedUpGun.GetComponentInChildren<Rigidbody>().isKinematic = true;

            // Keep a reference to what gun was active before replacement so we can return it as our "old gun"
            Gun oldGun = activeGun;
            // Keep track of what gun we want to shoot with now
            activeGun = pickedUpGun;
            // Make sure we don't collide with this gun again (while we're holding it)
            DisableActiveGunCollider();
            // Drop the gun we had before replacement
            DropGun(oldGun);
        }

        /// <summary>
        /// Drops the gun.
        /// </summary>
        /// <param name="gun">The gun. Must be gun that is currently being held by a player.</param>
        void DropGun(Gun gun)
        {
            // Make this gun a sibling of the player in the GameObject hierarchy
            gun.transform.parent = LocalPlayerInstance.transform.parent;
            // Toss gun away from player so we don't immediately collide with it again
            // (For now, just move it forward a bit)
            gun.transform.position = gun.transform.position + LocalPlayerInstance.transform.forward * howFarToTossWeapon;
            gun.transform.rotation = LocalPlayerInstance.transform.rotation;
            // Re-enable the gun and its gun's collider so it can be picked up again
            gun.transform.gameObject.SetActive(true);
            gun.GetComponentInChildren<BoxCollider>().enabled = true;

            // Re-enable the gun's ability to be moved by physics engine
            // (Because we disable it when we pick it up)
            //gun.GetComponentInChildren<Rigidbody>().isKinematic = false;
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

        /// <summary>
        /// Adds a death for this player. This death is registered on all clients. 
        /// This method is called by Die().
        /// </summary>
        void AddDeath()
        {
            // ***
            //
            // Look carefully at this code and how it is called during all possible gameplay scenarios!
            // This code is executed on every client (not just master client). 
            // There may be a hidden synchronization problems (edge cases) yet to be uncovered
            // 
            // ***

            // Get current deaths for this player
            photonView.Owner.CustomProperties.TryGetValue(PlayerProperties.KEY_DEATHS, out object value);
            int deaths = (value == null) ? 0 : Convert.ToInt32(value);

            // Add a death for this player
            /*
            deaths++;
            ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
            properties.Add(PlayerProperties.KEY_DEATHS, deaths);
            photonView.Owner.SetCustomProperties(properties);
            */
            photonView.Owner.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { {KEY_DEATHS, ++deaths} });

            if (DEBUG) Debug.LogFormat("PlayerManager: AddDeath() deaths = {0}, photonView.Owner.NickName = {1}", deaths, photonView.Owner.NickName);
        }

        /// <summary>
        /// Adds a kill for this player. This kill is registered on all clients. 
        /// This method is called by TakeDamage(float,PlayerManager) when player dies.
        /// </summary>
        void AddKill()
        {
            // ***
            //
            // Look carefully at this code and how it is called during all possible gameplay scenarios!
            // This code is executed on every client (not just master client). 
            // There may be a hidden synchronization problems (edge cases) yet to be uncovered
            // 
            // ***

            // Get current deaths for this player
            photonView.Owner.CustomProperties.TryGetValue(PlayerProperties.KEY_KILLS, out object value);
            int kills = (value == null) ? 0 : Convert.ToInt32(value);

            // Add a kill for this player
            /*
            kills++;
            ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
            properties.Add(PlayerProperties.KEY_KILLS, kills);
            photonView.Owner.SetCustomProperties(properties);
            */
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
            // ***
            // This code does not do what I originally thought it did... CHANGE IT!
            // ***
            // Share/Sync information about our Photon Player on the network
            PhotonNetwork.LocalPlayer.SetCustomProperties(PlayerInfo);
        }

        #endregion IPunInstantiateMagicCallback implementation
    }
}