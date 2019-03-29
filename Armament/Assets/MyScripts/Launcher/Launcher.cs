using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{ 
    
    public class Launcher : MonoBehaviourPunCallbacks
    {
        #region Public Fields

        public const string KEY_ARENA_FILTER = "map";
        public const string KEY_AI_FILTER = "ai";
        
        #endregion Public Fields

        #region Private Serializable Fields

        // *This variable is temporary and just to be used during early development. 
        // I was running into long build times when including the original (very large) unity scene file in the build.
        // Using this variable to quickly change what scene the master client code will load and removing all scenes 
        // except for Launcher and Simple Room from the build will hopefully keep build times down and make debugging
        // multiplayer-related code (that requires running a built second instance of the game) a lot easier.
        // Currently, this information is also accessed in the GameManager class to determine the correct transform
        // for the team dividing wall prefab during instantiation in the Start method.
        [Tooltip("The game level (unity scene) to load. See comments in code for more information.")]
        [SerializeField] public static string developmentOnly_levelToLoad = "Simple Room";

        [Tooltip("The UI Panel to let the user enter name, connect and play")]
        [SerializeField] private GameObject controlPanel;
        [Tooltip("The UI Label to inform the user that the connection is in progress")]
        [SerializeField] private GameObject progressLabel;
        [SerializeField] private InputField roomNameInputField;
        [SerializeField] private ScrollRect existingRoomList;
        [SerializeField] private GameObject playerData;
        [SerializeField] private Slider avatarSelectSlider;
        [SerializeField] private Dropdown arenaFilterDropdown;
        [Tooltip("Names of Unity Scenes that will be used as game arenas. Make sure you get the name exactly right!")]
        [SerializeField] private string[] namesOfArenas;


        /// <summary>
        /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
        /// </summary>
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField] private byte maxPlayersPerRoom = 4;

        #endregion Private Serializable Fields

        #region Private Fields

        // Debug flags
        private const bool DEBUG = true; // indicates whether we are debugging this class
        private const bool DEBUG_JoinRandomRoom = true; 

        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
        /// </summary>
        string gameVersion = "1";

        List<RoomInfo> _roomList;
        private int avatarSliderValue;
        /// <summary>
        /// An InputField to store a reference to the players. There is no null checking, players can enter room w/ blank name. This may change when we implement playfab. 
        /// </summary>
        //private InputField PlayerName;//make read only?

        /// <summary>
        /// Keep track of the current process. Since connection is asynchronous and is based on several callbacks from Photon,
        /// we need to keep track of this to properly adjust the behavior when we receive call back by Photon.
        /// Typically this is used for the OnConnectedToMaster() callback.
        /// </summary>
        bool isConnecting;
        bool isJoiningNamedRoom = false;

        #endregion Private Fields

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        private void Awake()
        {
            // #Critical
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            // If no arena names were specified in the inspector
            if (namesOfArenas.Length == 0)
            {
                Debug.LogError("Launcher: Start() Names of Arenas were not set in inspector!");
                return;
            }

            //progressLabel.SetActive(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;//frees up the cursor
            controlPanel.SetActive(true);
            
            // Add options to the arena filter dropdown based on the list of arenas specified on this script in the inspector
            List<Dropdown.OptionData> arenaOptions = new List<Dropdown.OptionData>();
            foreach (string arenaName in namesOfArenas)
            {
                arenaOptions.Add(new Dropdown.OptionData(arenaName));
            }
            arenaFilterDropdown.AddOptions(arenaOptions);

        }


        #endregion
        
        #region Public Methods

        public int GetAvatarSliderValueFromSlider()
        {
            if(avatarSelectSlider != null)
            {
                return (int)avatarSelectSlider.value;
            }
            return -1;
        }

        /// <summary>
        /// Lists the existing rooms.
        /// </summary>
        public void ListExistingRooms()
        {
            Debug.Log("Launcher: ListExistingRooms()");
            
            // Join the Default TypedLobby
            PhotonNetwork.JoinLobby(TypedLobby.Default);

            // Get the contents of the existing rooms list
            RectTransform scrollableContent = existingRoomList.content;
            VerticalLayoutGroup verticalLayoutGroup = scrollableContent.GetComponent<VerticalLayoutGroup>();
            

        }


        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            Debug.Log("Launcher: Connect()");
            //keep track of the will to join a room, because when we come back from the game we will get a callback that we are connected, so we need to know what to do then
            isConnecting = true;

            progressLabel.GetComponent<Text>().text = "Connecting...";

            //progressLabel.SetActive(true);
            //controlPanel.SetActive(false);

            /** My Note:
             *   I'm going to try to force connection to 'us' region for testing purposes.
             *   If my two clients are in different regions they cannot enter the same room.
             *   ...
             *   Looks like I didn't need this code. The reason clients weren't ending up in the same rooms was because
             *   they were connecting to different regional servers. I changed the whitelist to "us;" only. This seems
             *   to be a good solution for testing but would need to change before publishing.
             */

            /*// we check if we are connected or not, we join if we are, else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                // #Critical, we must first and foremost connect to Photon Online Server.
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }*/

            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        public void JoinRandomRoom()
        {
            playerData.GetComponent<PlayerData>().SetAvatarChoice(GetAvatarSliderValueFromSlider());

            if (PhotonNetwork.IsConnected)
            {
                // Get the arena filter information
                int arenaFilterIndex = arenaFilterDropdown.value;
                string selectedArenaFilter = arenaFilterDropdown.options[arenaFilterIndex].text;

                if (DEBUG && DEBUG_JoinRandomRoom) Debug.LogFormat("Launcher: JoinRandomRoom() arenaFilterIndex = {0}, selectedArenaFilter = {1}", arenaFilterIndex, selectedArenaFilter);

                // If user does not want to filter arenas...
                if (arenaFilterIndex == 0)
                {
                    progressLabel.GetComponent<Text>().text = "Joining Random Room...";
                    // Join random room with no filter
                    PhotonNetwork.JoinRandomRoom();
                }
                // If user wants to filter arenas...
                else
                {
                    progressLabel.GetComponent<Text>().text = "Joining Random Room... (Arena Filter = " + selectedArenaFilter + ")";
                    // Join random room matching arena filter
                    PhotonNetwork.JoinRandomRoom(new ExitGames.Client.Photon.Hashtable() { { KEY_ARENA_FILTER, arenaFilterIndex } }, 0);
                }
            }
            else
            {
                Connect();
            }
        }

        /// <summary>
        /// Join or create room without expected users
        /// </summary>
        public void JoinOrCreateRoom()
        {
            // Join or create room (with no expected users)
            JoinOrCreateRoom(null);
        }

        /// <summary>
        /// Join or create room with expected users
        /// </summary>
        /// <param name="expectedUsers">The expected users.</param>
        public void JoinOrCreateRoom(string[] expectedUsers)
        {

            playerData.GetComponent<PlayerData>().SetAvatarChoice(GetAvatarSliderValueFromSlider());

            Debug.Log("Launcher: JoinOrCreateRoom(string[] expectedUsers)");
            progressLabel.GetComponent<Text>().text = "Joining or Creating Room...";

            // In case we are not yet connected, keep track of what we're trying to do with isJoiningNamedRoom...
            // We will try to connect first. Then this function will be called again.
            isJoiningNamedRoom = true;

            // Check that we are connected to master server before we attempt to join a room.
            // If we're not connected to a master server...
            if (!PhotonNetwork.IsConnected)
            {
                // PlayerData.GetComponent<PlayerData>().SetAvatarChoice(GetAvatarSliderValueFromSlider());
                progressLabel.GetComponent<Text>().text = "Not yet connected!";
                Debug.Log("Launcher: JoinOrCreateRoom(string[] expectedUsers) Not yet connected! ");
                // Try to connect
                Connect();
                // Do not continue to trying to join a room. We will wait for the OnConnectedToMaster callback to call this function again
                return;
            }

            // Get the roomName from the GUI input field
            string roomName = roomNameInputField.text;
            
            // Create a new RoomOptions
            RoomOptions roomOptions = new RoomOptions();

            // Set room options
            //roomOptions.CustomRoomPropertiesForLobby = { "map", "ai" };
            //roomOptions.CustomRoomProperties = new Hashtable() { { "map", 1 } };
            //roomOptions.MaxPlayers = expectedMaxPlayers;

            // Join or create room (w/out expected users)
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default, expectedUsers);
        }

        #endregion

        #region MonoBehaviourPunCallbacks Callbacks

        public override void OnJoinedLobby()
        {
            Debug.Log("Launcher: OnJoinedLobby()");
            progressLabel.GetComponent<Text>().text = "Joined Lobby!";
            progressLabel.SetActive(true);
        }
        
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log("Launcher: OnRoomListUpdate()");
            _roomList = roomList;
            foreach (RoomInfo game in _roomList)
            {
                Debug.LogFormat("Launcher: ListExisitingRooms() game.Name = {0}, game.PlayerCount = {1}, game.MaxPlayers{2}", game.Name, game.PlayerCount, game.MaxPlayers);
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
            progressLabel.GetComponent<Text>().text = "Connected to Master server!";

            // we don't want to do anything if we are not attempting to join a room.
            // this case where isConnecting is false is typically when you lost or quit the game, when this level is loaded, OnConnectedToMaster will be called, in that case
            // we don't want to do anything.
            if (isConnecting)
            {
                // If user wanted to join a specific room but was not yet connected to master server...
                // (the JoinOrCreateRoom() function will have set isJoiningNamedRoom to true, tried to connect to master server, and then returned)
                if (isJoiningNamedRoom)
                {
                    // We need to call JoinOrCreateRoom back to finish its work
                    JoinOrCreateRoom();
                }
                // If user did not want to join a specific room...
                else
                {
                    //#Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
                    //PhotonNetwork.JoinRandomRoom();
                    // We need to call JoinRandomRoom back to finish its work
                    JoinRandomRoom();
                }
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            //progressLabel.SetActive(false);
            //controlPanel.SetActive(true);

            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnect() was called by PUN with reason {0}", cause);
            progressLabel.GetComponent<Text>().text = string.Format("Disconnected: {0}", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            //Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");


            // Get the arena filter information
            int arenaFilterIndex = arenaFilterDropdown.value;
            string selectedArenaFilter = arenaFilterDropdown.options[arenaFilterIndex].text;

            // If user does not want to filter arenas...
            if (arenaFilterIndex == 0)
            {
                progressLabel.GetComponent<Text>().text = "No random room available. Creating new room...";

                // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
                PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
            }
            // If user wants to filter arenas...
            else
            {
                progressLabel.GetComponent<Text>().text = "No random room available. Creating new room... (Arena Filter = " + selectedArenaFilter + ")";

                RoomOptions roomOptions = new RoomOptions();
                roomOptions.CustomRoomPropertiesForLobby = new string[]{ KEY_ARENA_FILTER, KEY_AI_FILTER };
                roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { KEY_ARENA_FILTER, arenaFilterIndex } };
                roomOptions.MaxPlayers = maxPlayersPerRoom;
                PhotonNetwork.CreateRoom(null, roomOptions);
            }

        }

        public override void OnJoinedRoom()
        {
            Debug.Log("PUN Basicis Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
            progressLabel.GetComponent<Text>().text = "Now this client is in a room!";
            
            // #Critical: We only load if we are the first player, else we rely on 'PhotonNetwork.AutomaticallySyncScene' to sync our instance scene.
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                Debug.Log("We load the '"+ developmentOnly_levelToLoad + "'");


                // Get the arena filter information
                int arenaFilterIndex = arenaFilterDropdown.value;
                string selectedArenaFilter = arenaFilterDropdown.options[arenaFilterIndex].text;

                // If user doesn't care what Arena is loaded
                if (arenaFilterIndex == 0)
                {
                    developmentOnly_levelToLoad = namesOfArenas[new System.Random().Next(0, namesOfArenas.Length)];
                }
                else
                {
                    // Set the level/arena to load
                    developmentOnly_levelToLoad = selectedArenaFilter;
                }

                progressLabel.GetComponent<Text>().text = "Loading the '" + developmentOnly_levelToLoad + "' map...";

                // #Critical
                // Load the Room Level.
                PhotonNetwork.LoadLevel(developmentOnly_levelToLoad);
            }
        }

        #endregion
    }
}