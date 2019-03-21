using System;
using System.Collections;


using UnityEngine;
using UnityEngine.SceneManagement;


using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>this is a sample summary created with GhostDoc</summary>
    [RequireComponent(typeof(CountdownTimer))]
    public class GameManager : MonoBehaviourPunCallbacks, IPunObservable, IOnEventCallback
    {
        #region Public static and const Fields 

        // Key references for the Room CustomProperties hash table (so we don't use messy string literals)
        public const string KEY_STAGE_1_COUNTDOWN_START_TIME = "Stage 1 Start Time";
        public const string KEY_STAGE_2_COUNTDOWN_START_TIME = "Stage 2 Start Time";
        public const string KEY_TEAM_A_PLAYERS_COUNT = "Team A Size";
        public const string KEY_TEAM_B_PLAYERS_COUNT = "Team B Size";

        // Value references for the Room CustomProperties hash table (so we don't use messy string literals)
        public const string VALUE_UNCLAIMED_ITEM = "Unclaimed";
        public const string VALUE_VANISHED_ITEM = "Vanished";

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

        [Tooltip("Number of seconds before the wall comes down")]
        [SerializeField] private int stage1Time = 30;
        [Tooltip("Number of seconds per round")]
        [SerializeField] private int stage2Time = 1 * 60;
        [Tooltip("Make unclaimed items vanish when wall timer is up")]
        [SerializeField] private bool makeItemsVanish = true;
        [Tooltip("List of locations where a player on team A can be spawned")]
        [SerializeField] private Transform[] teamAPlayerSpawnPoints;
        [Tooltip("List of locations where a player on team B can be spawned")]
        [SerializeField] private Transform[] teamBPlayerSpawnPoints;
        [Tooltip("List of locations where a weapon can be spawned")]
        [SerializeField] private Transform[] weaponSpawnPoints;

        #endregion Private Serialized Fields

        #region Private Fields

        private const bool DEBUG = true;
        private const bool DEBUG_ReturnVanishedItems = false;
        private const bool DEBUG_DestroyUnclaimedItems = false;
        private const bool DEBUG_InstantiateLocalPlayer = true;
        

        // Event codes
        private readonly byte ChooseTeamForPlayer = 0;
        private readonly byte JoinTeam = 1; 

        private double gameStartTime;

        private ArrayList teamAList;
        private ArrayList teamBList;
        private ArrayList spawnedWeaponsList;

        private GameObject dividingWallGO;

        /// <summary>
        /// Keeps track of whether items were destroyed. Synchronized on all clients; 
        /// This is important in case master client changes anytime before all items are destroyed.
        /// </summary>
        private bool madeItemsVanish; // default = false
        private bool madeItemsReturn; // default = false;

        #endregion Private Fields

        #region Properties

        // USE WITH CARE! 
        // It can start with any positive value.
        // It will "wrap around" from 4294967.295 to 0!
        //public double CurrentRoundStartTime { get; private set; } = PhotonNetwork.Time;

        public int Stage1Time
        {
            get { return stage1Time; }
            private set { stage1Time = value; }
        }

        public int Stage2Time
        {
            get { return stage2Time; }
            private set { stage2Time = value; }
        }

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

        void RemoveUnclaimedItems()
        {
            if (DEBUG && DEBUG_DestroyUnclaimedItems) Debug.LogFormat("GameManger: DestroyUnclaimedItems()");
            // Get through all PhotonViews
            foreach (PhotonView photonView in PhotonNetwork.PhotonViews)
            {
                if (DEBUG && DEBUG_DestroyUnclaimedItems) Debug.LogFormat("GameManger: DestroyUnclaimedItems() photonView = {0}", photonView.ToString());
                //  If this photonView is on a Gun...
                Gun gun = photonView.gameObject.GetComponent<Gun>();
                if (gun != null)
                {
                    if (DEBUG && DEBUG_DestroyUnclaimedItems) Debug.LogFormat("GameManger: DestroyUnclaimedItems() gun = {0}", gun.ToString());
                    // Get the owner of the gun
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(photonView.ViewID.ToString(), out object owner);

                    // If the gun is not owned by a player
                    if (VALUE_UNCLAIMED_ITEM.Equals((string)owner))
                    {
                        if (DEBUG && DEBUG_DestroyUnclaimedItems) Debug.LogFormat("GameManger: DestroyUnclaimedItems() owner = {0}", owner.ToString());

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
                        gunPosition.y += 1000f;
                        photonView.gameObject.transform.position = gunPosition;

                        // Set the gun's owner to VALUE_VANISHED_ITEM
                        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { photonView.ViewID.ToString(), VALUE_VANISHED_ITEM } });
                    }
                }
            }

            // Make it clear to all clients that items were made to vanish
            // *** Make sure madeItemsVanish is reliably synchronized on network.
            madeItemsVanish = true;
        }

        // This method does the opposite of RemoveUnclaimedItems
        void ReturnVanishedItems()
        {
            if (DEBUG && DEBUG_ReturnVanishedItems) Debug.LogFormat("GameManger: ReturnVanishedItems()");
            // Get through all PhotonViews
            foreach (PhotonView photonView in PhotonNetwork.PhotonViews)
            {
                if (DEBUG && DEBUG_ReturnVanishedItems) Debug.LogFormat("GameManger: ReturnVanishedItems() photonView = {0}", photonView.ToString());
                //  If this photonView is on a Gun...
                Gun gun = photonView.gameObject.GetComponent<Gun>();
                if (gun != null)
                {
                    if (DEBUG && DEBUG_ReturnVanishedItems) Debug.LogFormat("GameManger: ReturnVanishedItems() gun = {0}", gun.ToString());
                    // Get the owner of the gun
                    if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(photonView.ViewID.ToString(), out object owner))
                    {
                        // If the gun is not owned by a player
                        if (VALUE_VANISHED_ITEM.Equals((string)owner))
                        {
                            if (DEBUG && DEBUG_ReturnVanishedItems) Debug.LogFormat("GameManger: ReturnVanishedItems() owner = {0}", owner.ToString());
                            
                            // The above code caused a fatal error that I couldn't figure out so for now I'm just going 
                            // to move the guns where players can't reach them. 
                            // (I already tried just disabling the photonView.gameObject but that didn't sync over the network.)
                            Vector3 gunPosition = photonView.gameObject.transform.position;
                            gunPosition.y -= 1000f;
                            photonView.gameObject.transform.position = gunPosition;


                            // Set the gun's owner to VALUE_VANISHED_ITEM
                            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { photonView.ViewID.ToString(), VALUE_UNCLAIMED_ITEM } });
                        }
                    }
                }
            }

            // Make it clear to all clients that items were made to return
            // *** Make sure madeItemsReturn is reliably synchronized on network.
            madeItemsReturn = true;
        }

        /// <summary>
        /// Ends the current game round. 
        /// Is called when the stage 2 timer has expired or when all players on one team are dead 
        /// </summary>
        void EndRound()
        {
            if (DEBUG) Debug.Log("GameManager: EndRound()");

            // For now, we're just going to start a new round when the current round ends...
            // Later, we might figure out more interesting logic

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG) Debug.Log("GameManager: EndRound() NOT MASTER CLIENT: Not responsible for ending rounds");
                return;
            }

            // Remove old unclaimed items
            // TODO

            // Remove all players' items
            // Maybe TODO

            // Balance teams 
            BalanceTeams();

            // Start new round
            StartRound();
        }

        void BalanceTeams()
        {
            if (DEBUG) Debug.Log("GameManager: BalanceTeams()");

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG) Debug.Log("GameManager: BalanceTeams() NOT MASTER CLIENT: Not responsible for balancing teams");
                return;
            }

            // Get the size of Team A and size of Team B
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
            int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0;
            int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

            // While teams are unbalanced...
            while (teamACount - teamBCount  > 1 || teamBCount - teamACount > 1)
            {
                // If Team A has more players than Team B...
                if (teamACount > teamBCount)
                {
                    // Move one player (the first one we find) from Team A to Team B
                    foreach (Player player in PhotonNetwork.PlayerList)
                    {
                        PlayerManager playerPM = ((GameObject)player.TagObject).GetComponent<PlayerManager>();

                        if (playerPM.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_A))
                        {
                            if (DEBUG) Debug.LogFormat("GameManager: BalanceTeams() Moving player {0} from Team A to Team B", player);

                            // Set player's team to Team B
                            playerPM.SetTeam(PlayerManager.VALUE_TEAM_NAME_B);

                            teamBCount++;
                            teamACount--;

                            break;
                        }
                    }
                }
                // If Team B has more players than Team A...
                else
                {
                    // Move one player (the first one we find) form Team B to Team A
                    foreach (Player player in PhotonNetwork.PlayerList)
                    {
                        PlayerManager playerPM = ((GameObject)player.TagObject).GetComponent<PlayerManager>();

                        if (playerPM.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_B))
                        {
                            if (DEBUG) Debug.LogFormat("GameManager: BalanceTeams() Moving player {0} from Team B to Team A", player);

                            // Set player's team to Team A
                            playerPM.SetTeam(PlayerManager.VALUE_TEAM_NAME_A);

                            teamACount++;
                            teamBCount--;

                            break;
                        }
                    }
                }
            }

            // Update size of Team A and Team B in CurrentRoom's CustomProperties
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                { KEY_TEAM_A_PLAYERS_COUNT, teamACount },
                { KEY_TEAM_B_PLAYERS_COUNT, teamBCount } });
        }

        /// <summary>
        /// Starts a game round.
        /// This method will do nothing if not called by master client.
        /// </summary>
        void StartRound()
        {
            if (DEBUG) Debug.Log("GameManager: StartRound()");

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG) Debug.Log("GameManager: StartRound() NOT MASTER CLIENT: Not responsible for starting rounds");
                return;
            }

            // Reset flags
            madeItemsVanish = false;
            madeItemsReturn = false;

            // Start the stage 1 timer on the network. This marks the beginning of the first stage of the game
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                { KEY_STAGE_1_COUNTDOWN_START_TIME, (float)PhotonNetwork.Time },
                { KEY_STAGE_2_COUNTDOWN_START_TIME, null }
            });

            // Reset the position of the dividing wall
            if (dividingWallGO != null)
            {
                dividingWallGO.GetComponent<WallDropAnimator>().ResetWallPosition();
            }

            // Make all clients reset their local player's position
            photonView.RPC("ResetPlayerPosition", RpcTarget.All);

            // Spawn new items
            //SpawnNewItems();

            // Make items return 
            ReturnVanishedItems();
        }
        
        [PunRPC] 
        public void ResetPlayerPosition()
        {
            if (DEBUG) Debug.Log("GameManager: ResetPlayerPosition()");
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerManager.KEY_TEAM, out object teamNameProp))
            {
                // If player is on team A... pick a random team A spawn point. Else... pick a random team B spawn point
                Transform playerSpawnPoint = ((string)teamNameProp).Equals(PlayerManager.VALUE_TEAM_NAME_A) ?
                    teamAPlayerSpawnPoints[new System.Random().Next(teamAPlayerSpawnPoints.Length)] :
                    teamBPlayerSpawnPoints[new System.Random().Next(teamBPlayerSpawnPoints.Length)];

                // Get the player's GameObject (we set the TagObject in PlayerManager)
                GameObject playerGO = PlayerManager.LocalPlayerInstance;

                // Move the player to the spawn point
                playerGO.GetComponent<PlayerManager>().Respawn();

                // Reset player health
                playerGO.GetComponent<PlayerManager>().ResetHealth();
            }
        }

        /// <summary>
        /// Spawns new items such as weapons on the network.
        /// This method does nothing if not called by master client.
        /// 
        /// We instantiate most non-player networked objects like guns and the wall as scene objects so they are not owned by the client
        /// who instantiates them (they are owned by the room) and won't be removed if/when the master client leaves the room. The only 
        /// client who can/should instantiate them is the master client. 
        /// </summary>
        void SpawnNewItems()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

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

            if (DEBUG) Debug.LogFormat("GameManager: SpawnNewItems() " +
                "((GameObject)spawnedWeaponsList[0]).GetPhotonView().ViewID = {0} " +
                "((GameObject)spawnedWeaponsList[1]).GetPhotonView().ViewID = {1}",
                ((GameObject)spawnedWeaponsList[0]).GetPhotonView().ViewID,
                ((GameObject)spawnedWeaponsList[1]).GetPhotonView().ViewID);
        }

        /// <summary>
        /// Having this code in its own method is kinda sorta unnecessary. It's only called once from Start() so the code could just go there.
        /// I pulled it out to make Start() code look a little cleaner... probably not a great reason. Oh, and this is a shitty comment for the 
        /// summary of this method. Oh well! Clean it up later!
        /// </summary>
        /// <returns></returns>
        void InstantiateLocalPlayer(string teamToJoin)
        {
            // Tutorial Debug Statement
            if (DEBUG && DEBUG_InstantiateLocalPlayer) Debug.LogFormat("GameManager: InstantiateLocalPlayer() We are Instantiating LocalPlayer from {0}", SceneManager.GetActiveScene().name);

            // Get the size of Team A and size of Team B
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
            int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0;
            int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

            // Figure out which team to add player to
            bool addPlayerToTeamA = (teamBCount < teamACount) ? false : true;

            // Increment player count on team they joined (Every client will execute this)
            PhotonNetwork.CurrentRoom.SetCustomProperties(addPlayerToTeamA ?
                new ExitGames.Client.Photon.Hashtable { { KEY_TEAM_A_PLAYERS_COUNT, ++teamACount } } :
                new ExitGames.Client.Photon.Hashtable { { KEY_TEAM_B_PLAYERS_COUNT, ++teamBCount } });

            // If adding player to team A... pick a random team A spawn point. Else... pick a random team B spawn point
            Transform playerSpawnPoint = addPlayerToTeamA ?
                teamAPlayerSpawnPoints[new System.Random().Next(teamAPlayerSpawnPoints.Length)] :
                teamBPlayerSpawnPoints[new System.Random().Next(teamBPlayerSpawnPoints.Length)];

            // Tutorial comment: we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
            GameObject myPlayerGO = PhotonNetwork.Instantiate(this.PlayerPrefab.name, playerSpawnPoint.position, playerSpawnPoint.rotation, 0);

            // Tell player what team they are on
            myPlayerGO.GetComponent<PlayerManager>().SetTeam(addPlayerToTeamA ? PlayerManager.VALUE_TEAM_NAME_A : PlayerManager.VALUE_TEAM_NAME_B);

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

            // Disable scene camera; we'll use player's first-person camera now
            Camera.main.enabled = false;
            
            // Find the PlayerInfoUI in the canvas
            GameObject playerInfoUIGO = canvas.GetComponentInChildren<PlayerInfoUI>().gameObject;
            // Call SetTarget() on PlayerInfoUI component 
            playerInfoUIGO.SendMessage("SetTarget", myPlayerGO.GetComponent<PlayerManager>(), SendMessageOptions.RequireReceiver);
        }

        #endregion Private Methods

        #region MonoBehaviour Callbacks

        void Awake()
        {
            // Singleton!
            Instance = this;
        }

        void Update()
        {
            UpdatePlayerPropertiesDisplay();
            ProcessInputs();
        }
        
        void Start()
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
                    string teamForLocalPlayerToJoin;

                    if (PhotonNetwork.IsMasterClient)
                    {

                        if (DEBUG) Debug.LogFormat("GameManager: Start() Sending request to master client for team to join");
                        // Because we are just starting, if we're the master client there should be no one else in the room
                        // We could place ourselves on Team A (set Team A size to 1 and Team B size to 0 in custom properties)
                        // and then just instantiate our local player...
                        teamForLocalPlayerToJoin = PlayerManager.VALUE_TEAM_NAME_A;

                        // Instantiate locally controlled player
                        InstantiateLocalPlayer(teamForLocalPlayerToJoin);
                    }
                    else
                    {
                        teamForLocalPlayerToJoin = PlayerManager.VALUE_TEAM_NAME_SPECT;

                        // ***
                        // Because we're not the master client, someone else must be. 
                        // Let's ask the master client what team we should be on.
                        // ***

                        if (DEBUG) Debug.LogFormat("GameManager: Start() Sending request to master client for team to join");

                        // Send request to master client for team to join
                        // Raise Event for only the master client to respond to. 
                        // *** Be careful! the master client may leave before responding! 
                        // *** Need code to protect against this possibility. 
                        // *** Possible solution: keep sending this request until you get an answer or this client becomes master client
                        object[] content = new object[] { PhotonNetwork.LocalPlayer.ActorNumber };
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }; 
                        SendOptions sendOptions = new SendOptions { Reliability = true };
                        PhotonNetwork.RaiseEvent(ChooseTeamForPlayer, content, raiseEventOptions, sendOptions);
                        
                        // We'll now wait for the response to come back from master client and handle it in OnEvent.
                        // That's where we instantiate the player
                    }



                    // Go on... Guess what this does
                    if (PhotonNetwork.IsMasterClient) // this check is redundant but kept for clarity
                    {
                        SpawnNewItems();

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
                        else if (Launcher.developmentOnly_levelToLoad.Equals("Simple Room"))
                        {
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

                // For now, we're just going to start the first round when the first client (master client) enters the gameroom...
                // Later, we might figure out more interesting logic for when the first round should begin
                if (PhotonNetwork.IsMasterClient) // this check is redundant but kept for clarity
                {
                    StartRound();
                }
            }
        }

        #endregion MonoBehaviour Callbacks

        #region MonoBehaviourPun Callbacks

        public override void OnEnable()
        {
            // Used to setup callbacks to OnEvent()
            PhotonNetwork.AddCallbackTarget(this);

            // Setup event callbacks for the ending of the two stages of the game
            CountdownTimer.OnCountdownTimer1HasExpired += OnStage1TimerIsExpired;
            CountdownTimer.OnCountdownTimer2HasExpired += OnStage2TimerIsExpired;
        }

        /// <summary>
        /// Called in the event that the game's stage 1 timer is expired
        /// </summary>
        public void OnStage1TimerIsExpired()
        {
            if (DEBUG) Debug.LogFormat("GameManager: OnStage1TimerIsExpired() PhotonNetwork.IsMasterClient = {0}", PhotonNetwork.IsMasterClient);

            if (PhotonNetwork.IsMasterClient)
            {
                // Start the stage 2 timer on the network. This marks beginning of the second stage of the game
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_STAGE_2_COUNTDOWN_START_TIME, (float)PhotonNetwork.Time } });

                // If we set the game option to make items vanish when the wall timer is up AND
                // If all the items have not been made to vanish 
                if (MakeItemsVanish && !madeItemsVanish)
                {
                    RemoveUnclaimedItems();
                }
            }

        }

        /// <summary>
        /// Called in the event that the game's stage 2 timer is expired
        /// If this event is called, the round has to be ended.
        /// </summary>
        public void OnStage2TimerIsExpired()
        {
            if (DEBUG) Debug.LogFormat("GameManager: OnStage2TimerIsExpired() PhotonNetwork.IsMasterClient = {0}", PhotonNetwork.IsMasterClient);
            // End the round
            EndRound();
        }

        public override void OnDisable()
        {
            // Used to remove callbacks to OnEvent()
            PhotonNetwork.RemoveCallbackTarget(this);
        }

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
                if (PlayerManager.VALUE_TEAM_NAME_A.Equals(teamName))
                {
                    // Get the size of Team A
                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
                    // Decrement size of Team A
                    PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                        { KEY_TEAM_A_PLAYERS_COUNT, Convert.ToInt32(teamACountObject) - 1 }
                    });
                }
                // If Player was on Team B...
                else if (PlayerManager.VALUE_TEAM_NAME_B.Equals(teamName))
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

        #endregion MonoBehaviourPun Callbacks

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
                stream.SendNext(madeItemsReturn);
            }
            // If this client doesn't own this player (specifically, the PhotonView component on this player)...
            else
            {
                // Network player, receive data
                this.madeItemsVanish = (bool)stream.ReceiveNext();
                this.madeItemsReturn = (bool)stream.ReceiveNext();
            }
        }

        #endregion IPunObservable Implementation

        #region IOnEventCallback Implementation

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            // If a client is asking master client to choose a team for their local player to join
            if (eventCode == ChooseTeamForPlayer)
            {
                // Get the actor number of the player owned by the client who sent the request for a team to join
                // We'll use this actor number to target only them for our response
                object[] data = (object[])photonEvent.CustomData;
                int actorNumber = (int)data[0];

                if (DEBUG) Debug.LogFormat("GameManager: OnEvent() Got a request to choose a team for player with actorNumber = {0}", actorNumber);

                // Get the size of Team A and size of Team B
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
                int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0;
                int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

                // Figure out what team the player should join
                string teamToJoin = (teamBCount < teamACount) ? PlayerManager.VALUE_TEAM_NAME_B: PlayerManager.VALUE_TEAM_NAME_A;

                // Respond to client with the info they wanted
                object[] content = new object[] { teamToJoin };
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { TargetActors = new[] { actorNumber } };
                SendOptions sendOptions = new SendOptions { Reliability = true };
                PhotonNetwork.RaiseEvent(JoinTeam, content, raiseEventOptions, sendOptions);
            }
            // If we requested a team to join from the master client and the master client responded...
            else if (eventCode == JoinTeam)
            {
                // Get the teamToJoin info that was sent by the master client
                object[] data = (object[])photonEvent.CustomData;
                string teamToJoin = (string)data[0];

                if (DEBUG) Debug.LogFormat("GameManager: OnEvent() Got a response from master client. teamToJoin = {0}", teamToJoin);

                InstantiateLocalPlayer(teamToJoin);
            }
        }

        #endregion IOnEventCallback Implementation
    }
}
