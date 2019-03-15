using System;
using System.Collections;


using UnityEngine;
using UnityEngine.SceneManagement;


using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>this is a sample summary created with GhostDoc</summary>
    public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region Public static and const Fields 
  
        // Key references for the Room CustomProperties hash table (so we don't use messy string literals)
        public const string KEY_TEAM_A_PLAYERS_COUNT = "Team A Size";
        public const string KEY_TEAM_B_PLAYERS_COUNT = "Team B Size";

        // Value references for the Room CustomProperties hash table (so we don't use messy string literals)
        public const string VALUE_UNCLAIMED_ITEM = "Unclaimed";

        // Singleton - you know what that means. Also, this won't show up in the inspector in Unity
        public static GameManager Instance;

        #endregion Public static and const Fields 

        #region Public Fields 

        [Tooltip("The prefab to use for representing the local player")]
        public GameObject PlayerPrefab; // used to instantiate the player pref on PhotonNetwork
        [Tooltip("List of weapon prefabs for this game")]
        public GameObject[] weaponsPrefabs; // used to instantiate the weapons on PhotonNetwork
        [Tooltip("Prefab for the team dividing wall")]
        public GameObject dividingWallPrefab; // used to instantiate the player pref on PhotonNetwork
        [Tooltip("The root GameObject in the hierarchy for all our game levels that we call \"Environment\"")]
        public GameObject environment; // used to give us a way to find GameObjects that are children of Environment
        [Tooltip("The root GameObject in the hierarchy for all our game levels that we call \"Canvas\"")]
        public GameObject canvas; // used to give us a way to find GameObjects that are children of Canvas

        #endregion Public Fields

        #region Private Serialized Fields 

        [Tooltip("Make unclaimed items vanish when wall timer is up?")]
        [SerializeField] private bool makeItemsVanish = true;
        [Tooltip("List of locations where a player on team A can be spawned")]
        [SerializeField] private Transform[] teamAPlayerSpawnPoints; // list of locations where a player can be spawned
        [Tooltip("List of locations where a player on team B can be spawned")]
        [SerializeField] private Transform[] teamBPlayerSpawnPoints; // list of locations where a player can be spawned
        [Tooltip("List of locations where a weapon can be spawned")]
        [SerializeField] private Transform[] weaponSpawnPoints; // list of locations where a weapon can be spawned

        #endregion Private Serialized Fields

        #region Private Fields

        private const bool DEBUG = true;
        
        private ArrayList teamAList;
        private ArrayList teamBList;
        private ArrayList spawnedWeaponsList;

        private GameObject dividingWallGO;

        /// <summary>
        /// Keeps track of whether items were destroyed. Synchronized on all clients; 
        /// This is important in case master client changes anytime before all items are destroyed.
        /// </summary>
        private bool madeItemsVanish = false; 

        #endregion Private Fields

        #region Properties

        // this property may not be necessary... check if we make good use of it publicly or should just 
        // stick with the private makeItemsVanish
        public ArrayList SpawnedWeaponsList
        {
            get { return spawnedWeaponsList; }
            private set { spawnedWeaponsList = value; }
        }

        // this property may not be necessary... check if we make good use of it publicly or should just 
        // stick with the private makeItemsVanish
        public bool MakeItemsVanish {
            get { return makeItemsVanish; }
            private set { makeItemsVanish = value; }
        } 

        #endregion

        #region Public Methods

        /// <Summary> 
        /// Should be called when a user chooses to leave the room 
        /// </Summary>
        public void LeaveRoom()
        {
            if (DEBUG) Debug.Log("GameManger: LeaveRoom() called.");
            
            // Leave the photon game room
            PhotonNetwork.LeaveRoom();

            // Make sure the cursor is visible again and not locked
            // This code might not work as expected... 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Used to load a Unity scene (probably representing the game room) on the PhotonNetwork
        /// Note: in the tutorial, this method is only called in OnPlayerEnteredRoom() and OnPlayerLeftRoom() by player on master client
        /// </summary>
        void LoadArena()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG) Debug.LogError("GameManager: LoadArena() -> PhotonNetwork : Trying to Load a level but we are not the master Client");
            }

            if (DEBUG) Debug.LogFormat("GameManager: LoadArena() -> PhotonNetwork : Loading Level : PhotonNetwork.CurrentRoom.PlayerCount = {0}", PhotonNetwork.CurrentRoom.PlayerCount);

            // Excerpt from tutorial:
            // There are two things to watch out for here, it's very important.
            // - PhotonNetwork.LoadLevel() should only be called if we are the MasterClient. So we check first that we are the MasterClient using 
            //   PhotonNetwork.IsMasterClient. It will be the responsibility of the caller to also check for this, we'll cover that in the next part of this section.
            //
            // - We use PhotonNetwork.LoadLevel() to load the level we want, we don't use Unity directly, because we want to rely on Photon 
            //   to load this level on all connected clients in the room, since we've enabled PhotonNetwork.AutomaticallySyncScene for this Game.
            // Old code (from tutorial): 
            // - PhotonNetwork.LoadLevel("Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
            PhotonNetwork.LoadLevel("Room for 1");
        }

        void ProcessInputs()
        {
#if !MOBILE_INPUT
            if (Input.GetKey(KeyCode.Tab))
            {
                // Disable Crosshair
                Transform crosshair = canvas.transform.Find("Crosshair");
                crosshair.gameObject.SetActive(false);

                // Enable Player Info Panel
                Transform playerInfoPanel = canvas.transform.Find("Player Info Panel");
                playerInfoPanel.gameObject.SetActive(true);
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                // Enable Crosshair
                Transform crosshair = canvas.transform.Find("Crosshair");
                crosshair.gameObject.SetActive(true);

                // Disable Player Info Panel
                Transform playerInfoPanel = canvas.transform.Find("Player Info Panel");
                playerInfoPanel.gameObject.SetActive(false);
            }
#endif
        }

        /// <summary>
        /// Updates GUI display of information about Players in this game room.
        /// </summary>
        void UpdatePlayerPropertiesDisplay()
        {
            // Get the text component where we want to display all players' properties
            Transform playerInfoText = canvas.transform.Find("Player Info Panel/Player Info List Scroll View/Viewport/Content/Player Info Text");
            Text playerInfoTextComponent = playerInfoText.GetComponent<Text>();

            // Clear the text display
            playerInfoTextComponent.text = "Press TAB to toggle this display\n\n";


            // Add the player's nickname to the display
            playerInfoTextComponent.text += "Game Room Name = " + PhotonNetwork.CurrentRoom.Name + ":\n";

            // Go through list of room properties (information about game)
            foreach (object propertyKey in PhotonNetwork.CurrentRoom.CustomProperties.Keys)
            {
                // Get the propertyValue of the property using the propertyKey
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(propertyKey, out object propertyValue);
                // Add the player's property's kev and value to the display
                playerInfoTextComponent.text += "\t" + propertyKey + " = " + propertyValue + "\n";
            }

            // Add an empty line to separate the info about different players
            playerInfoTextComponent.text += "\n";

            // Iterate through the list of players in this room
            Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;
            foreach (int playerKey in players.Keys)
            {
                // Get the player using the playerKey
                players.TryGetValue(playerKey, out Player player);
                // Add the player's nickname to the display
                playerInfoTextComponent.text += player.NickName + ":\n";

                // Go through list of player properties (information about player)
                foreach (object propertyKey in player.CustomProperties.Keys)
                {
                    // Get the propertyValue of the property using the propertyKey
                    player.CustomProperties.TryGetValue(propertyKey, out object propertyValue);
                    // Add the player's property's kev and value to the display
                    playerInfoTextComponent.text += "\t" + propertyKey + " = " + propertyValue + "\n";
                }

                // Add an empty line to separate the info about different players
                playerInfoTextComponent.text += "\n";
            }
        }

        void DestroyUnclaimedItems()
        {
            Debug.LogFormat("GameManger: DestroyUnclaimedItems()");
            // Get through all PhotonViews
            foreach (PhotonView photonView in PhotonNetwork.PhotonViews)
            {
                Debug.LogFormat("GameManger: DestroyUnclaimedItems() photonView = {0}", photonView.ToString());
                //  If this photonView is on a Gun...
                Gun gun = photonView.gameObject.GetComponent<Gun>();
                if (gun != null)
                {
                    Debug.LogFormat("GameManger: DestroyUnclaimedItems() gun = {0}", gun.ToString());
                    // Get the owner of the gun
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(photonView.ViewID.ToString(), out object owner);

                    // If the gun is not owned by a player
                    if (VALUE_UNCLAIMED_ITEM.Equals((string)owner))
                    {
                        Debug.LogFormat("GameManger: DestroyUnclaimedItems() owner = {0}", owner.ToString());

                        // The code I've commented out causes a fatal error that I haven't been able to figure out.
                        // Situation that causes the bug:
                        // Two clients are spawned into the room before the wall comes down. Both clients are holding a gun that they
                        // have been spawned with. One client picks up a new gun and drops the gun they were spawned with. The wall comes
                        // down (wall timer is up). The client who didn't pick up and drop a gun crashes immediately. WTF?! 
                        /*
                        // Remove room custom property for the gun ownership
                        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { photonView.ViewID.ToString(), null } });
                    
                        // Network-Destroy the GameObject associated with photonView
                        PhotonNetwork.Destroy(photonView);
                        */

                        // The above code caused a fatal error that I couldn't figure out so for now I'm just going 
                        // to move the guns where players can't reach them. 
                        // (I already tried just disabling the photonView.gameObject but that didn't sync over the network.)
                        Vector3 gunPosition = photonView.gameObject.transform.position;
                        gunPosition.y = 1000f;
                        photonView.gameObject.transform.position = gunPosition;
                    }
                }
            }

            // Make it clear to all clients that items were made to vanish
            // *** Make sure madeItemsVanish is reliably synchronized on network.
            madeItemsVanish = true;
        }

        #endregion Private Methods

        #region MonoBehaviour Callbacks

        private void Awake()
        {
            // Singleton!
            Instance = this;
        }

        private void Update()
        {
            UpdatePlayerPropertiesDisplay();
            ProcessInputs();

            if (PhotonNetwork.IsMasterClient)
            {
                // If we set the game option to make items vanish when the wall timer is up AND
                // If the wall timer is up AND 
                // If all the items have not been made to vanish 
                if (MakeItemsVanish && dividingWallGO.GetComponent<WallDropTimer>().TimeIsUp && !madeItemsVanish)
                {
                    DestroyUnclaimedItems();
                }
            }
        }

        private void Start()
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
            }
            else
            {
                /** Note from tutorial:
                 *  With this, we now only instantiate if the PlayerManager doesn't have a reference to an existing instance of localPlayer
                 */
                if (PlayerManager.LocalPlayerInstance == null)
                {
                    // Tutorial Debug Statement
                    Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManager.GetActiveScene().name);
                    
                    // Get the size of Team A and size of Team B
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
                    int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0; 
                    int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

                    // If Team B has fewer players than Team A... Add this player to team B. Else... Add this player to team A
                    bool addPlayerToTeamA = (teamBCount < teamACount) ? false : true;

                    // If adding player to team A... pick a random team A spawn point. Else... pick a random team B spawn point
                    Transform playerSpawnPoint = addPlayerToTeamA ?
                        teamAPlayerSpawnPoints[new System.Random().Next(teamAPlayerSpawnPoints.Length)] :
                        teamBPlayerSpawnPoints[new System.Random().Next(teamBPlayerSpawnPoints.Length)];

                    // Tutorial comment: we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                    GameObject myPlayerGO = PhotonNetwork.Instantiate(this.PlayerPrefab.name, playerSpawnPoint.position, playerSpawnPoint.rotation, 0);

                    // Tell player what team they are on
                    myPlayerGO.GetComponent<PlayerManager>().SetTeam(addPlayerToTeamA ? PlayerManager.TEAM_NAME_A : PlayerManager.TEAM_NAME_B);

                    // We need to enable all the controlling components for the local player 
                    // The prefab has these components disabled so we won't be controlling other players
                    myPlayerGO.GetComponent<Animator>().enabled = true;
                    myPlayerGO.GetComponent<CharacterController>().enabled = true;
                    myPlayerGO.GetComponent<AudioSource>().enabled = true;
                    myPlayerGO.GetComponent<FirstPersonController>().enabled = true;
                    myPlayerGO.GetComponent<PlayerAnimatorManager>().enabled = true;
                    myPlayerGO.GetComponent<PlayerManager>().enabled = true;
                    myPlayerGO.GetComponentInChildren<Camera>().enabled = true;
                    myPlayerGO.GetComponentInChildren<AudioListener>().enabled = true;
                    myPlayerGO.GetComponentInChildren<FlareLayer>().enabled = true;

                    // Disable Photon's syncing of the position of the gun (prefab) we are spawned with. Syncing will be enabled again when we drop the gun
                    myPlayerGO.GetComponentInChildren<Gun>()
                        .GetComponent<PhotonView>().ObservedComponents = new List<Component> { };

                    // Increment player count on team they joined (Every client will execute this)
                    if (addPlayerToTeamA)
                    {
                        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_TEAM_A_PLAYERS_COUNT, ++teamACount } });
                    }
                    else
                    {
                        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_TEAM_B_PLAYERS_COUNT, ++teamBCount } });
                    }

                    // Disable scene camera
                    Camera.main.enabled = false;

                    // Find the PlayerInfoUI in the canvas
                    GameObject playerInfoUIGO = canvas.GetComponentInChildren<PlayerInfoUI>().gameObject;
                    // Call SetTarget() on PlayerInfoUI component 
                    playerInfoUIGO.SendMessage("SetTarget", myPlayerGO.GetComponent<PlayerManager>(), SendMessageOptions.RequireReceiver);

                    // We instantiate most non-player networked objects like guns and the wall as scene objects so they are not owned by the client
                    // who instantiates them (they are owned by the room) and won't be removed if/when the master client leaves the room. The only 
                    // client who can/should instantiate them is the master client. 
                    // 
                    // If this is the master client...
                    if (PhotonNetwork.IsMasterClient)
                    {
                        // Create a new list to keep track of spawned weapons
                        spawnedWeaponsList = new ArrayList();

                        // Instantiate our two weapons at different spawn points for team A. Add each newly spawned weapon to the list
                        spawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[0].name, weaponSpawnPoints[0].position, weaponSpawnPoints[0].rotation, 0));
                        spawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[1].name, weaponSpawnPoints[1].position, weaponSpawnPoints[1].rotation, 0));
                        // Instantiate our two weapons at different spawn points for team B. Add each newly spawned weapon to the list
                        spawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[0].name, weaponSpawnPoints[2].position, weaponSpawnPoints[2].rotation, 0));
                        spawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[1].name, weaponSpawnPoints[3].position, weaponSpawnPoints[3].rotation, 0));

                        // Add the spawned weapon (key) and it's owner (value) to the properties for the current room
                        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                            { ((GameObject)spawnedWeaponsList[0]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM },
                            { ((GameObject)spawnedWeaponsList[1]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM },
                            { ((GameObject)spawnedWeaponsList[2]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM },
                            { ((GameObject)spawnedWeaponsList[3]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM } });
                        
                        if (DEBUG) Debug.LogFormat("GameManager: Start() " +
                           "((GameObject)spawnedWeaponsList[0]).GetPhotonView().ViewID = {0} " +
                           "((GameObject)spawnedWeaponsList[1]).GetPhotonView().ViewID = {1}",
                           ((GameObject)spawnedWeaponsList[0]).GetPhotonView().ViewID,
                           ((GameObject)spawnedWeaponsList[1]).GetPhotonView().ViewID);

                        // Instantiate Team Dividing Wall for the scene. At least right now, this has to be done differently for each arena
                        // 
                        // If arena is "Room for 1" unity scene...
                        if (Launcher.developmentOnly_levelToLoad.Equals("Room for 1"))
                        {
                            // Instantiate the dividing wall for "Room for 1" level
                            Vector3 wallPosition = new Vector3(258.3562f, 26.397f, 279.6928f); // copied vector3s from "Original Dividing Wall" and "Scene Props" transform positions in Unity before it was turned into a prefab
                            Quaternion wallRotation = Quaternion.Euler(new Vector3(0f, -45f, 0f)); // copied vector3 from Original Dividing Wall transform rotation in Unity before it was turned into a prefab
                            dividingWallGO = PhotonNetwork.InstantiateSceneObject(this.dividingWallPrefab.name, wallPosition, wallRotation, 0);
                        }
                        // If arena is "Simple Room" unity scene...
                        else if (Launcher.developmentOnly_levelToLoad.Equals("Simple Room")) { 
                            // Instantiate the dividing wall for "Simple Room" level
                            Vector3 wallPosition = new Vector3(0f, 20f, 0f); // copied vector3s from "Original Dividing Wall" and "Scene Props" transform positions in Unity before it was turned into a prefab
                            Quaternion wallRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f)); // copied vector3 from Original Dividing Wall transform rotation in Unity before it was turned into a prefab
                            dividingWallGO = PhotonNetwork.InstantiateSceneObject(this.dividingWallPrefab.name, wallPosition, wallRotation, 0);
                            
                            // Set the scale to match the "Simple Room" level size
                            dividingWallGO.gameObject.transform.localScale = new Vector3(10f, 40f, 200f);
                        }
                    }
                }
                else
                {
                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }
            }
        }

        #endregion MonoBehaviour Callbacks

        #region Photon Callbacks
        
        ///<summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            // Load the launcher scene
            SceneManager.LoadScene(0);
        }

        // Note: I think this function is called on every computer when someone enters the room except the person who is entering the room
        public override void OnPlayerEnteredRoom(Player other)
        {
            // Tutorial comment: not seen if you're the player connecting
            if (DEBUG) Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); 

            if (PhotonNetwork.IsMasterClient)
            {
                // Tutorial comment: called before OnPlayerLeftRoom
                if (DEBUG) Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); 
            }
        }

        // Note: I think this function is called on every computer when someone leaves the room except the person who is leaving the room
        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); //seen when other disconnects

            if (PhotonNetwork.IsMasterClient)
            {
                // Tutorial comment: called before OnPlayerLeftRoom
                Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); 

                // Get team the player is on (we expect teamName != null after this call)
                other.CustomProperties.TryGetValue(PlayerManager.KEY_TEAM, out object teamName);

                // If Player was on Team A...
                if (PlayerManager.TEAM_NAME_A.Equals(teamName))
                {
                    // Get the size of Team A
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
                    // Decrement size of Team A
                    PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                        { KEY_TEAM_A_PLAYERS_COUNT, Convert.ToInt32(teamACountObject) - 1 }
                    });
                }
                // If Player was on Team B...
                else if (PlayerManager.TEAM_NAME_B.Equals(teamName))
                {
                    // Get the size of Team B
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
                    // Decrement size of Team B
                    PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                        { KEY_TEAM_B_PLAYERS_COUNT, Convert.ToInt32(teamBCountObject) - 1 }
                    });
                }
            }
        }

        #endregion Photon Callbacks

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
                stream.SendNext(madeItemsVanish);
            }
            // If this client doesn't own this player (specifically, the PhotonView component on this player)...
            else
            {
                // Network player, receive data
                this.madeItemsVanish = (bool)stream.ReceiveNext();
            }
        }

        #endregion IPunObservable Implementation
    }
}
