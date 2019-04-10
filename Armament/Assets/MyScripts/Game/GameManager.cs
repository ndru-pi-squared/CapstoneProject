using System;
using System.Collections;


using UnityEngine;
using UnityEngine.SceneManagement;


using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityStandardAssets.Characters.ThirdPerson;

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
        public GameObject PlayerPrefab1; // used to instantiate the player pref on PhotonNetwork
        [Tooltip("The prefab to use for representing the local player")]
        public GameObject PlayerPrefab2; // used to instantiate the player pref on PhotonNetwork
        [Tooltip("List of weapon prefabs for this game")]
        public GameObject[] weaponsPrefabs; // used to instantiate the weapons on PhotonNetwork
        [Tooltip("List of AIBot prefabs for this game")]
        public GameObject[] botPrefabs; // used to instantiate the bots on PhotonNetwork
        [Tooltip("Prefab for the team dividing wall")]
        public GameObject dividingWallPrefab; // used to instantiate the player pref on PhotonNetwork
        [Tooltip("The root GameObject in the hierarchy for all our game levels that we call \"Environment\"")]
        public GameObject environment; // used to give us a way to find GameObjects that are children of Environment
        [Tooltip("The root GameObject in the hierarchy for all our game levels that we call \"Canvas\"")]
        public GameObject canvas; // used to give us a way to find GameObjects that are children of Canvas
        [Tooltip("List of Scene Cameras")]
        public Camera[] sceneCameras; // used to disable scene cameras when local player is created

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
        [Tooltip("Whether players can damage players on same team")]
        [SerializeField] private bool friendlyFire;
        [Tooltip("Whether round ends when last opponent dies")]
        [SerializeField] private bool roundEndsWhenLastOpponentDies = true;
        [Tooltip("Audio Clip to play when a round starts.")]
        [SerializeField] private AudioClip newRoundSound;

        [Tooltip("")]
        [SerializeField] private GameObject playerData;

        #endregion Private Serialized Fields

        #region Private Fields

        //private GameObject unityChanPrefab;
        //private GameObject kyleRobotPrefab;

        // Debug flags
        private const bool DEBUG = true;
        private const bool DEBUG_ReturnVanishedItems = false;
        private const bool DEBUG_DestroyUnclaimedItems = false;
        private const bool DEBUG_InstantiateLocalPlayer = false;
        private const bool DEBUG_OnStage1TimerIsExpired = true;
        private const bool DEBUG_OnStage2TimerIsExpired = true;
        private const bool DEBUG_BalanceTeams = false;
        private const bool DEBUG_StartRound = true;
        private const bool DEBUG_EndRound = false;
        private const bool DEBUG_LeaveRoom = false;
        private const bool DEBUG_LoadArena = false;
        private const bool DEBUG_RemoveGunOwnerships = false;
        private const bool DEBUG_SpawnNewItems = false;
        private const bool DEBUG_Play = false;
        private const bool DEBUG_ResetPlayerPosition = false;
        private const bool DEBUG_SpawnWall = false;
        private const bool DEBUG_OnPlayerDeath = false;

        // Event codes
        private readonly byte InstantiatePlayer = 0;

        private double gameStartTime;

        private ArrayList teamAList;
        private ArrayList teamBList;
        private GameObject dividingWallGO;

        //AICount
        public int AIBotsToSpawn;

        /// <summary>
        /// Keeps track of whether items were destroyed. Synchronized on all clients; 
        /// This is important in case master client changes anytime before all items are destroyed.
        /// </summary>
        private bool madeItemsVanish; // default = false
        private bool madeItemsReturn; // default = false;

        #endregion Private Fields

        #region Properties

        public bool FriendlyFire { get; private set; }

        // USE WITH CARE! 
        // It can start with any positive value.
        // It will "wrap around" from 4294967.295 to 0!
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

        public ArrayList SpawnedWeaponsList { get; private set; }

        public ArrayList SpawnedBotsList { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Event Handler. Called in the event that the game's stage 1 timer is expired
        /// </summary>
        public void OnStage1TimerIsExpired()
        {
            if (DEBUG && DEBUG_OnStage1TimerIsExpired) Debug.LogFormat("GameManager: OnStage1TimerIsExpired() PhotonNetwork.IsMasterClient = {0}", PhotonNetwork.IsMasterClient);

            if (PhotonNetwork.IsMasterClient)
            {
                // Start the stage 2 timer on the network. This marks beginning of the second stage of the game
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { KEY_STAGE_2_COUNTDOWN_START_TIME, (float)PhotonNetwork.Time } });

                // If we set the game option to make items vanish when the wall timer is up AND
                // If all the items have not been made to vanish 
                if (makeItemsVanish && !madeItemsVanish)
                {
                    RemoveUnclaimedItems();
                }
            }

        }

        /// <summary>
        /// Event Handler. Called in the event that the game's stage 2 timer is expired
        /// If this event is called, the round has to be ended.
        /// </summary>
        public void OnStage2TimerIsExpired()
        {
            if (DEBUG && DEBUG_OnStage2TimerIsExpired) Debug.LogFormat("GameManager: OnStage2TimerIsExpired() PhotonNetwork.IsMasterClient = {0}", PhotonNetwork.IsMasterClient);
            // End the round
            EndRound();
        }

        /// <Summary> 
        /// Event Handler for Leave Room button being clicked
        /// </Summary>
        public void LeaveRoom()
        {
            if (DEBUG && DEBUG_LeaveRoom) Debug.Log("GameManger: LeaveRoom() called.");

            // Leave the photon game room
            PhotonNetwork.LeaveRoom();

            // Make sure the cursor is visible again and not locked
            // This code might not work as expected... 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Event Handler for Play button being clicked.
        /// </summary>
        public void OnPlayButtonClicked()
        {
            // Send request to master client for team to join
            // Raise Event for only the master client to respond to. 
            // *** There is a chance the master client leaves before handling the event. I think that might be alright
            // *** since the user can click the Play button again. 
            if (DEBUG && DEBUG_Play) Debug.LogFormat("GameManager: Play() Sending request to master client for team to join");

            string teamPreference = playerData.GetComponent<PlayerData>().GetAvatarChoice(); // *** We'll take this information for now but PlayerData.cs should be changed

            object[] content = new object[] { PhotonNetwork.LocalPlayer.ActorNumber, teamPreference };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(InstantiatePlayer, content, raiseEventOptions, sendOptions);

            //Create an AI player
            //object[] content2 = new object[] { PhotonNetwork.LocalPlayer.ActorNumber };
            //PhotonNetwork.RaiseEvent(InstantiatePlayer, content2, raiseEventOptions, sendOptions);
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
                if (DEBUG && DEBUG_LoadArena) Debug.LogError("GameManager: LoadArena() -> PhotonNetwork : Trying to Load a level but we are not the master Client");
            }

            if (DEBUG && DEBUG_LoadArena) Debug.LogFormat("GameManager: LoadArena() -> PhotonNetwork : Loading Level : PhotonNetwork.CurrentRoom.PlayerCount = {0}", PhotonNetwork.CurrentRoom.PlayerCount);

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
                playerInfoTextComponent.text += "NickName = [" + player.NickName + "], ActorNumber = [ " + player.ActorNumber + "], UserId = [" + player.UserId + "]:\n";

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

        /// <summary>
        /// Removes unclaimed items. Called after the wall comes down.
        /// </summary>
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

        /// <summary>
        /// This method does the opposite of RemoveUnclaimedItems
        /// </summary>
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
            if (DEBUG && DEBUG_EndRound) Debug.Log("GameManager: EndRound()");

            // For now, we're just going to start a new round when the current round ends...
            // Later, we might figure out more interesting logic

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG && DEBUG_EndRound) Debug.Log("GameManager: EndRound() NOT MASTER CLIENT: Not responsible for ending rounds");
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

        /// <summary>
        /// Balances teams (inelegantly) based on size. Makes both teams have same number of players (give or take one player).
        /// Called in EndRound() right before StartRound().
        /// </summary>
        void BalanceTeams()
        {
            if (DEBUG && DEBUG_BalanceTeams) Debug.Log("GameManager: BalanceTeams()");

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG && DEBUG_BalanceTeams) Debug.Log("GameManager: BalanceTeams() NOT MASTER CLIENT: Not responsible for balancing teams");
                return;
            }

            // Get the size of Team A and size of Team B
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
            int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0;
            int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

            // While teams are unbalanced...
            while (teamACount - teamBCount > 1 || teamBCount - teamACount > 1)
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
                            if (DEBUG && DEBUG_BalanceTeams) Debug.LogFormat("GameManager: BalanceTeams() Moving player {0} from Team A to Team B", player);

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
                            if (DEBUG && DEBUG_BalanceTeams) Debug.LogFormat("GameManager: BalanceTeams() Moving player {0} from Team B to Team A", player);

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
            if (DEBUG && DEBUG_StartRound) Debug.Log("GameManager: StartRound()");

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG && DEBUG_StartRound) Debug.Log("GameManager: StartRound() NOT MASTER CLIENT: Not responsible for starting rounds");
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
                dividingWallGO.GetComponent<WallTarget>().ResetHealth();
            }

            // Make all clients reset their local player's position
            photonView.RPC("ResetPlayerPosition", RpcTarget.All);

            // Make all clients reset their local player's position
            photonView.RPC("PlayNewRoundSound", RpcTarget.All);

            // Spawn new items
            //SpawnNewItems();

            // Make items return 
            ReturnVanishedItems();
        }

        [PunRPC]
        void PlayNewRoundSound()
        {
           /* GameObject announcer = new GameObject("Announcer");
            AudioSource audioSource = announcer.AddComponent<AudioSource>();
            Debug.LogFormat("PlayerManager: Die() audioSource = {0}, newRoundSound = {1}", audioSource, newRoundSound);

            // Play death sound
            audioSource.PlayOneShot(newRoundSound); // I read somewhere online that this allows the sounds to overlap
            Destroy(announcer, 5f);*/
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
            SpawnedWeaponsList = new ArrayList();

            // Instantiate our two weapons at different spawn points for team A. Add each newly spawned weapon to the list
            SpawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[0].name, weaponSpawnPoints[0].position, weaponSpawnPoints[0].rotation, 0));
            SpawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[1].name, weaponSpawnPoints[1].position, weaponSpawnPoints[1].rotation, 0));
            // Instantiate our two weapons at different spawn points for team B. Add each newly spawned weapon to the list
            SpawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[0].name, weaponSpawnPoints[2].position, weaponSpawnPoints[2].rotation, 0));
            SpawnedWeaponsList.Add(PhotonNetwork.InstantiateSceneObject(this.weaponsPrefabs[1].name, weaponSpawnPoints[3].position, weaponSpawnPoints[3].rotation, 0));

            // Add the spawned weapon (key) and it's owner (value) to the properties for the current room
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                        { ((GameObject)SpawnedWeaponsList[0]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM },
                        { ((GameObject)SpawnedWeaponsList[1]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM },
                        { ((GameObject)SpawnedWeaponsList[2]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM },
                        { ((GameObject)SpawnedWeaponsList[3]).GetPhotonView().ViewID.ToString(), VALUE_UNCLAIMED_ITEM } });

            if (DEBUG && DEBUG_SpawnNewItems) Debug.LogFormat("GameManager: SpawnNewItems() " +
                "((GameObject)spawnedWeaponsList[0]).GetPhotonView().ViewID = {0} " +
                "((GameObject)spawnedWeaponsList[1]).GetPhotonView().ViewID = {1}",
                ((GameObject)SpawnedWeaponsList[0]).GetPhotonView().ViewID,
                ((GameObject)SpawnedWeaponsList[1]).GetPhotonView().ViewID);
        }

        /// <summary>
        /// Spawns team dividng wall.
        /// </summary>
        void SpawnWall()
        {
            // Instantiate Team Dividing Wall for the scene. At least right now, this has to be done differently for each arena
            // 
            // If arena is "Room for 1" unity scene...
            if (Launcher.developmentOnly_levelToLoad.Equals("Room for 1"))
            {
                // Instantiate the dividing wall for "Room for 1" level
                Vector3 wallPosition = new Vector3(258.3562f, 26.397f, 279.6928f); // copied vector3s from "Original Dividing Wall" and "Scene Props" transform positions in Unity before it was turned into a prefab
                Quaternion wallRotation = Quaternion.Euler(new Vector3(0f, -45f, 0f)); // copied vector3 from Original Dividing Wall transform rotation in Unity before it was turned into a prefab
                dividingWallGO = PhotonNetwork.InstantiateSceneObject(this.dividingWallPrefab.name, wallPosition, wallRotation, 0, new[] { (object)wallPosition });
            }
            // If arena is "Simple Room" unity scene...
            else if (Launcher.developmentOnly_levelToLoad.Equals("Simple Room") || Launcher.developmentOnly_levelToLoad.Equals("Simple Room 2"))
            {
                // Instantiate the dividing wall for "Simple Room" level
                Vector3 wallPosition = new Vector3(0f, 20f, 0f); // copied vector3s from "Original Dividing Wall" and "Scene Props" transform positions in Unity before it was turned into a prefab
                Quaternion wallRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f)); // copied vector3 from Original Dividing Wall transform rotation in Unity before it was turned into a prefab
                dividingWallGO = PhotonNetwork.InstantiateSceneObject(this.dividingWallPrefab.name, wallPosition, wallRotation, 0, new[] { (object)wallPosition });

                // Set the scale to match the "Simple Room" level size
                dividingWallGO.gameObject.transform.localScale = new Vector3(1f, 40f, 200f);
                if (DEBUG && DEBUG_SpawnWall) Debug.LogFormat("GameManager: SpawnWall() dividingWallGO.transform.position = {0}", dividingWallGO.transform.position);
            }
            // If arena is "Space_Arena" unity scene...
            else if (Launcher.developmentOnly_levelToLoad.Equals("Space_Arena"))
            {
                // Instantiate the dividing wall for "Space_Arena" level
                Vector3 wallPosition = new Vector3(0f, 39.5f, 0f); // copied vector3s from "Original Dividing Wall" and "Scene Props" transform positions in Unity before it was turned into a prefab
                Quaternion wallRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f)); // copied vector3 from Original Dividing Wall transform rotation in Unity before it was turned into a prefab
                dividingWallGO = PhotonNetwork.InstantiateSceneObject(this.dividingWallPrefab.name, wallPosition, wallRotation, 0, new[] { (object)wallPosition });
            }
        }

        /// <summary>
        /// Instantiates locally controlled player on the network. We only have to instantiate locally controlled players; 
        /// all other (remotely controlled players) will automatically be instantiated locally by photon.
        /// </summary>
        /// <returns></returns>
        GameObject InstantiatePlayerForActor(string teamToJoin, int actorNumber)
        {
            // Tutorial Debug Statement
            if (DEBUG && DEBUG_InstantiateLocalPlayer) Debug.LogFormat("GameManager: InstantiateLocalPlayer() We are Instantiating LocalPlayer from {0}, teamToJoin = {1}", SceneManager.GetActiveScene().name, teamToJoin);

            // Get the size of Team A and size of Team B
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
            int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0;
            int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

            // Set a flag: whether this player is joining Team A or not
            bool addPlayerToTeamA = teamToJoin.Equals(PlayerManager.VALUE_TEAM_NAME_A);

            // Increment player count in CurrentRoom.CustomProperties for the team this player is joining
            PhotonNetwork.CurrentRoom.SetCustomProperties(addPlayerToTeamA ?
                new ExitGames.Client.Photon.Hashtable { { KEY_TEAM_A_PLAYERS_COUNT, ++teamACount } } :
                new ExitGames.Client.Photon.Hashtable { { KEY_TEAM_B_PLAYERS_COUNT, ++teamBCount } });

            // Find a spawn point for the player based on the team that player is on
            // If adding player to team A... pick a random team A spawn point. Else... pick a random team B spawn point
            Transform playerSpawnPoint = addPlayerToTeamA ?
                teamAPlayerSpawnPoints[new System.Random().Next(teamAPlayerSpawnPoints.Length)] :
                teamBPlayerSpawnPoints[new System.Random().Next(teamBPlayerSpawnPoints.Length)];

            GameObject playerPrefab = addPlayerToTeamA ? PlayerPrefab1 : PlayerPrefab2;

            // Tutorial comment: we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
            GameObject playerGO = PhotonNetwork.InstantiateSceneObject(playerPrefab.name, playerSpawnPoint.position, playerSpawnPoint.rotation, 0, new[] { (object)actorNumber, teamToJoin });
            //give FPS controller both models I guess? seems like there would be unnecessary amnts of memory stored, so maybe i can fix later
            //here
            //kyleRobotPrefab = playerGO.transform.GetChild(1).gameObject;
            //unityChanPrefab = playerGO.transform.GetChild(2).gameObject;
            //here
            /*if (photonView.IsMine)
            {
                if (PlayerData.GetComponent<PlayerData>().GetAvatarChoice() == "KyleRobot")//TODO change hardcoded string 
                {
                    Debug.Log("GameManager: Player chose KyleRobot");
                    unityChanPrefab.SetActive(false);//it's only doing it on the master

                    //set animator to kyle robot, or maybe do nothing since he's the default
                }
                else if (PlayerData.GetComponent<PlayerData>().GetAvatarChoice() == "UnityChan")
                {
                    Debug.Log("GameManager: Player chose UnityChan");
                    kyleRobotPrefab.SetActive(false);//it's only doing it on the master because this code is only called on the master client
                    //set animator to unity chan
                }
            }*/

            return playerGO;
        }

        /// <summary>
        /// Removes Gun Ownership properties for all the guns owned by the given player. 
        /// Called from OnPlayerLeftRoom() when a player leaves a room
        /// </summary>
        /// <param name="player">The player whose guns need to be</param>
        void RemoveGunOwnerships(Player player)
        {
            if (DEBUG && DEBUG_RemoveGunOwnerships) Debug.LogFormat("GameManager: RemoveGunOwnerships() player = {0}", player);

            ExitGames.Client.Photon.Hashtable propertiesToRemove = new ExitGames.Client.Photon.Hashtable();
            // Go through list of room properties (information about game)
            foreach (object propertyKey in PhotonNetwork.CurrentRoom.CustomProperties.Keys)
            {
                // Get the propertyValue of the property using the propertyKey
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(propertyKey, out object propertyValue);

                if (player.NickName.Equals(propertyValue))
                {
                    if (DEBUG && DEBUG_RemoveGunOwnerships) Debug.LogFormat("GameManager: RemoveGunOwnerships() player = {0}, propertyKey = {1}, propertyValue = {2}", player, propertyKey, propertyValue);

                    propertiesToRemove.Add(propertyKey, null);
                }
            }

            PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToRemove);
        }

        #endregion Private Methods

        #region MonoBehaviour Callbacks

        void Awake()
        {
            Debug.Log("Awake():");
            // Singleton!
            Instance = this;

            // Set public property to the value set in inspector
            FriendlyFire = friendlyFire;
        }

        void Update()
        {
            UpdatePlayerPropertiesDisplay();
            ProcessInputs();
        }

        void Start()
        {
            DontDestroyOnLoad(this.gameObject);//tentative fix for null gameobject, but it happened after i left the room when i put this line in. gotta look into this
            if (PlayerPrefab1 == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayerPrefab1 Reference. Please set it up in GameObject 'Game Manager'", this);
                return;
            }
            if (PlayerPrefab2 == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayerPrefab2 Reference. Please set it up in GameObject 'Game Manager'", this);
                return;
            }

            /** Note from tutorial:
                *  With this, we now only instantiate if the PlayerManager doesn't have a reference to an existing instance of localPlayer
                */
            if (PlayerManager.LocalPlayerInstance == null)
            {
                // Go on... Guess what this does
                if (PhotonNetwork.IsMasterClient) // this check is redundant but kept for clarity
                {
                    SpawnNewItems();
                    SpawnWall();
                    SpawnBots(AIBotsToSpawn);
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

        public override void OnEnable()
        {
            // Used to setup callbacks to OnEvent()
            PhotonNetwork.AddCallbackTarget(this);

            // Setup event callbacks for the ending of the two stages of the game
            CountdownTimer.OnCountdownTimer1HasExpired += OnStage1TimerIsExpired;
            CountdownTimer.OnCountdownTimer2HasExpired += OnStage2TimerIsExpired;
        }

        public override void OnDisable()
        {
            // Remove callbacks to OnEvent()
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        #endregion MonoBehaviour Callbacks

        #region PlayerManager Callbacks

        public void OnPlayerDeath(PlayerManager deadPlayer)
        {
            // TODO if master client, figure out if player was last one on team and if so, start next round

            if (DEBUG && DEBUG_OnPlayerDeath) Debug.Log("GameManager: OnPlayerDeath()");

            if (!PhotonNetwork.IsMasterClient)
            {
                if (DEBUG && DEBUG_OnPlayerDeath) Debug.Log("GameManager: OnPlayerDeath() NOT MASTER CLIENT: Doing nothing...");
                return;
            }
            
            if (!roundEndsWhenLastOpponentDies)
            {
                if (DEBUG && DEBUG_OnPlayerDeath) Debug.Log("GameManager: OnPlayerDeath() CLIENT IS MasterClient: " +
                    "roundEndsWhenLastOpponentDies = false, so Doing nothing...");
                return;
            }

            if (DEBUG && DEBUG_OnPlayerDeath) Debug.Log("GameManager: OnPlayerDeath() CLIENT IS MasterClient: " +
                "roundEndsWhenLastOpponentDies = true, so Checking if player who just died was last one alive on team...");

            // Figure out if player who just died was last one alive on team
            bool morePlayersOnTeamStillAlive = false;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                PlayerManager pPlayerManager = ((GameObject)player.TagObject).GetComponent<PlayerManager>();

                // If player is on the same team as deadPlayer...
                if (pPlayerManager.GetTeam().Equals(deadPlayer.GetTeam()))
                {
                    // If mode key was set...
                    if (player.CustomProperties.TryGetValue(PlayerManager.KEY_MODE, out object modeProp))
                    {
                        // If player is alive
                        if (((string)modeProp).Equals(PlayerManager.VALUE_MODE_ALIVE))
                        {
                            morePlayersOnTeamStillAlive = true;
                            break;
                        }
                    }
                }
            }

            // If player who just died was last one alive on team
            if (!morePlayersOnTeamStillAlive)
            {
                if (DEBUG && DEBUG_OnPlayerDeath) Debug.Log("GameManager: OnPlayerDeath() CLIENT IS MasterClient: " +
                    "morePlayersOnTeamStillAlive = false, so Ending this round...");
                
                // End this round (which will start a new round)
                EndRound();
            }

        }

        #endregion PlayerManager Callbacks

        #region MonoBehaviourPun Callbacks

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

            // All clients: destroy the game object (avatar) of the player who left
            Destroy((GameObject)other.TagObject);

            if (PhotonNetwork.IsMasterClient)
            {
                // Tutorial comment: called before OnPlayerLeftRoom
                Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient);

                // Remove Gun ownership properties from the CurrentRoom.CustomProperties for the player who left
                RemoveGunOwnerships(other);

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

        #region PunRPC Methods

        /// <summary>
        /// Moves the local player back to a spawn point
        /// </summary>
        [PunRPC]
        public void ResetPlayerPosition()
        {
            if (DEBUG && DEBUG_ResetPlayerPosition) Debug.Log("GameManager: ResetPlayerPosition()");

            // Every client needs to: 
            // Go through list of all players
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // If player has Mode set...
                if (player.CustomProperties.TryGetValue(PlayerManager.KEY_MODE, out object modeProp))
                {
                    // If they are in Dead spectator Mode...
                    if (PlayerManager.VALUE_MODE_DEAD_SPECT.Equals((string)modeProp))
                    {
                        // Change to Alive Mode
                        ((GameObject)player.TagObject).GetComponent<PlayerManager>().StopDeadSpectatorMode();
                    }
                }
            }

            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerManager.KEY_TEAM, out object teamNameProp))
            {
                // If player is on team A... pick a random team A spawn point. Else... pick a random team B spawn point
                Transform playerSpawnPoint = ((string)teamNameProp).Equals(PlayerManager.VALUE_TEAM_NAME_A) ?
                    teamAPlayerSpawnPoints[new System.Random().Next(teamAPlayerSpawnPoints.Length)] :
                    teamBPlayerSpawnPoints[new System.Random().Next(teamBPlayerSpawnPoints.Length)];

                // Get the player's GameObject (we set the TagObject in PlayerManager)
                GameObject playerGO = PlayerManager.LocalPlayerInstance;

                // Move the player to the spawn point
                playerGO.GetComponent<PlayerManager>().Respawn(playerSpawnPoint);

                // Reset player health
                playerGO.GetComponent<PlayerManager>().ResetHealth();
                // Reset player shield
                playerGO.GetComponent<PlayerManager>().ResetShield();
            }
        }

        #endregion PunRPC Methods

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

        /// <summary>
        /// Handles Events that have been raised on the network. 
        /// </summary>
        /// <param name="photonEvent"></param>
        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            // If a client is asking master client to instantiate a new player for them...
            if (eventCode == InstantiatePlayer)
            {
                Debug.Log("instantiate player event code in OnEvent() called");
                // Get the photon player actor number of the player owned by the client who sent the request
                // We'll use this actor number when instantiating the player GO on the network to identify who
                // should own/control the player. 
                // Each client has one player with an associated actor number. In PlayerManager.Awake(), each client
                // will find the player that belongs to them when it gets instantiated on the network and set it up as
                // their local player. Yippee!
                object[] data = (object[])photonEvent.CustomData;
                int actorNumber = (int)data[0];

                string teamPreference = (string)data[1] == "KyleRobot" ? PlayerManager.VALUE_TEAM_NAME_A : PlayerManager.VALUE_TEAM_NAME_B;

                if (DEBUG) Debug.LogFormat("GameManager: OnEvent() Got a request to choose a team for player with actorNumber = {0}", actorNumber);

                // Get the size of Team A and size of Team B
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_A_PLAYERS_COUNT, out object teamACountObject);
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_TEAM_B_PLAYERS_COUNT, out object teamBCountObject);
                int teamACount = (teamACountObject != null) ? Convert.ToInt32(teamACountObject) : 0;
                int teamBCount = (teamBCountObject != null) ? Convert.ToInt32(teamBCountObject) : 0;

                // Figure out what team the player should join
                string teamToJoin = teamBCount < teamACount ? PlayerManager.VALUE_TEAM_NAME_B : PlayerManager.VALUE_TEAM_NAME_A;
                teamToJoin = teamBCount == teamACount ? teamPreference : teamToJoin;

                // Instantiate player for client
                GameObject playerGO = InstantiatePlayerForActor(teamToJoin, actorNumber);


                // Transfer ownership of this player's photonview (and GameObject) to the client requesting a player be instantiated them
                playerGO.GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.CurrentRoom.GetPlayer(actorNumber));
            }
        }

        #endregion IOnEventCallback Implementation

        public void SpawnBots(int bots) {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (botPrefabs.Length > 0)
            {// Create a new list to keep track of spawned bots
                SpawnedBotsList = new ArrayList();

                // Instantiate our two bots at different spawn points for team A. Add each newly spawned bot to the list
                SpawnedBotsList.Add(PhotonNetwork.InstantiateSceneObject(this.botPrefabs[0].name, teamAPlayerSpawnPoints[0].position, teamAPlayerSpawnPoints[0].rotation, 0));
                SpawnedBotsList.Add(PhotonNetwork.InstantiateSceneObject(this.botPrefabs[1].name, teamAPlayerSpawnPoints[1].position, teamAPlayerSpawnPoints[1].rotation, 0));
                // Instantiate our two bots at different spawn points for team B. Add each newly spawned bot to the list
                SpawnedBotsList.Add(PhotonNetwork.InstantiateSceneObject(this.botPrefabs[0].name, teamBPlayerSpawnPoints[0].position, teamBPlayerSpawnPoints[0].rotation, 0));
                SpawnedBotsList.Add(PhotonNetwork.InstantiateSceneObject(this.botPrefabs[1].name, teamBPlayerSpawnPoints[1].position, teamBPlayerSpawnPoints[1].rotation, 0));

                ((GameObject)SpawnedBotsList[0]).GetComponent<AICharacterControl>().target = ((GameObject)SpawnedWeaponsList[0]).transform;
                ((GameObject)SpawnedBotsList[1]).GetComponent<AICharacterControl>().target = ((GameObject)SpawnedWeaponsList[0]).transform;
                ((GameObject)SpawnedBotsList[2]).GetComponent<AICharacterControl>().target = ((GameObject)SpawnedWeaponsList[0]).transform;
                ((GameObject)SpawnedBotsList[3]).GetComponent<AICharacterControl>().target = ((GameObject)SpawnedWeaponsList[0]).transform;
            }
        }
    }
}
